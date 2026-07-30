using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>RigLab 조작 순수 계산 검증 (Docs/06 §6).</summary>
    public sealed class RigLabMathTests
    {
        [Test]
        public void DirectionForIndex_Zero_IsForward()
        {
            Vector2 dir = RigLabMath.DirectionForIndex(0);
            Assert.Less(Vector2.Distance(dir, new Vector2(0f, 1f)), 1e-4f);
        }

        [Test]
        public void DirectionForIndex_Two_IsRight()
        {
            Vector2 dir = RigLabMath.DirectionForIndex(2);
            Assert.Less(Vector2.Distance(dir, new Vector2(1f, 0f)), 1e-4f);
        }

        [Test]
        public void DirectionForIndex_AllEight_AreUnitVectors()
        {
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(1f, RigLabMath.DirectionForIndex(i).magnitude, 1e-4f,
                    $"index {i}는 단위 벡터여야 한다.");
            }
        }

        [Test]
        public void DirectionForIndex_WrapsAndHandlesNegative()
        {
            Assert.Less(Vector2.Distance(
                RigLabMath.DirectionForIndex(8), RigLabMath.DirectionForIndex(0)), 1e-4f);
            Assert.Less(Vector2.Distance(
                RigLabMath.DirectionForIndex(-1), RigLabMath.DirectionForIndex(7)), 1e-4f);
        }

        [Test]
        public void StepPlaybackSpeed_ClampsToRange()
        {
            Assert.AreEqual(RigLabMath.MaxPlaybackSpeed,
                RigLabMath.StepPlaybackSpeed(1.95f, 0.5f));
            Assert.AreEqual(RigLabMath.MinPlaybackSpeed,
                RigLabMath.StepPlaybackSpeed(0.15f, -0.5f));
            Assert.AreEqual(1.1f, RigLabMath.StepPlaybackSpeed(1f, 0.1f), 1e-4f);
        }

        [Test]
        public void CycleIndex_WrapsBothDirections()
        {
            Assert.AreEqual(0, RigLabMath.CycleIndex(4, 1, 5));
            Assert.AreEqual(4, RigLabMath.CycleIndex(0, -1, 5));
            Assert.AreEqual(0, RigLabMath.CycleIndex(3, 1, 0), "빈 목록은 0 고정.");
        }

        [Test]
        public void ResolveEnemyMuzzleOffset_NoProfile_UsesEnemyData()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.MuzzleOffset = new Vector3(0f, 2f, 0.5f);

            Assert.AreEqual(new Vector3(0f, 2f, 0.5f),
                RigProfileMath.ResolveEnemyMuzzleOffset(data));

            Object.DestroyImmediate(data);
        }

        [Test]
        public void ResolveEnemyMuzzleOffset_ProfileMuzzle_TakesPriority()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var profile = ScriptableObject.CreateInstance<RigProfileData>();
            profile.Muzzles = new[]
            {
                new RigProfileData.MuzzleDef
                {
                    Id = RigProfileMath.EnemyMainMuzzleId,
                    MountId = "",
                    LocalPosition = new Vector3(0f, 3f, 1f),
                },
            };
            data.RigProfile = profile;

            Assert.AreEqual(new Vector3(0f, 3f, 1f),
                RigProfileMath.ResolveEnemyMuzzleOffset(data));

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void ResolveEnemyMuzzleOffset_MountedMuzzle_IsIgnoredForOffsetPath()
        {
            // 마운트 기준 총구는 빌더 경로 전용 — 오프셋 폴백은 모델 루트 기준만 읽는다.
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var profile = ScriptableObject.CreateInstance<RigProfileData>();
            profile.Muzzles = new[]
            {
                new RigProfileData.MuzzleDef
                {
                    Id = RigProfileMath.EnemyMainMuzzleId,
                    MountId = "SomeMount",
                    LocalPosition = new Vector3(9f, 9f, 9f),
                },
            };
            data.RigProfile = profile;

            Assert.AreEqual(data.MuzzleOffset,
                RigProfileMath.ResolveEnemyMuzzleOffset(data));

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(data);
        }
    }
}
