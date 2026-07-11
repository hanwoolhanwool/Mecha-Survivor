using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 타입 기반 정적 이벤트 버스의 발행/구독/해제 계약을 검증하는 스모크 테스트.
    /// 정적 상태가 테스트 간 누수되지 않도록 각 테스트 후 채널을 Clear한다.
    /// </summary>
    public sealed class EventBusTests
    {
        [TearDown]
        public void TearDown() => EventBus<EnemyKilledEvent>.Clear();

        [Test]
        public void Raise_InvokesSubscribedHandler_WithPayload()
        {
            int received = 0;
            int exp = 0;
            void Handler(EnemyKilledEvent e) { received++; exp = e.ExpReward; }

            EventBus<EnemyKilledEvent>.Subscribe(Handler);
            EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(Vector3.zero, 42));

            Assert.AreEqual(1, received, "구독 핸들러가 정확히 1회 호출되어야 한다.");
            Assert.AreEqual(42, exp, "이벤트 페이로드가 그대로 전달되어야 한다.");
        }

        [Test]
        public void Unsubscribe_StopsFurtherDelivery()
        {
            int received = 0;
            void Handler(EnemyKilledEvent e) => received++;

            EventBus<EnemyKilledEvent>.Subscribe(Handler);
            EventBus<EnemyKilledEvent>.Unsubscribe(Handler);
            EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(Vector3.zero, 1));

            Assert.AreEqual(0, received, "해제된 핸들러는 호출되지 않아야 한다.");
        }

        [Test]
        public void Raise_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(Vector3.zero, 0)));
        }

        [Test]
        public void MultipleSubscribers_AllReceive()
        {
            int a = 0, b = 0;
            void HandlerA(EnemyKilledEvent _) => a++;
            void HandlerB(EnemyKilledEvent _) => b++;

            EventBus<EnemyKilledEvent>.Subscribe(HandlerA);
            EventBus<EnemyKilledEvent>.Subscribe(HandlerB);
            EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(Vector3.zero, 0));

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }
    }
}
