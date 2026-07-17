using NUnit.Framework;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// SFX 재생 빈도 제한 검증 — 수백 마리 동시 사망 시 소리 도배·보이스 고갈을 막는 로직.
    /// </summary>
    public sealed class SfxThrottleTests
    {
        [Test]
        public void SameId_BlockedWithinMinInterval()
        {
            var throttle = new SfxThrottle(windowSeconds: 10f, windowBudget: 100);

            Assert.IsTrue(throttle.TryAcquire("gatling", 0.05f, now: 0f));
            Assert.IsFalse(throttle.TryAcquire("gatling", 0.05f, now: 0.03f),
                "minInterval 안의 재발음은 거부돼야 한다");
            Assert.IsTrue(throttle.TryAcquire("gatling", 0.05f, now: 0.06f));
        }

        [Test]
        public void DifferentIds_DoNotBlockEachOther()
        {
            var throttle = new SfxThrottle(windowSeconds: 10f, windowBudget: 100);

            Assert.IsTrue(throttle.TryAcquire("gatling", 1f, now: 0f));
            Assert.IsTrue(throttle.TryAcquire("beam", 1f, now: 0.01f),
                "다른 id는 서로의 minInterval에 걸리지 않는다");
        }

        [Test]
        public void WindowBudget_CapsTotalSoundsPerWindow()
        {
            var throttle = new SfxThrottle(windowSeconds: 0.05f, windowBudget: 3);

            // 대량 사망: 같은 순간 서로 다른 소리 요청 8건 → 예산 3건만 통과.
            int granted = 0;
            for (int i = 0; i < 8; i++)
            {
                if (throttle.TryAcquire($"sfx_{i}", 0f, now: 0.01f))
                {
                    granted++;
                }
            }

            Assert.AreEqual(3, granted);
        }

        [Test]
        public void WindowBudget_ResetsAfterWindowElapses()
        {
            var throttle = new SfxThrottle(windowSeconds: 0.05f, windowBudget: 1);

            Assert.IsTrue(throttle.TryAcquire("a", 0f, now: 0f));
            Assert.IsFalse(throttle.TryAcquire("b", 0f, now: 0.01f));
            Assert.IsTrue(throttle.TryAcquire("b", 0f, now: 0.06f),
                "윈도우가 지나면 예산이 초기화돼야 한다");
        }

        [Test]
        public void TimeRewind_ClearsStateInsteadOfDeadlocking()
        {
            // 플레이 모드 재시작 등으로 시간이 되감기면 이전 기록이 재생을 막으면 안 된다.
            var throttle = new SfxThrottle(windowSeconds: 0.05f, windowBudget: 8);

            Assert.IsTrue(throttle.TryAcquire("gatling", 5f, now: 100f));
            Assert.IsTrue(throttle.TryAcquire("gatling", 5f, now: 1f),
                "시간이 되감기면 상태를 버리고 허용해야 한다");
        }
    }
}
