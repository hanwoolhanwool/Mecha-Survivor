using System;
using NUnit.Framework;
using MechaSurvivor.Utilities;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>WAV 인코딩 검증 — 헤더가 깨지면 Unity가 임포트 자체를 거부한다.</summary>
    public sealed class WavEncoderTests
    {
        [Test]
        public void Encode_WritesValidRiffHeader()
        {
            byte[] wav = WavEncoder.EncodePcm16(new float[100], 44100);

            Assert.AreEqual("RIFF", ReadAscii(wav, 0, 4));
            Assert.AreEqual("WAVE", ReadAscii(wav, 8, 4));
            Assert.AreEqual("fmt ", ReadAscii(wav, 12, 4));
            Assert.AreEqual("data", ReadAscii(wav, 36, 4));
        }

        [Test]
        public void Encode_SizeFieldsAreConsistent()
        {
            const int sampleCount = 1234;
            byte[] wav = WavEncoder.EncodePcm16(new float[sampleCount], 44100);

            int riffSize = BitConverter.ToInt32(wav, 4);
            int dataSize = BitConverter.ToInt32(wav, 40);

            Assert.AreEqual(sampleCount * 2, dataSize, "16비트 모노: 샘플당 2바이트");
            Assert.AreEqual(wav.Length - 8, riffSize);
            Assert.AreEqual(44 + dataSize, wav.Length);
        }

        [Test]
        public void Encode_DeclaresPcm16Mono()
        {
            byte[] wav = WavEncoder.EncodePcm16(new float[10], 44100);

            Assert.AreEqual(1, BitConverter.ToInt16(wav, 20), "포맷 = PCM");
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 22), "채널 = 모노");
            Assert.AreEqual(44100, BitConverter.ToInt32(wav, 24), "샘플레이트");
            Assert.AreEqual(16, BitConverter.ToInt16(wav, 34), "비트 심도");
        }

        [Test]
        public void Encode_RoundTripsSampleValues()
        {
            var samples = new[] { 0f, 0.5f, -0.5f, 1f, -1f };
            byte[] wav = WavEncoder.EncodePcm16(samples, 44100);

            for (int i = 0; i < samples.Length; i++)
            {
                short pcm = BitConverter.ToInt16(wav, 44 + i * 2);
                Assert.AreEqual(samples[i], (float)pcm / short.MaxValue, 1e-3f,
                    $"샘플 {i} 왕복 오차");
            }
        }

        [Test]
        public void Encode_ClampsOutOfRangeSamples()
        {
            // 합성 실수로 범위를 벗어나도 정수 오버플로(랩어라운드 잡음)가 나면 안 된다.
            byte[] wav = WavEncoder.EncodePcm16(new[] { 2f, -2f }, 44100);

            Assert.AreEqual(short.MaxValue, BitConverter.ToInt16(wav, 44));
            Assert.AreEqual(-short.MaxValue, BitConverter.ToInt16(wav, 46));
        }

        [Test]
        public void Encode_EmptySamples_Throws()
        {
            Assert.Throws<ArgumentException>(() => WavEncoder.EncodePcm16(new float[0], 44100));
        }

        private static string ReadAscii(byte[] bytes, int offset, int count)
        {
            var chars = new char[count];
            for (int i = 0; i < count; i++)
            {
                chars[i] = (char)bytes[offset + i];
            }

            return new string(chars);
        }
    }
}
