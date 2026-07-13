using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 쿨감의 단일 진실 공급원 (GDD 8.3 설계 요점 ①).
    /// 무기는 자기 쿨다운이 얼마나 줄었는지 스스로 계산하지 않고 여기에 묻는다.
    /// 쿨감 출처(업그레이드·착지·조합품)가 늘어나도 Weapon 코드는 바뀌지 않는다.
    /// </summary>
    public sealed class CooldownModifier : MonoBehaviour
    {
        [SerializeField] private MechaController _mecha;

        [Tooltip("착지·보행 중 쿨다운 가속 배율 (GDD 2.2 — 지상 = 화력, 공중 = 안전). §9-1은 1.5 시작 권장")]
        [SerializeField] private float _groundedMultiplier = 1.5f;

        private float _globalReduction;
        private readonly Dictionary<string, float> _perWeaponReduction = new();

        /// <summary>누적 전역 쿨감 (나눗셈 모델의 분모 항. 1.0 = +100%).</summary>
        public float GlobalReduction => _globalReduction;

        public float GroundedMultiplier => _groundedMultiplier;

        public bool IsGroundedBonusActive => _mecha != null && _mecha.IsGrounded;

        public void AddGlobal(float amount) => _globalReduction += amount;

        public void AddForWeapon(string weaponId, float amount)
        {
            _perWeaponReduction.TryGetValue(weaponId, out float current);
            _perWeaponReduction[weaponId] = current + amount;
        }

        public void AddGroundedMultiplier(float amount) => _groundedMultiplier += amount;

        public float ReductionFor(string weaponId)
        {
            _perWeaponReduction.TryGetValue(weaponId, out float perWeapon);
            return _globalReduction + perWeapon;
        }

        /// <summary>현재 상태(쿨감 누적 + 접지 여부)를 반영한 실제 쿨다운.</summary>
        public float EffectiveCooldown(string weaponId, float baseCooldown)
        {
            float grounded = IsGroundedBonusActive ? _groundedMultiplier : 1f;
            return CooldownMath.Effective(baseCooldown, ReductionFor(weaponId), grounded);
        }
    }
}
