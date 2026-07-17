using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 이벤트 → 카메라 셰이크 연동 (GDD 2.4 — 피격/대형 무기 발사 시 짧은 흔들림).
    /// 전투 코드는 카메라의 존재를 모른다 — 이벤트만 구독한다.
    /// 강도는 CameraDynamics의 셰이크 강도 슬라이더(0~1)를 그대로 존중한다.
    /// </summary>
    public sealed class CameraShakeReactor : MonoBehaviour
    {
        [SerializeField] private CameraDynamics _dynamics;

        [Header("피격")]
        [SerializeField] private float _damagedShake = 0.35f;

        [Header("대형 무기 발사 (무기 ID → 셰이크)")]
        [SerializeField] private string[] _heavyWeaponIds = { "beam", "missile_pod" };
        [SerializeField] private float _heavyFireShake = 0.18f;

        [Header("카메라 킥 (무기 ID → 실제 발사 순간 위로 튐) — 레일건 전용 연출")]
        [Tooltip("WeaponDischarged(차징 완료 후 실탄이 나가는 순간)에 반응한다")]
        [SerializeField] private string[] _kickWeaponIds = { "railgun" };
        [SerializeField] private float _kickPitchDegrees = 2.5f;
        [SerializeField] private float _kickShake = 0.12f;

        [Header("대형 착탄 (궤도 폭격 등 HeavyImpact)")]
        [SerializeField] private float _heavyImpactShake = 0.45f;

        private void OnEnable()
        {
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
            EventBus<WeaponFiredEvent>.Subscribe(OnWeaponFired);
            EventBus<WeaponDischargedEvent>.Subscribe(OnWeaponDischarged);
            EventBus<HeavyImpactEvent>.Subscribe(OnHeavyImpact);
        }

        private void OnDisable()
        {
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
            EventBus<WeaponFiredEvent>.Unsubscribe(OnWeaponFired);
            EventBus<WeaponDischargedEvent>.Unsubscribe(OnWeaponDischarged);
            EventBus<HeavyImpactEvent>.Unsubscribe(OnHeavyImpact);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            if (_dynamics != null)
            {
                _dynamics.AddShake(_damagedShake);
            }
        }

        private void OnWeaponFired(WeaponFiredEvent evt)
        {
            if (_dynamics == null || string.IsNullOrEmpty(evt.WeaponId))
            {
                return;
            }

            for (int i = 0; i < _heavyWeaponIds.Length; i++)
            {
                if (_heavyWeaponIds[i] == evt.WeaponId)
                {
                    _dynamics.AddShake(_heavyFireShake);
                    return;
                }
            }
        }

        private void OnWeaponDischarged(WeaponDischargedEvent evt)
        {
            if (_dynamics == null || string.IsNullOrEmpty(evt.WeaponId))
            {
                return;
            }

            for (int i = 0; i < _kickWeaponIds.Length; i++)
            {
                if (_kickWeaponIds[i] == evt.WeaponId)
                {
                    _dynamics.AddKick(_kickPitchDegrees);
                    _dynamics.AddShake(_kickShake);
                    return;
                }
            }
        }

        private void OnHeavyImpact(HeavyImpactEvent evt)
        {
            if (_dynamics != null)
            {
                _dynamics.AddShake(_heavyImpactShake * Mathf.Clamp01(evt.Magnitude));
            }
        }
    }
}
