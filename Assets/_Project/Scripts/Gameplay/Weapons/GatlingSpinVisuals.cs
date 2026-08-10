using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 개틀링 배럴 스핀 연출 — 해당 무기의 WeaponFiredEvent 를 받는 동안 배럴 본을
    /// 로컬 Y축으로 가속 회전시키고, 사격이 멈추면 감속해 세운다.
    /// 연사 간격(쿨다운 0.12s) 사이 끊김은 유지창(_fireHoldSeconds)으로 흡수한다.
    /// 본을 스크립트로 직접 돌리므로 Animator·클립 배선이 필요 없다.
    /// </summary>
    public sealed class GatlingSpinVisuals : MonoBehaviour
    {
        [Tooltip("이 무기의 발사 이벤트에만 반응")]
        [SerializeField] private WeaponData _weapon;

        [Tooltip("스핀시킬 배럴 본 (로컬 Y = 스핀 축)")]
        [SerializeField] private Transform _barrels;

        [Tooltip("최대 회전 속도 (도/초)")]
        [SerializeField] private float _maxSpinSpeed = 900f;

        [Tooltip("정지 → 최대 속도 도달 시간(초)")]
        [SerializeField] private float _spinUpSeconds = 0.25f;

        [Tooltip("최대 속도 → 정지 시간(초)")]
        [SerializeField] private float _spinDownSeconds = 1.5f;

        [Tooltip("마지막 발사 후 사격 중으로 간주하는 시간(초) — 연사 간격보다 길게")]
        [SerializeField] private float _fireHoldSeconds = 0.3f;

        private Quaternion _restRotation;
        private bool _restCaptured;
        private float _speed;
        private float _angle;
        private float _lastFireTime;

        private void Awake()
        {
            if (_barrels != null)
            {
                _restRotation = _barrels.localRotation;
                _restCaptured = true;
            }
        }

        private void OnEnable()
        {
            // 마운트 표시 토글·풀 재사용마다 지나간 발사 기록과 관성을 버린다.
            _speed = 0f;
            _lastFireTime = float.NegativeInfinity;
            EventBus<WeaponFiredEvent>.Subscribe(OnWeaponFired);
        }

        private void OnDisable()
        {
            EventBus<WeaponFiredEvent>.Unsubscribe(OnWeaponFired);
        }

        private void OnWeaponFired(WeaponFiredEvent evt)
        {
            if (_weapon != null && evt.WeaponId == _weapon.Id)
            {
                _lastFireTime = Time.time;
            }
        }

        private void Update()
        {
            if (!_restCaptured)
            {
                return;
            }

            bool firing = Time.time - _lastFireTime <= _fireHoldSeconds;
            _speed = StepSpeed(_speed, firing, _maxSpinSpeed, _spinUpSeconds, _spinDownSeconds,
                Time.deltaTime);
            if (_speed <= 0f)
            {
                return;
            }

            _angle = Mathf.Repeat(_angle + _speed * Time.deltaTime, 360f);
            _barrels.localRotation = _restRotation * Quaternion.AngleAxis(_angle, Vector3.up);
        }

        /// <summary>
        /// 스핀 속도 한 스텝 — 사격 중이면 등가속으로 최대까지, 아니면 등감속으로 0까지.
        /// 도달 시간이 0 이하면 즉시 목표 속도. 정적 — EditMode 테스트 대상.
        /// </summary>
        public static float StepSpeed(
            float current, bool firing, float maxSpeed,
            float spinUpSeconds, float spinDownSeconds, float deltaTime)
        {
            if (firing)
            {
                float rate = spinUpSeconds > 0f ? maxSpeed / spinUpSeconds : float.MaxValue;
                return Mathf.Min(maxSpeed, current + rate * deltaTime);
            }

            float downRate = spinDownSeconds > 0f ? maxSpeed / spinDownSeconds : float.MaxValue;
            return Mathf.Max(0f, current - downRate * deltaTime);
        }
    }
}
