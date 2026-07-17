using System;

namespace MechaSurvivor.Utilities
{
    /// <summary>
    /// 절차 합성 DSP 프리미티브. 외부 오디오 에셋 없이 SFX를 코드로 굽는다.
    /// UnityEngine 의존 없음 — 전부 EditMode 테스트 가능한 순수 로직.
    /// 모든 버퍼는 44.1kHz 모노 float[-1, 1] 이다.
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 44100;

        public static float[] Buffer(float seconds) =>
            new float[Math.Max(1, (int)(seconds * SampleRate))];

        // ── 오실레이터 (모두 기존 버퍼에 더한다 — 레이어링) ────────────

        /// <summary>사인파. 주파수는 버퍼 길이에 걸쳐 start→end 선형 스윕.</summary>
        public static void AddSine(float[] buf, float freqStart, float freqEnd, float amp)
        {
            double phase = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / buf.Length;
                double freq = freqStart + (freqEnd - freqStart) * t;
                phase += 2.0 * Math.PI * freq / SampleRate;
                buf[i] += amp * (float)Math.Sin(phase);
            }
        }

        /// <summary>구형파 — 거친 전자음(레이저·EMP)용.</summary>
        public static void AddSquare(float[] buf, float freqStart, float freqEnd, float amp)
        {
            double phase = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / buf.Length;
                double freq = freqStart + (freqEnd - freqStart) * t;
                phase += freq / SampleRate;
                buf[i] += amp * (phase % 1.0 < 0.5 ? 1f : -1f);
            }
        }

        /// <summary>톱니파 — 빔 험(hum)의 배음용.</summary>
        public static void AddSaw(float[] buf, float freqStart, float freqEnd, float amp)
        {
            double phase = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / buf.Length;
                double freq = freqStart + (freqEnd - freqStart) * t;
                phase += freq / SampleRate;
                buf[i] += amp * (float)(phase % 1.0 * 2.0 - 1.0);
            }
        }

        /// <summary>FM 사인 — 캐리어 주파수가 modRate(Hz)로 ±modDepth 워블. 그래비티 웰용.</summary>
        public static void AddFmSine(float[] buf, float carrier, float modDepth, float modRate, float amp)
        {
            double phase = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                double t = (double)i / SampleRate;
                double freq = carrier + modDepth * Math.Sin(2.0 * Math.PI * modRate * t);
                phase += 2.0 * Math.PI * freq / SampleRate;
                buf[i] += amp * (float)Math.Sin(phase);
            }
        }

        /// <summary>백색 소음 — 시드 고정으로 결정적(재생성해도 같은 소리).</summary>
        public static void AddNoise(float[] buf, float amp, int seed)
        {
            var rng = new Random(seed);
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] += amp * ((float)rng.NextDouble() * 2f - 1f);
            }
        }

        // ── 필터·셰이핑 (제자리 변형) ─────────────────────────────────

        /// <summary>1폴 로우패스. 컷오프는 버퍼에 걸쳐 start→end 스윕 — 폭발의 "퍼엉→웅" 감쇠를 만든다.</summary>
        public static void LowPass(float[] buf, float cutoffStart, float cutoffEnd)
        {
            float y = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / buf.Length;
                float cutoff = cutoffStart + (cutoffEnd - cutoffStart) * t;
                float a = (float)Math.Exp(-2.0 * Math.PI * cutoff / SampleRate);
                y = a * y + (1f - a) * buf[i];
                buf[i] = y;
            }
        }

        /// <summary>지수 감쇠 엔벨로프. halfLife초마다 진폭이 절반이 된다.</summary>
        public static void ApplyDecay(float[] buf, float halfLife)
        {
            float k = (float)(Math.Log(2.0) / (halfLife * SampleRate));
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] *= (float)Math.Exp(-k * i);
            }
        }

        /// <summary>선형 어택 램프 — 클릭 노이즈 방지 + 차오르는 예열감.</summary>
        public static void ApplyAttack(float[] buf, float seconds)
        {
            int n = Math.Min(buf.Length, (int)(seconds * SampleRate));
            for (int i = 0; i < n; i++)
            {
                buf[i] *= (float)i / n;
            }
        }

        /// <summary>끝부분 선형 페이드아웃 — 잘린 듯한 종료 방지.</summary>
        public static void FadeOut(float[] buf, float seconds)
        {
            int n = Math.Min(buf.Length, (int)(seconds * SampleRate));
            for (int i = 0; i < n; i++)
            {
                int idx = buf.Length - n + i;
                buf[idx] *= 1f - (float)i / n;
            }
        }

        /// <summary>블록 단위 랜덤 뮤트 — EMP의 지직거리는 신호 끊김.</summary>
        public static void Gate(float[] buf, float blockSeconds, float dropProbability, int seed)
        {
            var rng = new Random(seed);
            int blockSize = Math.Max(1, (int)(blockSeconds * SampleRate));
            for (int start = 0; start < buf.Length; start += blockSize)
            {
                if (rng.NextDouble() >= dropProbability)
                {
                    continue;
                }

                int end = Math.Min(start + blockSize, buf.Length);
                for (int i = start; i < end; i++)
                {
                    buf[i] = 0f;
                }
            }
        }

        /// <summary>source를 target의 offset초 지점에 겹쳐 넣는다 (레이어 합성).</summary>
        public static void MixInto(float[] target, float[] source, float offsetSeconds, float gain)
        {
            int offset = (int)(offsetSeconds * SampleRate);
            int n = Math.Min(source.Length, target.Length - offset);
            for (int i = 0; i < n; i++)
            {
                target[offset + i] += source[i] * gain;
            }
        }

        /// <summary>tanh 소프트 클립 — 폭발음의 압축된 "빵" 질감.</summary>
        public static void SoftClip(float[] buf, float drive)
        {
            float norm = (float)Math.Tanh(drive);
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (float)Math.Tanh(buf[i] * drive) / norm;
            }
        }

        /// <summary>피크를 지정값으로 정규화. 무음 버퍼는 그대로 둔다.</summary>
        public static void Normalize(float[] buf, float peak)
        {
            float max = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float abs = Math.Abs(buf[i]);
                if (abs > max)
                {
                    max = abs;
                }
            }

            if (max < 1e-6f)
            {
                return;
            }

            float scale = peak / max;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] *= scale;
            }
        }

        /// <summary>
        /// 루프용 크로스페이드: 꼬리 fade초를 머리에 겹쳐 접합부를 없앤 뒤 잘라낸다.
        /// 반환 버퍼는 (원본 - fade) 길이의 무이음(seamless) 루프다.
        /// </summary>
        public static float[] CrossfadeLoop(float[] buf, float fadeSeconds)
        {
            int fade = Math.Min(buf.Length / 2, (int)(fadeSeconds * SampleRate));
            int outLength = buf.Length - fade;
            var result = new float[outLength];
            Array.Copy(buf, result, outLength);
            for (int i = 0; i < fade; i++)
            {
                float w = (float)i / fade;
                result[i] = result[i] * w + buf[outLength + i] * (1f - w);
            }

            return result;
        }
    }
}
