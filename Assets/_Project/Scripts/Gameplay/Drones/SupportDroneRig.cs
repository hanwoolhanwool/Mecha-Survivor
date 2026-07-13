using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 서포트 드론 편대 관리 — 플레이어 등 뒤 호(arc) 대형으로 부드럽게 따라다닌다.
    /// "등 뒤"의 기준은 카메라 요 — 조준 반대편에 떠서 시야를 가리지 않는다 (GDD 3.6 규칙 1).
    /// </summary>
    public sealed class SupportDroneRig : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private SupportDrone _dronePrefab;
        [SerializeField] private CameraDirector _camera;

        [Header("대형")]
        [SerializeField] private float _radius = 2.8f;
        [SerializeField] private float _height = 1.8f;
        [SerializeField] private float _arcDegrees = 140f;

        [Tooltip("따라붙는 속도 — 낮을수록 둥실거린다 (시각 전용, 판정 무관)")]
        [SerializeField] private float _followResponse = 7f;

        private readonly List<SupportDrone> _drones = new();

        public int DroneCount => _drones.Count;

        /// <summary>드론 업그레이드가 호출. 편대에 n기 추가.</summary>
        public void AddDrones(int count)
        {
            if (_dronePrefab == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var drone = (SupportDrone)PoolManager.Instance.Spawn(
                    _dronePrefab, transform.position + Vector3.up * _height, Quaternion.identity);
                _drones.Add(drone);
            }
        }

        private void LateUpdate()
        {
            if (_drones.Count == 0)
            {
                return;
            }

            float yaw = _camera != null ? _camera.Yaw : transform.eulerAngles.y;
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            float t = 1f - Mathf.Exp(-_followResponse * Time.deltaTime);

            for (int i = 0; i < _drones.Count; i++)
            {
                Vector3 offset = DroneFormation.LocalOffset(
                    i, _drones.Count, _radius, _height, _arcDegrees);
                Vector3 target = transform.position + yawRotation * offset;
                Transform droneTransform = _drones[i].transform;
                droneTransform.position = Vector3.Lerp(droneTransform.position, target, t);
            }
        }
    }
}
