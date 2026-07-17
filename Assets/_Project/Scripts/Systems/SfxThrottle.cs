using System.Collections.Generic;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// SFX 재생 빈도 제한 (순수 로직 — EditMode 테스트 대상).
    /// 수백 마리가 동시에 죽는 게임이므로 소리를 걸러내지 않으면 귀가 터지고 보이스가 고갈된다.
    /// 두 겹 제한: ① 같은 id는 minInterval 안에 재발음 금지, ② 윈도우당 전체 발음 수 상한.
    /// </summary>
    public sealed class SfxThrottle
    {
        private readonly Dictionary<string, float> _lastPlayTime = new();
        private readonly float _windowSeconds;
        private readonly int _windowBudget;

        private float _windowStart = float.NegativeInfinity;
        private int _windowCount;

        public SfxThrottle(float windowSeconds = 0.05f, int windowBudget = 8)
        {
            _windowSeconds = windowSeconds;
            _windowBudget = windowBudget;
        }

        /// <summary>재생 허가 여부. 허가 시 해당 id의 마지막 재생 시각을 갱신한다.</summary>
        public bool TryAcquire(string id, float minInterval, float now)
        {
            // 시간이 되감기면(플레이 모드 재시작 등) 상태를 버린다.
            if (now < _windowStart)
            {
                Clear();
            }

            if (now - _windowStart >= _windowSeconds)
            {
                _windowStart = now;
                _windowCount = 0;
            }

            if (_windowCount >= _windowBudget)
            {
                return false;
            }

            if (_lastPlayTime.TryGetValue(id, out float last) && now - last < minInterval)
            {
                return false;
            }

            _lastPlayTime[id] = now;
            _windowCount++;
            return true;
        }

        public void Clear()
        {
            _lastPlayTime.Clear();
            _windowStart = float.NegativeInfinity;
            _windowCount = 0;
        }
    }
}
