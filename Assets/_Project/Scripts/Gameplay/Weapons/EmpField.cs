using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// EMP 필드 (GDD 3.4 무기 10번 — Control). 지속 시간 동안 반경 내 적에게
    /// 틱마다 이동 슬로우 + 사격 봉쇄를 건다. Lv.5부터 감전된 적끼리 전기가 연쇄(체인)한다.
    /// 그래비티 웰과 같은 필드 패턴 — 상황을 만드는 무기, 피해는 체인만 준다.
    /// </summary>
    public sealed class EmpField : MonoBehaviour, IPoolable
    {
        [Header("감전 상태")]
        [SerializeField] private float _tickInterval = 0.4f;

        [Tooltip("감전 중 이동 속도 배율 (0.4 = 60% 슬로우)")]
        [SerializeField] private float _slowFactor = 0.4f;

        [Tooltip("틱 1회가 부여하는 상태 지속(초) — 틱 간격보다 길어야 끊기지 않는다")]
        [SerializeField] private float _statusDuration = 0.7f;

        [Header("Lv.5 — 체인 감전")]
        [Tooltip("체인 최대 고리 수 (시작점 포함)")]
        [SerializeField] private int _chainMaxLinks = 5;

        [Tooltip("고리 간 최대 거리(m)")]
        [SerializeField] private float _chainLinkRadius = 9f;

        [Tooltip("체인 아크 시각 — 푸른 전기 그물 (풀링)")]
        [SerializeField] private LineFlashVfx _arcVfxPrefab;

        [SerializeField] private float _arcWidth = 0.12f;

        [Header("연출")]
        [Tooltip("테두리 전기 링이 최대 반경으로 전개되는 시간(초) — 기폭 파편 연출이 먼저 읽히도록 천천히 큰다. 판정 반경은 즉시 적용된다.")]
        [SerializeField] private float _visualGrowDuration = 0.6f;

        [Tooltip("필드 테두리를 도는 전기 링 시각")]
        [SerializeField] private ElectricRingVfx _ringVfx;

        private float _radius;
        private float _activateTime;
        private bool _growDone;
        private float _endTime;
        private float _nextTickTime;
        private bool _chainEnabled;
        private float _chainDamage;
        private string _sourceId;
        private bool _active;

        // 재사용 버퍼 — 틱마다의 힙 할당 방지.
        private readonly List<EnemyBrain> _affected = new(64);
        private readonly List<Vector3> _affectedPositions = new(64);
        private readonly List<int> _chain = new(8);

        /// <summary>
        /// 전개 연출의 스케일 배수 (0~1). ease-in 큐빅 — 초반에는 작게 머물러
        /// 기폭 파편이 먼저 읽히고, 후반에 확 퍼진다. 순수 로직(테스트용).
        /// </summary>
        public static float VisualGrowFactor(float elapsed, float growDuration)
        {
            if (growDuration <= 0f)
            {
                return 1f;
            }

            float t = Mathf.Clamp01(elapsed / growDuration);
            return t * t * t;
        }

        public void Activate(float radius, float duration, bool chainEnabled, float chainDamage,
            string sourceId)
        {
            _radius = radius;
            _activateTime = Time.time;
            _endTime = Time.time + duration;
            _nextTickTime = 0f; // 첫 프레임에 즉시 1틱.
            _chainEnabled = chainEnabled;
            _chainDamage = chainDamage;
            _sourceId = sourceId;
            _active = true;
            _growDone = false;
            if (_ringVfx != null)
            {
                _ringVfx.SetRadius(radius * VisualGrowFactor(0f, _visualGrowDuration));
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (Time.time >= _endTime)
            {
                _active = false;
                PoolManager.Instance.Despawn(this);
                return;
            }

            // 비주얼만 성장 — 판정은 Activate 시점부터 _radius 전체로 돈다.
            if (!_growDone)
            {
                float factor = VisualGrowFactor(Time.time - _activateTime, _visualGrowDuration);
                if (_ringVfx != null)
                {
                    _ringVfx.SetRadius(_radius * factor);
                }
                _growDone = factor >= 1f;
            }

            if (Time.time < _nextTickTime)
            {
                return;
            }

            _nextTickTime = Time.time + _tickInterval;
            Tick();
        }

        private void Tick()
        {
            _affected.Clear();
            _affectedPositions.Clear();

            Vector3 center = transform.position;
            float radiusSqr = _radius * _radius;
            IReadOnlyList<EnemyBrain> enemies = EnemyBrain.ActiveEnemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBrain enemy = enemies[i];
                if ((enemy.transform.position - center).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                enemy.ApplyEmp(_slowFactor, _statusDuration);
                _affected.Add(enemy);
                _affectedPositions.Add(enemy.transform.position);
            }

            if (_chainEnabled && _affected.Count >= 2)
            {
                ChainLightning();
            }
        }

        /// <summary>Lv.5 — 중심에서 가장 가까운 적부터 이웃으로 전기가 옮겨붙는다.</summary>
        private void ChainLightning()
        {
            int start = FindNearestToCenter();
            int length = EmpChainMath.BuildChain(
                _affectedPositions, start, _chainLinkRadius, _chainMaxLinks, _chain);

            for (int i = 0; i < length; i++)
            {
                EnemyBrain enemy = _affected[_chain[i]];
                var health = enemy.GetComponent<Health>();
                if (health == null || !health.IsAlive)
                {
                    continue;
                }

                Vector3 position = enemy.transform.position;
                health.TakeDamage(_chainDamage,
                    new DamageInfo(position, Vector3.zero, false, _sourceId));
                EventBus<DamageDealtEvent>.Raise(new DamageDealtEvent(
                    _sourceId, _chainDamage, position, !health.IsAlive));

                // 아크 시각 — 이전 고리에서 이번 고리로.
                if (i > 0 && _arcVfxPrefab != null)
                {
                    var arc = (LineFlashVfx)PoolManager.Instance.Spawn(
                        _arcVfxPrefab, position, Quaternion.identity);
                    arc.Show(_affectedPositions[_chain[i - 1]], position, _arcWidth);
                }
            }
        }

        private int FindNearestToCenter()
        {
            Vector3 center = transform.position;
            int nearest = 0;
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < _affectedPositions.Count; i++)
            {
                float sqr = (_affectedPositions[i] - center).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = i;
                }
            }

            return nearest;
        }

        public void OnSpawnedFromPool() { }

        public void OnReturnedToPool()
        {
            _active = false;
            _affected.Clear();
            _affectedPositions.Clear();
        }
    }
}
