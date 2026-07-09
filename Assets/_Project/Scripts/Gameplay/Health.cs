using System;
using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 체력 컴포넌트. Core의 IDamageable을 구현해 무기·투사체가 대상에 데미지를 준다.
    /// 풀에서 재사용되므로 OnEnable에서 체력을 리셋한다.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 10f;

        public float MaxHealth => _maxHealth;
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        /// <summary>사망 시 알림. 스포너/드롭 시스템이 구독해 처리.</summary>
        public event Action<Health> Died;

        /// <summary>데이터 기반 스폰 시 최대 체력을 주입.</summary>
        public void Init(float maxHealth)
        {
            _maxHealth = maxHealth;
            Current = maxHealth;
        }

        private void OnEnable() => Current = _maxHealth;

        public void TakeDamage(float amount, in DamageInfo info = default)
        {
            if (!IsAlive)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - amount);

            if (Current <= 0f)
            {
                Died?.Invoke(this);
            }
        }
    }
}
