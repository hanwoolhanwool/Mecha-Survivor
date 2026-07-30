using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// RigLab 전용 궤도 카메라 — 우클릭 드래그 회전 / 휠 줌 (Docs/06 §4.1).
    /// 조정 부위 프레이밍이 목적이라 본편 카메라 리그를 쓰지 않는다.
    /// </summary>
    public sealed class RigLabOrbitCamera : MonoBehaviour
    {
        [SerializeField] private float _distance = 7f;
        [SerializeField] private float _minDistance = 1.5f;
        [SerializeField] private float _maxDistance = 25f;

        [Tooltip("피벗 높이 — 대상 루트 기준 (기체 몸통 높이)")]
        [SerializeField] private float _pivotHeight = 1.5f;

        [SerializeField] private float _rotateSensitivity = 0.25f;
        [SerializeField] private float _zoomSensitivity = 0.01f;

        private Transform _target;
        private float _yaw = 180f;
        private float _pitch = 15f;

        public void SetTarget(Transform target) => _target = target;

        private void LateUpdate()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    _yaw += delta.x * _rotateSensitivity;
                    _pitch = Mathf.Clamp(_pitch - delta.y * _rotateSensitivity, -80f, 80f);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0f)
                {
                    _distance = Mathf.Clamp(
                        _distance - scroll * _zoomSensitivity * _distance,
                        _minDistance, _maxDistance);
                }
            }

            Vector3 pivot = (_target != null ? _target.position : Vector3.zero)
                + Vector3.up * _pivotHeight;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * _distance, rotation);
        }
    }
}
