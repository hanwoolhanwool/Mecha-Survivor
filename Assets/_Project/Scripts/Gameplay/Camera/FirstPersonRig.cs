using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 1인칭(콕핏) 리그 — "정확함". 조준 로직 문제와 3인칭 카메라 문제를 분리하는
    /// 디버깅 기준선 역할이 크다 (GDD 2.4).
    /// </summary>
    public sealed class FirstPersonRig : CameraRig
    {
        [Tooltip("기체 기준 콕핏(눈) 위치. 요 회전을 따라 도는 로컬 오프셋")]
        [SerializeField] private Vector3 _cockpitOffset = new(0f, 1.6f, 0.35f);

        public override void Tick(Camera camera, Transform followTarget, float yaw, float pitch, float deltaTime)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            camera.transform.SetPositionAndRotation(
                followTarget.position + yawRotation * _cockpitOffset,
                Quaternion.Euler(pitch, yaw, 0f));
        }
    }
}
