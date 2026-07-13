using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 무기 베이스: 쿨다운 관리 + 발사 템플릿. 자원 검사는 없다 — 쿨다운이 전부다 (GDD 3.1).
    /// 실제 쿨다운은 CooldownModifier에게 묻는다 (단일 진실 공급원, GDD 8.3 ①).
    /// 발사 방식(투사체/빔/차징)은 파생 클래스가 Fire로 구현한다.
    /// </summary>
    public abstract class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponData _data;

        [Tooltip("총구. 비우면 자기 트랜스폼 사용")]
        [SerializeField] private Transform _muzzle;

        public WeaponData Data => _data;

        /// <summary>강화 레벨 (1~MaxLevel). 파츠 강화가 올린다.</summary>
        public int Level { get; private set; } = 1;

        private float _cooldownEndTime;
        private float _lastEffectiveCooldown = 1f;

        public bool IsReady => Time.time >= _cooldownEndTime;

        /// <summary>남은 쿨다운 비율 (1 = 방금 발사, 0 = 준비 완료). HUD 게이지가 읽는다.</summary>
        public float Cooldown01 =>
            IsReady ? 0f : Mathf.Clamp01((_cooldownEndTime - Time.time) / _lastEffectiveCooldown);

        protected Transform Muzzle => _muzzle != null ? _muzzle : transform;

        /// <summary>런타임 장착(업그레이드로 무기를 얻을 때) 지원.</summary>
        public void SetData(WeaponData data) => _data = data;

        public void SetLevel(int level) => Level = Mathf.Max(1, level);

        /// <summary>쿨다운 확인만 한다. 통과 시 발사하고 다음 쿨다운을 시작한다.</summary>
        public bool TryFire(MechaAimer aimer, CooldownModifier cooldowns)
        {
            if (_data == null || !IsReady)
            {
                return false;
            }

            Fire(aimer);

            float cooldown = cooldowns != null
                ? cooldowns.EffectiveCooldown(_data.Id, _data.BaseCooldown)
                : _data.BaseCooldown;
            cooldown *= CooldownScale;
            _lastEffectiveCooldown = Mathf.Max(cooldown, 0.0001f);
            _cooldownEndTime = Time.time + _lastEffectiveCooldown;

            EventBus<WeaponFiredEvent>.Raise(new WeaponFiredEvent(_data.Id));
            return true;
        }

        protected abstract void Fire(MechaAimer aimer);

        /// <summary>무기 고유 쿨다운 배율 훅 (개틀링 가속 회전 등). 1 = 변화 없음.</summary>
        protected virtual float CooldownScale => 1f;
    }
}
