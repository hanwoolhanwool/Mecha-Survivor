using System.Collections;
using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 산탄 캐논 (GDD 3.4 무기 4번 — Burst). 근거리 원뿔 광역 — 탄 수·퍼짐은 WeaponData로.
    /// 강력한 반동으로 기체가 뒤로 밀린다 — 공중에서 후퇴기로 활용 가능.
    /// 명중한 적의 넉백은 ShotgunPellet이 처리한다.
    /// Lv.5: 일제 발사가 화염 방사 형태로 변한다 — 같은 탄수를 웨이브로 쪼개
    /// 짧게 연속 분사하고, 총구 앞에 화염 퍼프가 뿜어진다 (GDD 3.4-4).
    /// </summary>
    public sealed class ShotgunWeapon : ProjectileWeapon
    {
        [Header("반동 — 기체가 뒤로 밀린다")]
        [Tooltip("발사 순간 조준 반대 방향으로 받는 임펄스(m/s)")]
        [SerializeField] private float _recoilImpulse = 16f;

        [Header("Lv.5 — 화염 방사 형태 변화")]
        [SerializeField] private int _flameUnlockLevel = 5;

        [Tooltip("일제 발사를 이 수의 웨이브로 쪼개 연속 분사한다 — '펑'이 '화르륵'이 된다")]
        [SerializeField] private int _flameWaves = 4;

        [SerializeField] private float _flameWaveInterval = 0.06f;

        [Tooltip("웨이브마다 총구 앞에 뿜는 화염 퍼프 VFX (풀링)")]
        [SerializeField] private PooledVfx _flamePuffPrefab;

        [Tooltip("웨이브당 화염 퍼프 수")]
        [SerializeField] private int _puffsPerWave = 2;

        [Tooltip("퍼프 간 전방 간격(m) — 화염 줄기가 앞으로 뻗는다")]
        [SerializeField] private float _puffForwardSpacing = 2.2f;

        [Tooltip("퍼프 위치 랜덤 산포 반경(m)")]
        [SerializeField] private float _puffScatter = 0.7f;

        private MechaController _controller;

        protected override void Fire(MechaAimer aimer)
        {
            if (Level >= _flameUnlockLevel)
            {
                ApplyRecoil(aimer);
                StartCoroutine(FlameBurst(aimer));
                return;
            }

            base.Fire(aimer);
            ApplyRecoil(aimer);
        }

        /// <summary>같은 탄수를 웨이브로 나눠 분사 — 데미지 총량은 일제 발사와 동일하다.</summary>
        private IEnumerator FlameBurst(MechaAimer aimer)
        {
            int total = Mathf.Max(1, Data.GetProjectileCount(Level));
            var wait = new WaitForSeconds(_flameWaveInterval);

            for (int wave = 0; wave < _flameWaves; wave++)
            {
                int pellets = FlameBurstMath.PelletsInWave(total, _flameWaves, wave);
                for (int i = 0; i < pellets; i++)
                {
                    FireOne(aimer);
                }

                SpawnFlamePuffs(aimer);

                if (wave < _flameWaves - 1)
                {
                    yield return wait;
                }
            }
        }

        private void SpawnFlamePuffs(MechaAimer aimer)
        {
            if (_flamePuffPrefab == null)
            {
                return;
            }

            Vector3 direction = aimer != null
                ? aimer.FireDirectionFrom(Muzzle.position)
                : Muzzle.forward;

            for (int i = 0; i < _puffsPerWave; i++)
            {
                Vector3 position = Muzzle.position
                    + direction * (_puffForwardSpacing * (i + 1))
                    + Random.insideUnitSphere * _puffScatter;
                PoolManager.Instance.Spawn(_flamePuffPrefab, position, Quaternion.LookRotation(direction));
            }
        }

        private void ApplyRecoil(MechaAimer aimer)
        {
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
