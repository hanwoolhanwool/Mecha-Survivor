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
        public void CycleWithNone_CyclesThroughNoneAndBackToStart()
        {
            // -1(없음) → 0 → 1 → 2 → -1 (마운트/총구 선택, 포즈 클립 선택 공용)
            Assert.AreEqual(0, RigLabMath.CycleWithNone(-1, 1, 3));
            Assert.AreEqual(1, RigLabMath.CycleWithNone(0, 1, 3));
            Assert.AreEqual(2, RigLabMath.CycleWithNone(1, 1, 3));
            Assert.AreEqual(-1, RigLabMath.CycleWithNone(2, 1, 3), "마지막 다음은 '없음'으로 돌아온다.");
        }

        [Test]
        public void CycleWithNone_NegativeDeltaAndEmptyList()
        {
            Assert.AreEqual(2, RigLabMath.CycleWithNone(-1, -1, 3), "'없음'에서 역방향은 마지막.");
            Assert.AreEqual(-1, RigLabMath.CycleWithNone(0, -1, 3));
            Assert.AreEqual(-1, RigLabMath.CycleWithNone(0, 1, 0), "빈 목록은 항상 '없음'.");
            Assert.AreEqual(-1, RigLabMath.CycleWithNone(-1, 1, 0));
        }

        [Test]
        public void CycleWithNone_SingleEntry_TogglesWithNone()
        {
            // 포즈 클립이 1개일 때 P 키는 켜기/끄기 토글처럼 동작해야 한다.
            Assert.AreEqual(0, RigLabMath.CycleWithNone(-1, 1, 1));
            Assert.AreEqual(-1, RigLabMath.CycleWithNone(0, 1, 1));
        }

        // ── 장착 무기 표시 규칙 ──────────────────────────────────

        [Test]
        public void ShouldShowMount_NothingEquipped_ShowsEverything()
        {
            // 랩 기본값(전체 표시) — 종전처럼 모든 장착 모델을 한꺼번에 본다.
            Assert.IsTrue(RigLabMath.ShouldShowMount("laser_cannon", null));
            Assert.IsTrue(RigLabMath.ShouldShowMount("gatling", string.Empty));
            Assert.IsTrue(RigLabMath.ShouldShowMount(null, null));
        }

        [Test]
        public void ShouldShowMount_Equipped_ShowsOnlyMatchingWeapon()
        {
            Assert.IsTrue(RigLabMath.ShouldShowMount("beam", "beam"));
            Assert.IsFalse(RigLabMath.ShouldShowMount("gatling", "beam"),
                "다른 무기의 마운트는 숨겨야 같은 손 모델끼리 겹치지 않는다.");
            Assert.IsFalse(RigLabMath.ShouldShowMount("laser_cannon", "beam"));
        }

        [Test]
        public void ShouldShowMount_UnboundMount_StaysVisibleWhileEquipped()
        {
            // ShowForWeapon이 없는 마운트는 무기 조건이 없는 장식 — 장착과 무관하게 남는다.
            Assert.IsTrue(RigLabMath.ShouldShowMount(null, "beam"));
            Assert.IsTrue(RigLabMath.ShouldShowMount(string.Empty, "beam"));
        }

        [Test]
        public void ShouldShowMount_IsCaseSensitiveOnWeaponId()
        {
            // WeaponData.Id는 통계 집계 키라 대소문자까지 정확히 일치해야 한다.
            Assert.IsFalse(RigLabMath.ShouldShowMount("Beam", "beam"));
        }

        // ── 조정 항목 필터 (총구) ────────────────────────────────

        [Test]
        public void ShouldShowMuzzle_NothingEquipped_ShowsEverything()
        {
            // 전체 표시 모드 = 종전 동작 — 조정 항목 목록이 프로필 전체다.
            Assert.IsTrue(RigLabMath.ShouldShowMuzzle("beam", "gatling", null));
            Assert.IsTrue(RigLabMath.ShouldShowMuzzle("beam", "gatling", string.Empty));
        }

        [Test]
        public void ShouldShowMuzzle_OwnMuzzle_AlwaysShown()
        {
            // 장착 무기 자신의 총구는 마운트 조건과 무관하게 조정 대상이어야 한다
            // (마운트 없이 모델 루트에서 쏘는 무기도 여기에 걸린다).
            Assert.IsTrue(RigLabMath.ShouldShowMuzzle("beam", "beam", "beam"));
            Assert.IsTrue(RigLabMath.ShouldShowMuzzle("beam", null, "beam"));
        }

        [Test]
        public void ShouldShowMuzzle_OtherWeaponMount_Hidden()
        {
            // 다른 무기의 마운트에 달린 총구는 모델도 안 보이므로 목록에서 뺀다.
            Assert.IsFalse(RigLabMath.ShouldShowMuzzle("gatling", "gatling", "beam"));
            Assert.IsFalse(RigLabMath.ShouldShowMuzzle("laser_cannon", "laser_cannon", "beam"));
        }

        [Test]
        public void ShouldShowMuzzle_SharedMount_ShowsSiblingMuzzles()
        {
            // 등 마운트는 missile_pod가 켜고 twin_rocket 총구를 함께 이고 있다 —
            // 모델이 보이는 동안 형제 총구도 만질 수 있어야 조정이 막히지 않는다.
            Assert.IsTrue(RigLabMath.ShouldShowMuzzle("twin_rocket", "missile_pod", "missile_pod"));
        }

        [Test]
        public void ShouldShowMuzzle_UnboundMount_StaysVisible()
        {
            // 무기 조건 없는 마운트(또는 모델 루트 기준 총구)는 걸러내면 조정 방법이 사라진다.
            Assert.IsTrue(RigLabMath.ShouldShowMuzzle("enemy_main", null, "beam"));
            Assert.IsTrue(RigLabMath.ShouldShowMuzzle("enemy_main", string.Empty, "beam"));
        }

        // ── 손 규약 (Docs/06 §3.4) ──────────────────────────────

        [Test]
        public void MuzzleKey_AnyHand_KeepsPlainId()
        {
            // 등 마운트·적 총구는 종전 그대로 무기 Id가 키다 — 기존 프로필과 호환된다.
            Assert.AreEqual("missile_pod", RigProfileMath.MuzzleKey("missile_pod", MountHand.Any));
            Assert.AreEqual("gatling@R", RigProfileMath.MuzzleKey("gatling", MountHand.Right));
            Assert.AreEqual("gatling@L", RigProfileMath.MuzzleKey("gatling", MountHand.Left));
        }

        [Test]
        public void MatchesHand_AnyOnEitherSide_Passes()
        {
            // 손 조건 없는 마운트는 항상 표시, 손을 특정하지 않은 조회는 좌우를 안 가린다.
            Assert.IsTrue(RigProfileMath.MatchesHand(MountHand.Any, MountHand.Left));
            Assert.IsTrue(RigProfileMath.MatchesHand(MountHand.Right, MountHand.Any));
        }

        [Test]
        public void MatchesHand_OppositeHands_Fails()
        {
            // 이게 무너지면 무기 하나가 양손에 동시에 나타난다.
            Assert.IsFalse(RigProfileMath.MatchesHand(MountHand.Right, MountHand.Left));
            Assert.IsFalse(RigProfileMath.MatchesHand(MountHand.Left, MountHand.Right));
            Assert.IsTrue(RigProfileMath.MatchesHand(MountHand.Left, MountHand.Left));
        }

        [Test]
        public void ToMountHand_MapsWeaponHand()
        {
            Assert.AreEqual(MountHand.Right, RigProfileMath.ToMountHand(WeaponHand.Right));
            Assert.AreEqual(MountHand.Left, RigProfileMath.ToMountHand(WeaponHand.Left));
        }

        [Test]
        public void ResolveMuzzleHand_TakesHandFromOwningMount()
        {
            var profile = ScriptableObject.CreateInstance<RigProfileData>();
            profile.Mounts = new[]
            {
                new RigProfileData.MountDef { Id = "L", Hand = MountHand.Left },
                new RigProfileData.MountDef { Id = "Back", Hand = MountHand.Any },
            };

            Assert.AreEqual(MountHand.Left, RigProfileMath.ResolveMuzzleHand(profile, "L"));
            Assert.AreEqual(MountHand.Any, RigProfileMath.ResolveMuzzleHand(profile, "Back"));
            // 모델 루트 기준 총구(마운트 없음)와 이름이 틀린 마운트는 Any로 떨어진다.
            Assert.AreEqual(MountHand.Any, RigProfileMath.ResolveMuzzleHand(profile, ""));
            Assert.AreEqual(MountHand.Any, RigProfileMath.ResolveMuzzleHand(profile, "없는마운트"));

            Object.DestroyImmediate(profile);
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
