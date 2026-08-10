using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 발사 모션 배선 가드 (Docs/05 §10-B11) — 산탄 캐논 반동 킥 + 펌프 재장전.
    /// 전신 포즈(PoseType)와 달리 모션은 **무기**가 정하고 손은 PoseType이 정하므로,
    /// 코드(GetFireMotion)·컨트롤러 상태·클립 셋 세 군데가 어긋나면 조용히 동작이 사라진다.
    /// </summary>
    public sealed class WeaponFireMotionTests
    {
        private const string ControllerPath = "Assets/_Project/Art/Models/Mecha/AC_Mecha.controller";
        private const string PoseLayer = "WeaponPose";
        private const string MotionParam = "FireMotion";
        private const string MotionTrigger = "FireMotionTrigger";
        private const string ClipPrefix = "Mecha_Fire_ShotgunPump";

        private AnimatorController _controller;
        private AnimatorStateMachine _poseMachine;

        [SetUp]
        public void SetUp()
        {
            _controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(_controller, $"컨트롤러를 찾지 못했다: {ControllerPath}");

            _poseMachine = null;
            foreach (AnimatorControllerLayer layer in _controller.layers)
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

        private static AnimatorStateTransition FindTransition(AnimatorState from, string toName)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.destinationState != null && transition.destinationState.name == toName)
                {
                    return transition;
                }
            }

            return null;
        }

        private static bool HasCondition(AnimatorStateTransition transition, string parameter,
            AnimatorConditionMode mode, int threshold)
        {
            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == parameter && condition.mode == mode
                    && (int)condition.threshold == threshold)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void GetFireMotion_OnlyShotgunPumps()
        {
            Assert.AreEqual(MechaAnimParams.FireMotionShotgunPump,
                MechaAnimParams.GetFireMotion("shotgun_cannon"));

            // 대출력 빔은 같은 두손 무기지만 지속 빔이라 펌프 동작이 붙으면 안 된다.
            Assert.AreEqual(MechaAnimParams.FireMotionNone, MechaAnimParams.GetFireMotion("beam"));
            Assert.AreEqual(MechaAnimParams.FireMotionNone, MechaAnimParams.GetFireMotion("gatling"));
            Assert.AreEqual(MechaAnimParams.FireMotionNone, MechaAnimParams.GetFireMotion("railgun"));
            Assert.AreEqual(MechaAnimParams.FireMotionNone, MechaAnimParams.GetFireMotion("laser_cannon"));
            Assert.AreEqual(MechaAnimParams.FireMotionNone, MechaAnimParams.GetFireMotion("missile_pod"));
        }

        [Test]
        public void GetFireMotion_UnknownWeapon_DoesNotFallBack()
        {
            // 반동 그룹(GetFireGroup)은 Light로 폴백하지만 모션은 폴백하면 안 된다 —
            // 신무기가 남의 펌프 동작을 물려받으면 총을 안 든 채 슬라이드를 당긴다.
            Assert.AreEqual(MechaAnimParams.FireMotionNone, MechaAnimParams.GetFireMotion("no_such_weapon"));
            Assert.AreEqual(MechaAnimParams.FireMotionNone, MechaAnimParams.GetFireMotion(null));
        }

        [Test]
        public void Controller_HasFireMotionParameters()
        {
            AnimatorControllerParameterType? motionType = null;
            AnimatorControllerParameterType? triggerType = null;
            foreach (AnimatorControllerParameter parameter in _controller.parameters)
            {
                if (parameter.name == MotionParam)
                {
                    motionType = parameter.type;
                }
                else if (parameter.name == MotionTrigger)
                {
                    triggerType = parameter.type;
                }
            }

            Assert.AreEqual(AnimatorControllerParameterType.Int, motionType, $"'{MotionParam}' 파라미터가 없다");
            Assert.AreEqual(AnimatorControllerParameterType.Trigger, triggerType, $"'{MotionTrigger}' 파라미터가 없다");
        }

        /// <summary>발사 모션 BT — 두손 포즈와 같은 중앙 + 축 4방 구성이어야 방향이 이어진다.</summary>
        private void AssertMotionTree(string stateName, bool mirrored)
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
                StringAssert.StartsWith(ClipPrefix, child.motion.name,
                    $"{stateName} BT에 발사 모션이 아닌 클립이 물려 있다");
                Assert.AreEqual(mirrored, child.motion.name.Contains("_LH"),
                    $"{stateName} BT의 '{child.motion.name}' 이 반대 그립 세트의 클립이다");

                var clip = child.motion as AnimationClip;
                Assert.IsNotNull(clip, $"'{child.motion.name}' 이 AnimationClip이 아니다");
                Assert.IsFalse(clip.isLooping,
                    $"'{clip.name}' 이 루프로 임포트됐다 — 발사 모션은 단발이라 Exit Time 복귀가 깨진다");
                Assert.AreEqual(0.9f, clip.length, 0.02f,
                    $"'{clip.name}' 길이가 승인 타임라인(0.9초)과 다르다");
            }
        }

        [Test]
        public void ShotgunFireStates_HaveFiveDirectionBlendTrees()
        {
            AssertMotionTree("ShotgunFire", mirrored: false);
            AssertMotionTree("ShotgunFireLeft", mirrored: true);
        }

        [Test]
        public void ShotgunFireStates_MirrorEachOtherDirectionByDirection()
        {
            // 미러 세트는 L/R 기능명이 교차한다 (원본 _R의 미러가 _LH_L — Docs/05 §10-B9 규약).
            var right = FindState("ShotgunFire").motion as BlendTree;
            var left = FindState("ShotgunFireLeft").motion as BlendTree;

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
                    Assert.AreEqual(rc.motion.name, lc.motion.name.Replace("_LH", string.Empty),
                        $"{rc.position} 지점의 좌우 그립 클립이 같은 방향이 아니다");
                    break;
                }

                Assert.IsTrue(found, $"왼그립 BT에 {rc.position} 지점이 없다 (오른그립은 {rc.motion.name})");
            }
        }

        [Test]
        public void BraceStates_EnterFireMotionOnTrigger()
        {
            // 브레이스 유지 중 발사 — 트리거 + 모션 코드가 맞아야 들어간다.
            var cases = new[]
            {
                new { From = "HeroPoseTwoHand", To = "ShotgunFire" },
                new { From = "HeroPoseTwoHandLeft", To = "ShotgunFireLeft" },
            };

            foreach (var c in cases)
            {
                AnimatorState from = FindState(c.From);
                Assert.IsNotNull(from, $"{c.From} 상태가 없다");
                AnimatorStateTransition transition = FindTransition(from, c.To);
                Assert.IsNotNull(transition, $"{c.From} → {c.To} 전환이 없다");
                Assert.IsFalse(transition.hasExitTime, $"{c.From} → {c.To} 는 즉시 반응해야 한다");
                Assert.IsTrue(HasCondition(transition, MotionTrigger, AnimatorConditionMode.If, 0),
                    $"{c.From} → {c.To} 에 {MotionTrigger} 조건이 없다");
                Assert.IsTrue(HasCondition(transition, MotionParam, AnimatorConditionMode.Equals,
                        MechaAnimParams.FireMotionShotgunPump),
                    $"{c.From} → {c.To} 의 {MotionParam} 값이 코드 상수와 다르다");

                // 포즈 이탈 전환보다 앞에 있어야 트리거가 먹는다 (전환 배열은 앞에서부터 평가된다).
                Assert.AreSame(transition, from.transitions[0],
                    $"{c.From} 의 발사 모션 전환이 첫 번째가 아니다 — 포즈 이탈에 가로채인다");
            }
        }

        [Test]
        public void Empty_EntersFireMotionDirectly_WithMatchingGripHand()
        {
            // 포즈 유지창이 닫힌 뒤의 첫 발도 브레이스를 거치지 않고 바로 모션으로 들어가야
            // 0.12초 늦게 반동이 나오는 일이 없다. 손은 PoseType 이 가른다.
            AnimatorState empty = FindState("Empty");
            Assert.IsNotNull(empty, "Empty 상태가 없다");

            var cases = new[]
            {
                new { To = "ShotgunFire", Pose = MechaAnimParams.FirePoseHeroTwoHand },
                new { To = "ShotgunFireLeft", Pose = MechaAnimParams.FirePoseHeroTwoHandLeft },
            };

            foreach (var c in cases)
            {
                AnimatorStateTransition transition = FindTransition(empty, c.To);
                Assert.IsNotNull(transition, $"Empty → {c.To} 전환이 없다");
                Assert.IsTrue(HasCondition(transition, MotionTrigger, AnimatorConditionMode.If, 0),
                    $"Empty → {c.To} 에 {MotionTrigger} 조건이 없다");
                Assert.IsTrue(HasCondition(transition, MotionParam, AnimatorConditionMode.Equals,
                        MechaAnimParams.FireMotionShotgunPump),
                    $"Empty → {c.To} 의 {MotionParam} 값이 코드 상수와 다르다");
                Assert.IsTrue(HasCondition(transition, "PoseType", AnimatorConditionMode.Equals, c.Pose),
                    $"Empty → {c.To} 가 그립 손(PoseType {c.Pose})을 가리지 않는다");
            }
        }

        [Test]
        public void FireMotionStates_ReturnToBraceAndBailOutOnPoseEnd()
        {
            var cases = new[]
            {
                new { State = "ShotgunFire", Brace = "HeroPoseTwoHand", Pose = MechaAnimParams.FirePoseHeroTwoHand },
                new { State = "ShotgunFireLeft", Brace = "HeroPoseTwoHandLeft", Pose = MechaAnimParams.FirePoseHeroTwoHandLeft },
            };

            foreach (var c in cases)
            {
                AnimatorState state = FindState(c.State);
                Assert.IsNotNull(state, $"{c.State} 상태가 없다");

                AnimatorStateTransition back = FindTransition(state, c.Brace);
                Assert.IsNotNull(back, $"{c.State} → {c.Brace} 복귀 전환이 없다");
                Assert.IsTrue(back.hasExitTime, $"{c.State} 는 끝까지 재생하고 복귀해야 한다");
                Assert.AreEqual(1f, back.exitTime, 1e-4f, $"{c.State} 복귀가 클립 끝이 아니다");

                AnimatorStateTransition bail = FindTransition(state, "Empty");
                Assert.IsNotNull(bail, $"{c.State} → Empty 이탈 전환이 없다");
                Assert.IsTrue(HasCondition(bail, "PoseType", AnimatorConditionMode.NotEqual, c.Pose),
                    $"{c.State} 가 포즈 유지창 종료를 못 따라간다");
                Assert.AreSame(bail, state.transitions[0],
                    $"{c.State} 의 이탈 전환이 첫 번째가 아니다 — Exit Time 복귀에 가로채인다");
            }
        }
    }
}
