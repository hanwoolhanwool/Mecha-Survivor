using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 자율 서포트 드론 (GDD 3.4 무기 11번) — 유일한 자동 공격 요소.
    /// 사거리 내 가장 가까운 적에게 주기적으로 미니 빔을 쏜다.
    /// 위치는 SupportDroneRig가 관리하고, 이 컴포넌트는 사격만 담당한다.
    /// </summary>
    public sealed class SupportDrone : MonoBehaviour, IPoolable
    {
        public const string SourceId = "support_drone";

        [SerializeField] private float _range = 45f;
        [SerializeField] private float _fireInterval = 1.4f;
        [SerializeField] private float _damage = 6f;

        [Tooltip("빔 잔상 표시 시간(초)")]
        [SerializeField] private float _beamShowTime = 0.12f;

        [Tooltip("빔 지름")]
        [SerializeField] private float _beamWidth = 0.12f;

        [Tooltip("벽 뒤의 적은 쏘지 않는다 — Wall(13)")]
        [SerializeField] private LayerMask _losBlockMask = 1 << 13;

        [Tooltip("비우면 자식 'DroneBeam' 탐색")]
        [SerializeField] private Transform _beamVisual;

        private float _nextFireTime;
        private float _beamHideTime;

        private void Awake()
        {
            if (_beamVisual == null)
            {
                Transform found = transform.Find("DroneBeam");
                if (found != null)
                {
                    _beamVisual = found;
                }
            }

            HideBeam();
        }

        private void Update()
        {
            if (_beamVisual != null && _beamVisual.gameObject.activeSelf &&
                Time.time >= _beamHideTime)
            {
                HideBeam();
            }

            if (Time.time < _nextFireTime)
            {
                return;
            }

            EnemyBrain target = FindTarget();
            if (target == null)
            {
                return;
            }

            _nextFireTime = Time.time + _fireInterval;
            FireAt(target);
        }

        private EnemyBrain FindTarget()
        {
            var enemies = EnemyBrain.ActiveEnemies;
            EnemyBrain nearest = null;
            float nearestSqr = _range * _range;
            Vector3 self = transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBrain enemy = enemies[i];
                float sqr = (enemy.transform.position - self).sqrMagnitude;
                if (sqr >= nearestSqr)
                {
                    continue;
                }

                // 벽 뒤는 무시.
                if (Physics.Linecast(self, enemy.transform.position + Vector3.up * 0.8f,
                        _losBlockMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                nearestSqr = sqr;
                nearest = enemy;
            }

            return nearest;
        }

        private void FireAt(EnemyBrain target)
        {
            Vector3 targetPoint = target.transform.position + Vector3.up * 0.8f;
            Vector3 origin = transform.position;
            Vector3 direction = (targetPoint - origin).normalized;

            var health = target.GetComponent<Health>();
            if (health != null && health.IsAlive)
            {
                health.TakeDamage(_damage,
                    new DamageInfo(targetPoint, direction, false, SourceId));
                EventBus<DamageDealtEvent>.Raise(
                    new DamageDealtEvent(SourceId, _damage, targetPoint, !health.IsAlive));
            }

            ShowBeam(origin, targetPoint);
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void ShowBeam(Vector3 from, Vector3 to)
        {
            if (_beamVisual == null)
            {
                return;
            }

            Vector3 axis = to - from;
            float length = axis.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            _beamVisual.gameObject.SetActive(true);
            _beamVisual.SetPositionAndRotation(
                from + axis * 0.5f,
                Quaternion.LookRotation(axis / length) * Quaternion.Euler(90f, 0f, 0f));
            _beamVisual.localScale = new Vector3(_beamWidth, length * 0.5f, _beamWidth);
            _beamHideTime = Time.time + _beamShowTime;
        }

        private void HideBeam()
        {
            if (_beamVisual != null)
            {
                _beamVisual.gameObject.SetActive(false);
            }
        }

        public void OnSpawnedFromPool()
        {
            // 드론끼리 일제 사격하지 않도록 발사 시점을 흩뜨린다.
            _nextFireTime = Time.time + Random.Range(0f, _fireInterval);
        }

        public void OnReturnedToPool()
        {
            HideBeam();
        }
    }
}
