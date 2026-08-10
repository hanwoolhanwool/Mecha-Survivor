using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 사격 포즈 배선 가드 (Docs/05 §10-B8·B10) — 코드가 내보내는 PoseType과 AC_Mecha의
    /// 상태 진입 조건은 서로 다른 파일에 있어서 한쪽만 바뀌면 조용히 포즈가 안 나온다.
    /// 무기 에셋의 파지 방식도 여기서 고정한다 (Grip이 None이면 포즈가 통째로 사라진다).
    /// </summary>
    public sealed class WeaponPoseWiringTests
    {
        private const string ControllerPath = "Assets/_Project/Art/Models/Mecha/AC_Mecha.controller";
        private const string WeaponsDir = "Assets/_Project/ScriptableObjects/Weapons";
        private const string PoseLayer = "WeaponPose";
        private const string PoseParam = "PoseType";

        private AnimatorStateMachine _poseMachine;

        [SetUp]
        public void SetUp()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(controller, $"컨트롤러를 찾지 못했다: {ControllerPath}");

            _poseMachine = null;
            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                if (layer.name == PoseLayer)
                {
                    _poseMachine = layer.stateMachine;
                }
            }

            Assert.IsNotNull(_poseMachine, $"'{PoseLayer}' 레이어가 없다");
        }

        private AnimatorState FindState(string name)
        {
            foreach (ChildAnimatorState child in _poseMachine.states)
            {
                if (child.state.name == name)
                {
                    return child.state;
                }
            }

            return null;
        }

        /// <summary>Empty→state 진입 조건이 PoseType Equals expected 인지.</summary>
        private void AssertEntryCondition(string stateName, int expected)
        {
            AnimatorState empty = FindState("Empty");
            Assert.IsNotNull(empty, "기본 상태 Empty가 없다");

            foreach (AnimatorStateTransition transition in empty.transitions)
            {
                if (transition.destinationState == null || transition.destinationState.name != stateName)
                {
                    continue;
                }

                foreach (AnimatorCondition condition in transition.conditions)
                {
                    if (condition.parameter == PoseParam
                        && condition.mode == AnimatorConditionMode.Equals)
                    {
                        Assert.AreEqual(expected, (int)condition.threshold,
                            $"'{stateName}' 진입 조건이 코드의 PoseType과 다르다");
                        return;
                    }
                }
            }

            Assert.Fail($"Empty → '{stateName}' 의 {PoseParam} Equals 전환이 없다");
        }

        [Test]
        public void PoseStates_MatchAnimParamCodes()
        {
            AssertEntryCondition("HeroPose", MechaAnimParams.FirePoseHero);
            AssertEntryCondition("HeroPoseLeft", MechaAnimParams.FirePoseHeroLeft);
            AssertEntryCondition("HeroPoseTwoHand", MechaAnimParams.FirePoseHeroTwoHand);
            AssertEntryCondition("HeroPoseTwoHandLeft", MechaAnimParams.FirePoseHeroTwoHandLeft);
        }

        /// <summary>두손 BT 공통 검사 — 중앙(정지) + 축 4방, 대각은 인접 블렌드 (Docs/05 §10-B8 방침).</summary>
        private void AssertTwoHandTree(string stateName, string clipPrefix, bool mirrored)
        {
            AnimatorState state = FindState(stateName);
            Assert.IsNotNull(state, $"{stateName} 상태가 없다");

            var tree = state.motion as BlendTree;
            Assert.IsNotNull(tree, $"{stateName}의 모션이 블렌드트리가 아니다");
            Assert.AreEqual(BlendTreeType.FreeformDirectional2D, tree.blendType);
            Assert.AreEqual("MoveX", tree.blendParameter);
            Assert.AreEqual("MoveZ", tree.blendParameterY);
            Assert.AreEqual(5, tree.children.Length, $"{stateName} BT는 중앙 + 축 4방 5개여야 한다");

            foreach (ChildMotion child in tree.children)
            {
                Assert.IsNotNull(child.motion, $"{stateName} BT에 빈 클립 자리가 있다");
                StringAssert.StartsWith(clipPrefix, child.motion.name,
                    $"{stateName} BT에 두손 클립이 아닌 것이 물려 있다");
                // 좌우 세트가 서로의 클립을 물면 손이 어긋난 채 조용히 재생된다.
                Assert.AreEqual(mirrored, child.motion.name.Contains("_LH"),
                    $"{stateName} BT의 '{child.motion.name}' 이 반대 손 세트의 클립이다");
            }
        }

        [Test]
        public void TwoHandStates_HaveFiveDirectionBlendTrees()
        {
            AssertTwoHandTree("HeroPoseTwoHand", "Mecha_Pose_HeroBrace", mirrored: false);
            AssertTwoHandTree("HeroPoseTwoHandLeft", "Mecha_Pose_HeroBrace", mirrored: true);
        }

        [Test]
        public void TwoHandStates_MirrorEachOtherDirectionByDirection()
        {
            // 미러 세트는 L/R 기능명이 교차한다 (원본 _R의 미러가 _LH_L — Docs/05 §10-B9 규약).
            // BT 좌표까지 맞물려야 왼손 두손 무기가 왼쪽으로 갈 때 왼쪽 포즈가 나온다.
            var right = FindState("HeroPoseTwoHand").motion as BlendTree;
            var left = FindState("HeroPoseTwoHandLeft").motion as BlendTree;

            foreach (ChildMotion rc in right.children)
            {
                bool found = false;
                foreach (ChildMotion lc in left.children)
                {
                    if (lc.position != rc.position)
                    {
                        continue;
                    }

                    found = true;
                    // 이름에서 _LH만 빼면 같은 방향의 오른손 클립이 되어야 한다.
                    Assert.AreEqual(rc.motion.name, lc.motion.name.Replace("_LH", string.Empty),
                        $"{rc.position} 지점의 좌우 클립이 같은 방향이 아니다");
                    break;
                }

                Assert.IsTrue(found, $"왼손 BT에 {rc.position} 지점이 없다 (오른손은 {rc.motion.name})");
            }
        }

        [Test]
        public void WeaponGrips_MatchDesignatedHandling()
        {
            var expected = new Dictionary<string, WeaponGrip>
            {
                { "gatling", WeaponGrip.OneHanded },
                { "laser_cannon", WeaponGrip.OneHanded },
                { "railgun", WeaponGrip.OneHanded },
                { "shotgun_cannon", WeaponGrip.TwoHanded },
                { "beam", WeaponGrip.TwoHanded },
            };

            string[] guids = AssetDatabase.FindAssets("t:WeaponData", new[] { WeaponsDir });
            Assert.Greater(guids.Length, 0, "무기 데이터를 찾지 못했다");

            var seen = new HashSet<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
                if (data == null)
                {
                    continue;
                }

                seen.Add(data.Id);
                WeaponGrip want = expected.TryGetValue(data.Id, out WeaponGrip g) ? g : WeaponGrip.None;
                Assert.AreEqual(want, data.Grip, $"'{data.Id}'의 파지 방식이 다르다 ({path})");
            }

            foreach (string id in expected.Keys)
            {
                Assert.IsTrue(seen.Contains(id), $"총 종류 무기 '{id}' 에셋이 사라졌다");
            }
        }
    }
}
