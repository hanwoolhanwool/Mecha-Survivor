using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 산탄 캐논 (GDD 3.4 무기 4번 — Burst). 근거리 원뿔 광역 — 탄 수·퍼짐은 WeaponData로.
    /// 강력한 반동으로 기체가 뒤로 밀린다 — 공중에서 후퇴기로 활용 가능.
    /// 명중한 적의 넉백은 ShotgunPellet이 처리한다.
    /// </summary>
    public sealed class ShotgunWeapon : ProjectileWeapon
    {
        [Header("반동 — 기체가 뒤로 밀린다")]
        [Tooltip("발사 순간 조준 반대 방향으로 받는 임펄스(m/s)")]
        [SerializeField] private float _recoilImpulse = 16f;

        private MechaController _controller;

        protected override void Fire(MechaAimer aimer)
        {
            base.Fire(aimer);

            // 런타임 장착(파츠 획득) 후 첫 발사에서 지연 결합 — 장착 시점엔 부모가 없다.
            if (_controller == null)
            {
                _controller = GetComponentInParent<MechaController>();
            }

            if (_controller != null && _recoilImpulse > 0f)
            {
                Vector3 direction = aimer != null
                    ? aimer.FireDirectionFrom(Muzzle.position)
                    : Muzzle.forward;
                _controller.AddImpulse(-direction * _recoilImpulse);
            }
        }
    }
}
