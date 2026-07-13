using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 무기 가동률·지상/공중 체류를 주기 샘플링해 통계에 밀어넣는다 (GDD 6장).
    /// 통계 시스템은 이벤트/샘플만 받는다 — 전투 코드는 여전히 통계를 모른다.
    /// </summary>
    public sealed class RunTelemetryReporter : MonoBehaviour
    {
        [SerializeField] private WeaponSlots _weaponSlots;
        [SerializeField] private MechaController _controller;

        [Tooltip("샘플 간격(초)")]
        [SerializeField] private float _sampleInterval = 0.5f;

        private float _nextSampleTime;

        private void Update()
        {
            if (Time.time < _nextSampleTime)
            {
                return;
            }

            _nextSampleTime = Time.time + _sampleInterval;

            if (!ServiceLocator.TryGet(out StatisticsRecorder recorder))
            {
                return;
            }

            if (ServiceLocator.TryGet(out RunTimer timer) && !timer.IsRunning)
            {
                return;
            }

            if (_controller != null)
            {
                recorder.Current.RecordAltitudeSample(_controller.IsGrounded);
            }

            if (_weaponSlots != null)
            {
                for (int i = 0; i < WeaponSlots.MaxSlots; i++)
                {
                    Weapon weapon = _weaponSlots.GetWeapon(i);
                    if (weapon != null && weapon.Data != null)
                    {
                        recorder.Current.RecordUptimeSample(weapon.Data.Id, weapon.IsReady);
                    }
                }
            }
        }
    }
}
