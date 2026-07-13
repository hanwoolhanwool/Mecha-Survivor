using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 카메라 배치 전략의 베이스. CameraDirector가 활성 리그의 Tick만 호출한다.
    /// 리그는 카메라 트랜스폼을 어디에 둘지만 결정한다 — 조준 계약(ICameraRig)은
    /// CameraDirector가 카메라 트랜스폼으로 대신 이행한다.
    /// </summary>
    public abstract class CameraRig : MonoBehaviour
    {
        /// <summary>활성화 순간 호출. 스냅이 필요한 내부 상태(지연 위치 등)를 리셋한다.</summary>
        public virtual void OnActivated(Camera camera, Transform followTarget) { }

        /// <summary>매 프레임(LateUpdate) 카메라 배치.</summary>
        public abstract void Tick(Camera camera, Transform followTarget, float yaw, float pitch, float deltaTime);
    }
}
