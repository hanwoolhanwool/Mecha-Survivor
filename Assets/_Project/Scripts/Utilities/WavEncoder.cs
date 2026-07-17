using System;

namespace MechaSurvivor.Utilities
{
    /// <summary>
    /// float[-1,1] 샘플 → 16비트 PCM 모노 WAV 바이트. 에디터 SFX 베이크 전용.
    /// UnityEngine 의존 없음 — EditMode 테스트 대상.
    /// </summary>
    public static class WavEncoder
    {
        private const int HeaderSize = 44;
        private const short BitsPerSample = 16;
        private const short Channels = 1;

        public static byte[] EncodePcm16(float[] samples, int sampleRate)
        {
            if (samples == null || samples.Length == 0)
            {
                throw new ArgumentException("샘플이 비어 있다.", nameof(samples));
            }

            int dataSize = samples.Length * 2;
            var bytes = new byte[HeaderSize + dataSize];

            WriteAscii(bytes, 0, "RIFF");
            WriteInt32(bytes, 4, HeaderSize - 8 + dataSize);
            WriteAscii(bytes, 8, "WAVE");

            WriteAscii(bytes, 12, "fmt ");
            WriteInt32(bytes, 16, 16);                    // fmt 청크 크기
            WriteInt16(bytes, 20, 1);                     // PCM
            WriteInt16(bytes, 22, Channels);
            WriteInt32(bytes, 24, sampleRate);
            WriteInt32(bytes, 28, sampleRate * Channels * BitsPerSample / 8);
            WriteInt16(bytes, 32, Channels * BitsPerSample / 8);
            WriteInt16(bytes, 34, BitsPerSample);

            WriteAscii(bytes, 36, "data");
            WriteInt32(bytes, 40, dataSize);

            for (int i = 0; i < samples.Length; i++)
            {
                float clamped = Math.Max(-1f, Math.Min(1f, samples[i]));
                short pcm = (short)Math.Round(clamped * short.MaxValue);
                WriteInt16(bytes, HeaderSize + i * 2, pcm);
            }

            return bytes;
        }

        private static void WriteAscii(byte[] dst, int offset, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                dst[offset + i] = (byte)text[i];
            }
        }

        private static void WriteInt16(byte[] dst, int offset, int value)
        {
            dst[offset] = (byte)value;
            dst[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteInt32(byte[] dst, int offset, int value)
        {
            dst[offset] = (byte)value;
            dst[offset + 1] = (byte)(value >> 8);
            dst[offset + 2] = (byte)(value >> 16);
            dst[offset + 3] = (byte)(value >> 24);
        }
    }
}
