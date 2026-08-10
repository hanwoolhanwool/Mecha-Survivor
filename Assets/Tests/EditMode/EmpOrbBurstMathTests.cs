using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>EMP 오브 기폭 연출 수학 + 기폭 거리 클램프 검증.</summary>
    public sealed class EmpOrbBurstMathTests
    {
        private const float Eps = 1e-4f;

        // ---- 기폭 거리 클램프 (EmpProjectile) ----

        [Test]
        public void ClampDetonationRange_기폭거리가_사거리보다_짧으면_기폭거리()
        {
            Assert.AreEqual(12f, EmpProjectile.ClampDetonationRange(80f, 12f), Eps);
        }

        [Test]
        public void ClampDetonationRange_기폭거리가_더_길면_사거리_유지()
        {
            Assert.AreEqual(8f, EmpProjectile.ClampDetonationRange(8f, 12f), Eps);
        }

        [Test]
        public void ClampDetonationRange_0이하면_사거리_그대로()
        {
            Assert.AreEqual(80f, EmpProjectile.ClampDetonationRange(80f, 0f), Eps);
            Assert.AreEqual(80f, EmpProjectile.ClampDetonationRange(80f, -1f), Eps);
        }

        // ---- 접촉 반경 (EmpProjectile) ----

        [Test]
        public void ContactRadius_오브_스케일에_비례한다()
        {
            Assert.AreEqual(0.5f, EmpProjectile.ContactRadius(0.5f, 1f), Eps);
            Assert.AreEqual(0.9f, EmpProjectile.ContactRadius(0.5f, 1.8f), Eps);
        }

        [Test]
        public void ContactRadius_0이면_중심선_판정으로_되돌린다()
        {
            Assert.AreEqual(0f, EmpProjectile.ContactRadius(0f, 1.8f), Eps);
            Assert.AreEqual(0f, EmpProjectile.ContactRadius(-1f, 1.8f), Eps);
        }

        [Test]
        public void ContactRadius_스케일이_0이어도_반경이_사라지지_않는다()
        {
            Assert.Greater(EmpProjectile.ContactRadius(0.5f, 0f), 0f);
        }

        // ---- 이징 ----

        [Test]
        public void EaseOutCubic_경계값()
        {
            Assert.AreEqual(0f, EmpOrbBurstMath.EaseOutCubic(0f), Eps);
            Assert.AreEqual(1f, EmpOrbBurstMath.EaseOutCubic(1f), Eps);
            Assert.AreEqual(0f, EmpOrbBurstMath.EaseOutCubic(-0.5f), Eps);
            Assert.AreEqual(1f, EmpOrbBurstMath.EaseOutCubic(1.5f), Eps);
        }

        [Test]
        public void EaseOutCubic_감속형이라_전반부가_후반부보다_빠르다()
        {
            float firstHalf = EmpOrbBurstMath.EaseOutCubic(0.5f);
            float secondHalf = 1f - firstHalf;
            Assert.Greater(firstHalf, secondHalf);
        }

        // ---- 방사 방향 ----

        [Test]
        public void SpreadDirection_파츠_위치의_방사_방향을_정규화해_반환()
        {
            Vector3 dir = EmpOrbBurstMath.SpreadDirection(new Vector3(3f, 0f, 4f), Vector3.up);
            Assert.AreEqual(1f, dir.magnitude, Eps);
            Assert.AreEqual(0.6f, dir.x, Eps);
            Assert.AreEqual(0.8f, dir.z, Eps);
        }

        [Test]
        public void SpreadDirection_중심과_겹치면_fallback()
        {
            Vector3 dir = EmpOrbBurstMath.SpreadDirection(Vector3.zero, new Vector3(0f, 2f, 0f));
            Assert.AreEqual(Vector3.up.x, dir.x, Eps);
            Assert.AreEqual(Vector3.up.y, dir.y, Eps);
            Assert.AreEqual(Vector3.up.z, dir.z, Eps);
        }

        // ---- 파츠 이동 ----

        [Test]
        public void FragmentPosition_t0은_제자리_t1은_퍼짐거리만큼_이동()
        {
            Vector3 rest = new Vector3(0.3f, 0f, 0f);
            Vector3 dir = Vector3.right;

            Vector3 at0 = EmpOrbBurstMath.FragmentPosition(rest, dir, 2.5f, 0f);
            Assert.AreEqual(rest.x, at0.x, Eps);

            Vector3 at1 = EmpOrbBurstMath.FragmentPosition(rest, dir, 2.5f, 1f);
            Assert.AreEqual(rest.x + 2.5f, at1.x, Eps);
        }

        // ---- 스케일 곡선 ----

        [Test]
        public void FragmentScale_축소_시작_전에는_1_끝에서_0()
        {
            Assert.AreEqual(1f, EmpOrbBurstMath.FragmentScale(0f, 0.55f), Eps);
            Assert.AreEqual(1f, EmpOrbBurstMath.FragmentScale(0.55f, 0.55f), Eps);
            Assert.AreEqual(0f, EmpOrbBurstMath.FragmentScale(1f, 0.55f), Eps);
        }

        [Test]
        public void FragmentScale_축소_구간에서는_단조_감소()
        {
            float a = EmpOrbBurstMath.FragmentScale(0.6f, 0.55f);
            float b = EmpOrbBurstMath.FragmentScale(0.8f, 0.55f);
            Assert.Greater(a, b);
        }

        [Test]
        public void CoreScale_시작은_1_종료_시점_이후에는_0()
        {
            Assert.AreEqual(1f, EmpOrbBurstMath.CoreScale(0f, 0.45f), Eps);
            Assert.AreEqual(0f, EmpOrbBurstMath.CoreScale(0.45f, 0.45f), Eps);
            Assert.AreEqual(0f, EmpOrbBurstMath.CoreScale(0.9f, 0.45f), Eps);
        }

        // ---- 필드 전개 곡선 (EmpField) ----

        [Test]
        public void VisualGrowFactor_시작은_0_전개_완료_후에는_1()
        {
            Assert.AreEqual(0f, EmpField.VisualGrowFactor(0f, 0.6f), Eps);
            Assert.AreEqual(1f, EmpField.VisualGrowFactor(0.6f, 0.6f), Eps);
            Assert.AreEqual(1f, EmpField.VisualGrowFactor(2f, 0.6f), Eps);
        }

        [Test]
        public void VisualGrowFactor_가속형이라_전반부에는_작게_머문다()
        {
            float atHalf = EmpField.VisualGrowFactor(0.3f, 0.6f);
            Assert.Less(atHalf, 0.5f);
        }

        [Test]
        public void VisualGrowFactor_전개_시간이_0이하면_즉시_1()
        {
            Assert.AreEqual(1f, EmpField.VisualGrowFactor(0f, 0f), Eps);
            Assert.AreEqual(1f, EmpField.VisualGrowFactor(0f, -1f), Eps);
        }
    }
}
