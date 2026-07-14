using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 반경 폭발 피해 공통 처리 (미사일·클러스터·궤도 폭격·레일건 Lv.5).
    /// 물리 오버랩 대신 활성 적 레지스트리를 순회한다 — 수백 개체 전제의 저비용 판정.
    /// </summary>
    public static class AreaDamage
    {
        /// <summary>반경 내 모든 생존 적에게 피해를 준다. 판정은 코드가, 화려함은 VFX가 (GDD 3.6-7).</summary>
        public static void Apply(Vector3 center, float radius, float damage, string sourceId)
        {
            if (radius <= 0f || damage <= 0f)
            {
                return;
            }

            float radiusSqr = radius * radius;
            var enemies = EnemyBrain.ActiveEnemies;

            // 역순: TakeDamage → 사망 → 리스트 제거가 일어나도 안전하게.
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyBrain enemy = enemies[i];
                if ((enemy.transform.position - center).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                var damageable = enemy.GetComponent<Health>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                Vector3 direction = (enemy.transform.position - center).normalized;
                damageable.TakeDamage(damage,
                    new DamageInfo(enemy.transform.position, direction, false, sourceId));
                EventBus<DamageDealtEvent>.Raise(new DamageDealtEvent(
                    sourceId, damage, enemy.transform.position, !damageable.IsAlive));
            }
        }
    }
}
