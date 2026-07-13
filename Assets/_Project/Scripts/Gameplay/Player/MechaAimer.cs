using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 조준: 화면 중앙 크로스헤어(카메라 정면) 레이캐스트 → 월드 목표점.
    /// ICameraRig에게 시선 원점·방향만 묻는다 — 시점이 1인칭인지 3인칭인지 모른다 (GDD 2.4).
    /// 발사 판정은 언제나 이 조준선 기준이며, 상체 메시 회전(표현)과 무관하다 (GDD 2.3).
    /// </summary>
    [DefaultExecutionOrder(60)]
    public sealed class MechaAimer : MonoBehaviour
    {
        [Tooltip("ICameraRig 공급자 (CameraDirector)")]
        [SerializeField] private CameraDirector _cameraRig;

        [SerializeField] private float _maxAimDistance = 500f;

        [Tooltip("Player(8)·투사체(10,11)·Pickup(12) 제외 — 자기 자신/아군 탄을 조준하지 않는다")]
        [SerializeField] private LayerMask _aimMask = ~((1 << 8) | (1 << 10) | (1 << 11) | (1 << 12));

        /// <summary>크로스헤어가 가리키는 월드 지점. 무기는 총구에서 이 점을 향해 쏜다.</summary>
        public Vector3 AimPoint { get; private set; }

        /// <summary>조준선에 걸린 대상이 있는지 (없으면 AimPoint는 최대 거리 지점).</summary>
        public bool HasHit { get; private set; }

        public Collider HitCollider { get; private set; }

        private ICameraRig _rig;

        private void Awake()
        {
            _rig = _cameraRig;
        }

        private void LateUpdate()
        {
            if (_rig == null)
            {
                return;
            }

            Vector3 origin = _rig.AimOrigin;
            Vector3 direction = _rig.AimDirection;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, _maxAimDistance,
                    _aimMask, QueryTriggerInteraction.Ignore))
            {
                AimPoint = hit.point;
                HasHit = true;
                HitCollider = hit.collider;
            }
            else
            {
                AimPoint = origin + direction * _maxAimDistance;
                HasHit = false;
                HitCollider = null;
            }
        }

        /// <summary>총구 위치에서 조준점을 향하는 발사 방향.</summary>
        public Vector3 FireDirectionFrom(Vector3 muzzlePosition)
        {
            Vector3 to = AimPoint - muzzlePosition;
            return to.sqrMagnitude > 0.0001f ? to.normalized : transform.forward;
        }
    }
}
