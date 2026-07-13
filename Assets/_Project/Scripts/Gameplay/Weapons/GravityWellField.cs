using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 그래비티 웰 중력장 (GDD 3.4 무기 9번). 지속 시간 동안 반경 내 적을 중심으로 끌어모은다.
    /// 뭉친 무리에 미사일/빔을 꽂는 콤보의 핵심 — 다른 무기의 화려함을 증폭시키는 무기.
    /// 포탑(고정형)은 끌지 않는다.
    /// </summary>
    public sealed class GravityWellField : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _pullSpeed = 9f;

        private float _radius;
        private float _endTime;
        private bool _active;

        public void Activate(float radius, float duration)
        {
            _radius = radius;
            _endTime = Time.time + duration;
            _active = true;
            transform.localScale = Vector3.one * (radius * 2f);
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

            Vector3 center = transform.position;
            float radiusSqr = _radius * _radius;
            float pull = _pullSpeed * Time.deltaTime;

            IReadOnlyList<EnemyBrain> enemies = EnemyBrain.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBrain enemy = enemies[i];
                if (enemy.Data == null || enemy.Data.Archetype == EnemyArchetype.Turret)
                {
                    continue;
                }

                Vector3 toCenter = center - enemy.transform.position;
                if (toCenter.sqrMagnitude > radiusSqr || toCenter.sqrMagnitude < 0.25f)
                {
                    continue;
                }

                enemy.transform.position += toCenter.normalized * pull;
            }
        }

        public void OnSpawnedFromPool() { }

        public void OnReturnedToPool()
        {
            _active = false;
            transform.localScale = Vector3.one;
        }
    }
}
