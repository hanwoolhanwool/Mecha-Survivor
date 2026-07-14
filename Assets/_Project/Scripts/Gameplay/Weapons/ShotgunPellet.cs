using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 산탄 캐논 펠릿 — 맞은 적이 뒤로 날아간다 (GDD 3.4-4 넉백).
    /// 적은 물리 없이 transform으로 움직이므로 넉백도 위치 변위로 처리한다.
    /// 포탑(고정형)은 밀리지 않는다.
    /// </summary>
    public sealed class ShotgunPellet : Projectile
    {
        [Header("넉백")]
        [Tooltip("명중 시 적이 밀려나는 거리(m)")]
        [SerializeField] private float _knockbackDistance = 1.6f;

        protected override void OnImpact(in RaycastHit hit)
        {
            var enemy = hit.collider.GetComponentInParent<EnemyBrain>();
            if (enemy != null && enemy.Data != null &&
                enemy.Data.Archetype != EnemyArchetype.Turret && _knockbackDistance > 0f)
            {
                Vector3 push = Velocity.normalized * _knockbackDistance;
                enemy.transform.position += push;
            }

            base.OnImpact(hit);
        }
    }
}
