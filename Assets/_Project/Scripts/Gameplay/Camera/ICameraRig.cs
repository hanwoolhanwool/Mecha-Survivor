using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 카메라 계약: 시선 원점과 시선 방향만 노출한다.
    /// MechaAimer는 이 인터페이스만 알며, 현재 시점이 1인칭인지 3인칭인지 모른다.
    /// (GDD 2.4 — 조준은 시점에 종속되지 않는다)
    /// </summary>
    public interface ICameraRig
    {
        Vector3 AimOrigin { get; }
        Vector3 AimDirection { get; }
    }
}
