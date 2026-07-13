using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 플레이어 전용 체력. 피격 무적 시간(GDD §9-5 — 물량 게임 순삭 방지)을 갖고,
    /// 피격/사망을 EventBus로 알린다. 적 접촉·투사체는 IDamageable로만 접근한다.
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        [Tooltip("피격 후 무적 시간(초)")]
        [SerializeField] private float _invulnerabilityDuration = 0.5f;

        public float MaxHealth => _maxHealth;
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        private float _invulnerableUntil;

        private void Awake()
        {
            Current = _maxHealth;
        }

        public void AddMaxHealth(float amount, bool healBySameAmount = true)
        {
            _maxHealth += amount;
            if (healBySameAmount)
            {
                Current = Mathf.Min(Current + amount, _maxHealth);
            }

            EventBus<PlayerDamagedEvent>.Raise(new PlayerDamagedEvent(Current, _maxHealth));
        }

        public void TakeDamage(float amount, in DamageInfo info = default)
        {
            if (!IsAlive || Time.time < _invulnerableUntil)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - amount);
            _invulnerableUntil = Time.time + _invulnerabilityDuration;

            EventBus<PlayerDamagedEvent>.Raise(new PlayerDamagedEvent(Current, _maxHealth));

            if (Current <= 0f)
            {
                EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent(transform.position));
            }
        }
    }
}
