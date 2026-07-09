using System;

namespace MechaSurvivor.Core
{
    /// <summary>이벤트 마커. GC 부담을 줄이기 위해 readonly struct로 구현하는 것을 권장.</summary>
    public interface IEvent { }

    /// <summary>
    /// 타입 기반 정적 이벤트 버스. 시스템 간 직접 참조 없이 발행/구독으로 결합도를 낮춘다.
    /// <example>
    /// EventBus&lt;EnemyKilledEvent&gt;.Subscribe(OnEnemyKilled);
    /// EventBus&lt;EnemyKilledEvent&gt;.Raise(new EnemyKilledEvent(pos, exp));
    /// </example>
    /// 주의: Enter Play Mode Options에서 도메인 리로드를 끄면 정적 필드가 유지되므로,
    /// 부트스트랩에서 필요한 채널을 Clear() 하거나 구독 해제를 철저히 한다.
    /// </summary>
    public static class EventBus<T> where T : IEvent
    {
        private static Action<T> _handlers;

        public static void Subscribe(Action<T> handler) => _handlers += handler;

        public static void Unsubscribe(Action<T> handler) => _handlers -= handler;

        public static void Raise(in T evt) => _handlers?.Invoke(evt);

        public static void Clear() => _handlers = null;
    }
}
