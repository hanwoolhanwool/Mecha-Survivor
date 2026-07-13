using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 그래비티 웰 투척체 (GDD 3.4 무기 9번 — v1 우선순위 4, Control).
    /// 명중하거나 사거리를 소진한 지점에 중력장을 편다.
    /// 무기 쪽은 커스텀 클래스가 필요 없다 — ProjectileWeapon + 이 프리팹으로 성립한다.
    /// 강화 시 VisualScale이 커지며 장 반경도 함께 커진다.
    /// </summary>
    public sealed class GravityWellProjectile : Projectile
    {
        [SerializeField] private GravityWellField _fieldPrefab;

        [Header("중력장")]
        [SerializeField] private float _fieldRadius = 7f;
        [SerializeField] private float _fieldDuration = 3.5f;

        protected override void OnExpire(Vector3 position)
        {
            if (_fieldPrefab == null)
            {
                return;
            }

            var field = (GravityWellField)PoolManager.Instance.Spawn(
                _fieldPrefab, position, Quaternion.identity);

            // 무기 레벨(투척체 스케일)에 비례해 장 반경 확대 — 범위·지속 성장 (GDD Lv.5 방향).
            float scale = Mathf.Max(transform.localScale.x, 0.1f);
            field.Activate(_fieldRadius * scale, _fieldDuration);
        }
    }
}
