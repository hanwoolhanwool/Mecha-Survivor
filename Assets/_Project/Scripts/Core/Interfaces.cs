using UnityEngine;

namespace MechaSurvivor.Core
{
    /// <summary>데미지를 받을 수 있는 대상(플레이어, 적, 파괴 가능 오브젝트 등).</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount, in DamageInfo info = default);
    }

    /// <summary>피격 부가 정보(타격 지점, 넉백 방향, 크리티컬 여부).</summary>
    public readonly struct DamageInfo
    {
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitDirection;
        public readonly bool IsCritical;

        public DamageInfo(Vector3 hitPoint, Vector3 hitDirection, bool isCritical = false)
        {
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            IsCritical = isCritical;
        }
    }

    /// <summary>오브젝트 풀에서 재사용될 때 호출되는 콜백. 상태 초기화/정리에 사용.</summary>
    public interface IPoolable
    {
        void OnSpawnedFromPool();
        void OnReturnedToPool();
    }
}
