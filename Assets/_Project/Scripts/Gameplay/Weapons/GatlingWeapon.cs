using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 개틀링 (GDD 3.4 무기 1번 — v1 우선순위 3, Sustain). 로테이션의 바닥.
    /// 홀드 연사(WeaponData.HoldToFire) 중 열이 쌓여 연사가 빨라지고, 멈추면 식는다.
    /// 가속 회전은 해금 레벨(기본 Lv.5)부터 — "개틀링의 로망".
    /// </summary>
    public sealed class GatlingWeapon : ProjectileWeapon
    {
        [Header("예열 (스핀업)")]
        [SerializeField] private float _heatPerShot = 0.06f;
        [SerializeField] private float _heatDecayPerSecond = 0.8f;

        [Tooltip("최대 예열 시 쿨다운 배율 (0.4 = 연사 2.5배)")]
        [SerializeField] private float _maxSpinUpScale = 0.4f;

        [Tooltip("이 레벨부터 가속 회전 발동")]
        [SerializeField] private int _spinUpUnlockLevel = 5;

        private float _heat;
        private float _lastShotTime;

        /// <summary>현재 예열 (0~1). 총열 회전 연출(MechaVisuals)이 읽는다.</summary>
        public float Heat => _heat;

        protected override float CooldownScale =>
            Level >= _spinUpUnlockLevel ? GatlingMath.CooldownScale(_heat, _maxSpinUpScale) : 1f;

        protected override void FireOne(MechaAimer aimer)
        {
            base.FireOne(aimer);
            _heat = GatlingMath.AddHeat(_heat, _heatPerShot);
            _lastShotTime = Time.time;
        }

        private void Update()
        {
            // 마지막 발사 직후에는 식지 않는다 — 홀드 유지 중 열 보존.
            if (Time.time - _lastShotTime > 0.15f && _heat > 0f)
            {
                _heat = GatlingMath.DecayHeat(_heat, _heatDecayPerSecond, Time.deltaTime);
            }
        }
    }
}
