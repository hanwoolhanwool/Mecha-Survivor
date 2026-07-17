using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 메카 기계음 — 표현 레이어 (GDD 2.3: "로봇다움의 절반은 소리다").
    /// MechaVisuals와 같은 원칙: 컨트롤러 상태를 읽기만 하고 게임플레이에 0의 영향.
    /// 스러스터 루프의 볼륨·피치가 속도를 따라가고, 대시 순간 휙 소리를 낸다.
    /// </summary>
    public sealed class MechaAudio : MonoBehaviour
    {
        [Header("참조 (읽기 전용)")]
        [SerializeField] private MechaController _controller;

        [Header("클립 (SfxAssetGenerator가 구운 WAV)")]
        [SerializeField] private AudioClip _thrusterLoop;
        [SerializeField] private AudioClip _dashClip;

        [Header("스러스터 루프")]
        [Range(0f, 1f)]
        [SerializeField] private float _thrusterVolume = 0.3f;

        [Tooltip("이 속도에서 볼륨·피치가 최대에 도달")]
        [SerializeField] private float _speedReference = 25f;

        [SerializeField] private float _pitchMin = 0.85f;
        [SerializeField] private float _pitchMax = 1.3f;

        [Tooltip("착지 중 스러스터 볼륨 배율 (스러스터는 꺼지고 잔열만)")]
        [Range(0f, 1f)]
        [SerializeField] private float _groundedVolumeScale = 0.15f;

        [Tooltip("볼륨·피치 추종 속도 — 표현 레이어이므로 얼마든지 보간해도 된다")]
        [SerializeField] private float _followResponse = 6f;

        [Header("대시")]
        [Range(0f, 1f)]
        [SerializeField] private float _dashVolume = 0.7f;

        private AudioSource _loopSource;
        private AudioSource _oneShotSource;
        private float _currentVolume;
        private float _currentPitch = 1f;
        private bool _wasDashing;

        private void Awake()
        {
            _loopSource = CreateSource("ThrusterLoop");
            _loopSource.loop = true;
            _oneShotSource = CreateSource("MechaOneShot");
        }

        private void Start()
        {
            if (_thrusterLoop != null)
            {
                _loopSource.clip = _thrusterLoop;
                _loopSource.volume = 0f;
            }
        }

        private void Update()
        {
            if (_controller == null)
            {
                return;
            }

            // 자가 복구: 첫 프레임의 오디오 시스템 워밍업이나 보이스 스틸로
            // 루프가 드롭돼도 다음 프레임에 다시 건다.
            if (_loopSource.clip != null && !_loopSource.isPlaying)
            {
                _loopSource.Play();
            }

            float speedRatio = Mathf.Clamp01(
                _controller.Velocity.magnitude / Mathf.Max(_speedReference, 0.01f));

            // 정지 호버 중에도 낮게 깔리고, 속도를 낼수록 커진다.
            float targetVolume = _thrusterVolume * (0.3f + 0.7f * speedRatio);
            if (_controller.IsGrounded)
            {
                targetVolume *= _groundedVolumeScale;
            }

            float targetPitch = Mathf.Lerp(_pitchMin, _pitchMax, speedRatio);

            float t = 1f - Mathf.Exp(-_followResponse * Time.deltaTime);
            _currentVolume = Mathf.Lerp(_currentVolume, targetVolume, t);
            _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, t);
            _loopSource.volume = _currentVolume;
            _loopSource.pitch = _currentPitch;

            // 대시 시작 순간에만 1회 발음.
            bool dashing = _controller.IsDashing;
            if (dashing && !_wasDashing && _dashClip != null)
            {
                _oneShotSource.PlayOneShot(_dashClip, _dashVolume);
            }

            _wasDashing = dashing;
        }

        private AudioSource CreateSource(string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }
    }
}
