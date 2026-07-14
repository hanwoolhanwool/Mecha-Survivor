using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// EMP 투척체 (GDD 3.4 무기 10번). 명중/사거리 소진 지점에 EMP 필드를 편다 —
    /// 그래비티 웰 투척체와 같은 패턴. 무기 쪽은 ProjectileWeapon 그대로 쓴다.
    /// 체인 감전은 Lv.5 해금이라 ConfigureFromWeapon으로 레벨을 받는다.
    /// </summary>
    public sealed class EmpProjectile : Projectile
    {
        [SerializeField] private EmpField _fieldPrefab;

        [Header("EMP 필드")]
        [SerializeField] private float _fieldRadius = 9f;
        [SerializeField] private float _fieldDuration = 4f;

        [Header("Lv.5 — 체인 감전")]
        [SerializeField] private int _chainUnlockLevel = 5;

        private bool _chainEnabled;
        private float _chainDamage;

        public override void ConfigureFromWeapon(WeaponData data, int level)
        {
            _chainEnabled = level >= _chainUnlockLevel;
            _chainDamage = data.GetDamage(level);
        }

        protected override void OnExpire(Vector3 position)
        {
            if (_fieldPrefab == null)
            {
                return;
            }

            var field = (EmpField)PoolManager.Instance.Spawn(
                _fieldPrefab, position, Quaternion.identity);

            // 무기 레벨(투척체 스케일)에 비례해 필드 반경 확대 — 그래비티 웰과 같은 성장.
            float scale = Mathf.Max(transform.localScale.x, 0.1f);
            field.Activate(_fieldRadius * scale, _fieldDuration, _chainEnabled, _chainDamage, SourceId);
        }

        public override void OnReturnedToPool()
        {
            base.OnReturnedToPool();
            _chainEnabled = false;
            _chainDamage = 0f;
        }
    }
}
