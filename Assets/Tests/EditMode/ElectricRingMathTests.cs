using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>EMP 필드 테두리 전기 링 좌표 생성 검증.</summary>
    public sealed class ElectricRingMathTests
    {
        private const float Eps = 1e-4f;

        [Test]
        public void FillRing_지터_0이면_정확한_원()
        {
            var buffer = new Vector3[16];
            ElectricRingMath.FillRing(buffer, 9f, 0f, 0f, new System.Random(1));

            foreach (Vector3 p in buffer)
            {
                float xz = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                Assert.AreEqual(9f, xz, Eps);
                Assert.AreEqual(0f, p.y, Eps);
            }
        }

        [Test]
        public void FillRing_모든_점이_지터_범위_안()
        {
            var buffer = new Vector3[64];
            ElectricRingMath.FillRing(buffer, 9f, 0.5f, 0.35f, new System.Random(42));

            foreach (Vector3 p in buffer)
            {
                float xz = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                Assert.GreaterOrEqual(xz, 9f - 0.5f - Eps);
                Assert.LessOrEqual(xz, 9f + 0.5f + Eps);
                Assert.LessOrEqual(Mathf.Abs(p.y), 0.35f + Eps);
            }
        }

        [Test]
        public void FillRing_점들이_원주를_고르게_돈다()
        {
            var buffer = new Vector3[4];
            ElectricRingMath.FillRing(buffer, 5f, 0f, 0f, new System.Random(1));

            // 4분할이면 0°, 90°, 180°, 270° — 첫 점은 +X, 두 번째는 +Z.
            Assert.AreEqual(5f, buffer[0].x, Eps);
            Assert.AreEqual(0f, buffer[0].z, Eps);
            Assert.AreEqual(0f, buffer[1].x, Eps);
            Assert.AreEqual(5f, buffer[1].z, Eps);
        }

        [Test]
        public void FillRing_같은_시드면_같은_결과()
        {
            var a = new Vector3[32];
            var b = new Vector3[32];
            ElectricRingMath.FillRing(a, 9f, 0.5f, 0.3f, new System.Random(7));
            ElectricRingMath.FillRing(b, 9f, 0.5f, 0.3f, new System.Random(7));

            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i], b[i]);
            }
        }

        // ---- 구형 케이지 (대원 링) ----

        [Test]
        public void FillOrientedRing_identity_방향은_FillRing과_동일()
        {
            var a = new Vector3[32];
            var b = new Vector3[32];
            ElectricRingMath.FillRing(a, 9f, 0.5f, 0.3f, new System.Random(7));
            ElectricRingMath.FillOrientedRing(
                b, 9f, 0.5f, 0.3f, Quaternion.identity, new System.Random(7));

            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i], b[i]);
            }
        }

        [Test]
        public void FillOrientedRing_지터_0이면_어떤_방향이든_원점_거리_반경_유지()
        {
            var buffer = new Vector3[32];
            var rng = new System.Random(11);
            Quaternion tilt = Quaternion.Euler(63f, 127f, 41f);
            ElectricRingMath.FillOrientedRing(buffer, 9f, 0f, 0f, tilt, rng);

            foreach (Vector3 p in buffer)
            {
                Assert.AreEqual(9f, p.magnitude, 1e-3f);
            }
        }

        [Test]
        public void RandomUnitVector_항상_단위_길이()
        {
            var rng = new System.Random(5);
            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(1f, ElectricRingMath.RandomUnitVector(rng).magnitude, 1e-3f);
            }
        }

        [Test]
        public void RandomRingOrientation_회전해도_길이가_보존된다()
        {
            var rng = new System.Random(9);
            for (int i = 0; i < 20; i++)
            {
                Quaternion q = ElectricRingMath.RandomRingOrientation(rng);
                Assert.AreEqual(1f, (q * Vector3.up).magnitude, 1e-3f);
                Assert.AreEqual(3f, (q * new Vector3(3f, 0f, 0f)).magnitude, 1e-3f);
            }
        }

        // ---- 흐르는 파동 (부드러운 연출) ----

        [Test]
        public void FlowWave_진폭이_1을_넘지_않는다()
        {
            for (int i = 0; i < 200; i++)
            {
                float w = ElectricRingMath.FlowWave(i * 0.37f, i * 0.11f, i * 0.53f);
                Assert.LessOrEqual(Mathf.Abs(w), 1f + 1e-4f);
            }
        }

        [Test]
        public void FlowWave_한_바퀴에서_이음새_없이_이어진다()
        {
            for (int i = 0; i < 10; i++)
            {
                float t = i * 0.7f;
                Assert.AreEqual(
                    ElectricRingMath.FlowWave(0f, t, 1.1f),
                    ElectricRingMath.FlowWave(Mathf.PI * 2f, t, 1.1f),
                    1e-3f);
            }
        }

        [Test]
        public void FlowWave_시간에_대해_연속이다()
        {
            // 아주 작은 시간 변화에는 아주 작은 값 변화 — 순간이동 없음.
            float a = ElectricRingMath.FlowWave(1f, 5f, 0.3f);
            float b = ElectricRingMath.FlowWave(1f, 5.001f, 0.3f);
            Assert.Less(Mathf.Abs(a - b), 0.02f);
        }

        [Test]
        public void FillFlowingRing_진폭_0이면_정확한_원()
        {
            var buffer = new Vector3[32];
            Quaternion tilt = Quaternion.Euler(30f, 80f, 10f);
            ElectricRingMath.FillFlowingRing(buffer, 9f, 0f, 0f, 3f, 1f, tilt);

            foreach (Vector3 p in buffer)
            {
                Assert.AreEqual(9f, p.magnitude, 1e-3f);
            }
        }

        [Test]
        public void FillFlowingRing_모든_점이_진폭_범위_안()
        {
            var buffer = new Vector3[64];
            ElectricRingMath.FillFlowingRing(
                buffer, 9f, 0.4f, 0.3f, 7f, 2f, Quaternion.identity);

            foreach (Vector3 p in buffer)
            {
                float xz = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                Assert.GreaterOrEqual(xz, 9f - 0.4f - Eps);
                Assert.LessOrEqual(xz, 9f + 0.4f + Eps);
                Assert.LessOrEqual(Mathf.Abs(p.y), 0.3f + Eps);
            }
        }
    }
}
