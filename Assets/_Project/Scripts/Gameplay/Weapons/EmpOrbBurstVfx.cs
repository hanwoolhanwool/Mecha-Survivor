using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// EMP 오브 기폭 연출. 중심 몸체(_core)는 제자리에서 줄어들며 사라지고,
    /// 주변 파츠(_fragments)는 각자의 방사 방향으로 퍼지며 회전·소멸한다.
    /// 곡선 계산은 EmpOrbBurstMath, 재생은 풀 경유 (GDD 3.6 규칙 5).
    /// </summary>
    public sealed class EmpOrbBurstVfx : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _duration = 0.6f;

        [Tooltip("파츠가 오브 중심에서 퍼져 나가는 거리 (로컬 단위)")]
        [SerializeField] private float _spreadDistance = 2.5f;

        [Tooltip("파츠 축소가 시작되는 정규화 시점 (0~1)")]
        [SerializeField] private float _fragmentShrinkStart = 0.55f;

        [Tooltip("중심 몸체가 완전히 사라지는 정규화 시점 (0~1)")]
        [SerializeField] private float _coreShrinkEnd = 0.45f;

        [Tooltip("파츠 텀블 회전 속도 (도/초)")]
        [SerializeField] private float _tumbleSpeed = 360f;

        [SerializeField] private Transform _core;
        [SerializeField] private Transform[] _fragments;

        private Vector3 _rootRestScale;
        private Vector3 _coreRestScale;
        private Vector3[] _restPositions;
        private Quaternion[] _restRotations;
        private Vector3[] _restScales;
        private Vector3[] _directions;
        private Vector3[] _tumbleAxes;
        private float _startTime;

        private void Awake()
        {
            _rootRestScale = transform.localScale;
            _coreRestScale = _core != null ? _core.localScale : Vector3.one;

            int count = _fragments != null ? _fragments.Length : 0;
            _restPositions = new Vector3[count];
            _restRotations = new Quaternion[count];
            _restScales = new Vector3[count];
            _directions = new Vector3[count];
            _tumbleAxes = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Transform frag = _fragments[i];
                _restPositions[i] = frag.localPosition;
                _restRotations[i] = frag.localRotation;
                _restScales[i] = frag.localScale;
                _directions[i] = EmpOrbBurstMath.SpreadDirection(frag.localPosition, Vector3.up);

                Vector3 axis = Vector3.Cross(_directions[i], Vector3.up);
                _tumbleAxes[i] = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.right;
            }
        }

        private void OnEnable()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            float t = (Time.time - _startTime) / _duration;
            if (t >= 1f)
            {
                PoolManager.Instance.Despawn(this);
                return;
            }

            if (_core != null)
            {
                _core.localScale = _coreRestScale * EmpOrbBurstMath.CoreScale(t, _coreShrinkEnd);
            }

            float elapsed = t * _duration;
            float fragScale = EmpOrbBurstMath.FragmentScale(t, _fragmentShrinkStart);
            for (int i = 0; i < _restPositions.Length; i++)
            {
                Transform frag = _fragments[i];
                frag.localPosition = EmpOrbBurstMath.FragmentPosition(
                    _restPositions[i], _directions[i], _spreadDistance, t);
                frag.localRotation =
                    Quaternion.AngleAxis(_tumbleSpeed * elapsed, _tumbleAxes[i]) * _restRotations[i];
                frag.localScale = _restScales[i] * fragScale;
            }
        }

        public void OnSpawnedFromPool() { }

        public void OnReturnedToPool()
        {
            // 스폰 측이 덮어쓴 루트 스케일·연출 도중의 파츠 트랜스폼을 원상 복구.
            transform.localScale = _rootRestScale;
            if (_core != null)
            {
                _core.localScale = _coreRestScale;
            }

            for (int i = 0; i < _restPositions.Length; i++)
            {
                Transform frag = _fragments[i];
                frag.localPosition = _restPositions[i];
                frag.localRotation = _restRotations[i];
                frag.localScale = _restScales[i];
            }
        }
    }
}
