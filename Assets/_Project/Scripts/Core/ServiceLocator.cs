using System;
using System.Collections.Generic;

namespace MechaSurvivor.Core
{
    /// <summary>
    /// 경량 서비스 로케이터. 전역 시스템(풀, 오디오, 세이브 등)을 등록/조회한다.
    /// 무거운 DI 프레임워크 대신 초기 개발 속도를 위한 최소 구현이며,
    /// 필요 시 VContainer/Zenject 등으로 대체할 수 있도록 사용처를 이 API에 국한한다.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        public static void Register<T>(T service) where T : class
        {
            Services[typeof(T)] = service;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var obj))
            {
                return (T)obj;
            }

            throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
        }

        public static void Unregister<T>() where T : class => Services.Remove(typeof(T));

        public static void Clear() => Services.Clear();
    }
}
