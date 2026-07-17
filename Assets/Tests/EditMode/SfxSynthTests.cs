using System;
using NUnit.Framework;
using MechaSurvivor.Utilities;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 절차 합성 검증: 모든 레시피가 유효한 오디오 버퍼를 내놓는지,
    /// 클리핑·클릭 노이즈(급격한 종료)가 없는지 확인한다.
    /// </summary>
    public sealed class SfxSynthTests
    {
        [Test]
        public void AllRecipes_ProduceFiniteSamplesWithinRange()
        {
            foreach (var recipe in SfxRecipes.All)
            {
                float[] samples = recipe.Render();

                Assert.Greater(samples.Length, 0, $"{recipe.Id}: 빈 버퍼");
                for (int i = 0; i < samples.Length; i++)
                {
                    Assert.IsFalse(float.IsNaN(samples[i]) || float.IsInfinity(samples[i]),
                        $"{recipe.Id}: 샘플 {i}이 유한하지 않다");
                    Assert.LessOrEqual(Math.Abs(samples[i]), 1f,
                        $"{recipe.Id}: 샘플 {i}이 클리핑된다 ({samples[i]})");
                }
            }
        }

        [Test]
        public void AllRecipes_AreAudible()
        {
            // 피크가 너무 작으면 사실상 무음 — 합성 실수를 잡는다.
            foreach (var recipe in SfxRecipes.All)
            {
                float[] samples = recipe.Render();
                float peak = 0f;
                for (int i = 0; i < samples.Length; i++)
                {
                    peak = Math.Max(peak, Math.Abs(samples[i]));
                }

                Assert.Greater(peak, 0.1f, $"{recipe.Id}: 피크 {peak} — 사실상 무음");
            }
        }

        [Test]
        public void OneShotRecipes_EndNearSilence()
        {
            // 끝이 잘려 있으면 재생 종료마다 "딱" 클릭이 난다. 루프 클립은 제외.
            foreach (var recipe in SfxRecipes.All)
            {
                if (recipe.Id == "thruster_loop")
                {
                    continue;
                }

                float[] samples = recipe.Render();
                float tail = Math.Abs(samples[samples.Length - 1]);

                Assert.Less(tail, 0.05f, $"{recipe.Id}: 마지막 샘플 {tail} — 클릭 노이즈 위험");
            }
        }

        [Test]
        public void Recipes_AreDeterministic()
        {
            // 시드 고정 — 다시 구워도 같은 소리 (에셋 재현성).
            foreach (var recipe in SfxRecipes.All)
            {
                float[] first = recipe.Render();
                float[] second = recipe.Render();

                Assert.AreEqual(first.Length, second.Length, $"{recipe.Id}: 길이 불일치");
                for (int i = 0; i < first.Length; i++)
                {
                    Assert.AreEqual(first[i], second[i], $"{recipe.Id}: 샘플 {i} 불일치");
                }
            }
        }

        [Test]
        public void RecipeIds_AreUnique()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var recipe in SfxRecipes.All)
            {
                Assert.IsTrue(seen.Add(recipe.Id), $"중복 id: {recipe.Id}");
            }
        }

        [Test]
        public void CrossfadeLoop_SeamMatchesLoopStart()
        {
            // 루프 접합부: 결과의 시작은 (머리 × 0) + (꼬리 × 1) = 원본 꼬리 시작과 같아야 한다.
            var buf = SfxSynth.Buffer(1f);
            SfxSynth.AddNoise(buf, 0.8f, seed: 42);

            int fadeSamples = (int)(0.1f * SfxSynth.SampleRate);
            float[] loop = SfxSynth.CrossfadeLoop(buf, 0.1f);

            Assert.AreEqual(buf.Length - fadeSamples, loop.Length);
            Assert.AreEqual(buf[loop.Length], loop[0], 1e-4f,
                "루프 시작이 원본 꼬리와 이어지지 않는다 — 접합부에서 틱 소리가 난다");
        }

        [Test]
        public void Normalize_ScalesPeakToTarget()
        {
            var buf = new float[] { 0.1f, -0.4f, 0.2f };
            SfxSynth.Normalize(buf, 0.8f);

            Assert.AreEqual(0.8f, Math.Abs(buf[1]), 1e-4f);
        }

        [Test]
        public void ApplyDecay_HalvesAmplitudeAtHalfLife()
        {
            var buf = new float[SfxSynth.SampleRate];
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = 1f;
            }

            SfxSynth.ApplyDecay(buf, 0.5f);

            Assert.AreEqual(0.5f, buf[SfxSynth.SampleRate / 2], 1e-3f,
                "halfLife 시점에서 진폭이 절반이어야 한다");
        }
    }
}
