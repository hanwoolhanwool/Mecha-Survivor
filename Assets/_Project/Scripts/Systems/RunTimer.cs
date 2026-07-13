using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 20분 런 진행 타이머 (GDD 5.1). 시간 도달 = 클리어, 플레이어 사망 = 실패.
    /// 두 경우 모두 RunEndedEvent를 정확히 1회 발행한다.
    /// 3택 중 일시정지는 Time.timeScale=0으로 처리되며 deltaTime 기반이라 자동 반영된다.
    /// </summary>
    public sealed class RunTimer : MonoBehaviour
    {
        [Tooltip("런 길이(초). 20분 = 1200")]
        [SerializeField] private float _runDuration = 1200f;

        public float Elapsed { get; private set; }
        public float Duration => _runDuration;
        public float Remaining => Mathf.Max(0f, _runDuration - Elapsed);
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            IsRunning = true;
        }

        private void OnDisable()
        {
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<RunTimer>();
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            Elapsed += Time.deltaTime;

            if (Elapsed >= _runDuration)
            {
                Elapsed = _runDuration;
                EndRun(victory: true);
            }
        }

        private void OnPlayerDied(PlayerDiedEvent _)
        {
            EndRun(victory: false);
        }

        private void EndRun(bool victory)
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            EventBus<RunEndedEvent>.Raise(new RunEndedEvent(victory, Elapsed));
        }
    }
}
