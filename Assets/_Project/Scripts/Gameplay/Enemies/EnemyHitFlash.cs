using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 피격 화이트 플래시 — "맞았다"를 육안으로 읽게 하는 최소 피드백.
    /// MaterialPropertyBlock을 써서 머티리얼 인스턴스 생성(GC/배칭 파괴) 없이 색만 덮는다.
    /// 풀 재사용 시 플래시가 남지 않도록 OnEnable에서 복원한다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class EnemyHitFlash : MonoBehaviour
    {
        [Tooltip("비우면 자기 MeshRenderer 사용")]
        [SerializeField] private MeshRenderer _renderer;

        [SerializeField] private float _flashDuration = 0.08f;
        [SerializeField] private Color _flashColor = new(1f, 1f, 1f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Health _health;
        private MaterialPropertyBlock _block;
        private float _flashUntil;
        private bool _flashing;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
            }

            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            ClearFlash();
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
        }

        private void OnDamaged(float amount, DamageInfo info)
        {
            if (_renderer == null)
            {
                return;
            }

            _flashUntil = Time.time + _flashDuration;
            _flashing = true;
            _block.SetColor(BaseColorId, _flashColor);
            _renderer.SetPropertyBlock(_block);
        }

        private void Update()
        {
            if (_flashing && Time.time >= _flashUntil)
            {
                ClearFlash();
            }
        }

        private void ClearFlash()
        {
            _flashing = false;
            if (_renderer != null)
            {
                _renderer.SetPropertyBlock(null);
            }
        }
    }
}
