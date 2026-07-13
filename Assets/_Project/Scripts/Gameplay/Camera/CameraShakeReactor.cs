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

        private void OnEnable()
        {
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
            EventBus<WeaponFiredEvent>.Subscribe(OnWeaponFired);
        }

        private void OnDisable()
        {
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
            EventBus<WeaponFiredEvent>.Unsubscribe(OnWeaponFired);
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
    }
}
