using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 3인칭 카메라 역동 연출(FOV·뱅킹·피치 오프셋·셰이크)의 목표값 계산과 상태 보간.
    /// 모든 효과는 개별 강도(0~1)로 조절 가능하며, 전부 0이면 정적인 카메라가 된다 (GDD 2.4).
    /// 계산은 순수 정적 함수로 분리해 EditMode 테스트로 검증한다.
    /// </summary>
    public sealed class CameraDynamics : MonoBehaviour
    {
        [Header("강도 (0 = 끔) — 플레이하며 조정한다")]
        [Range(0f, 1f)] [SerializeField] private float _fovIntensity = 1f;
        [Range(0f, 1f)] [SerializeField] private float _bankIntensity = 1f;
        [Range(0f, 1f)] [SerializeField] private float _pitchIntensity = 1f;
        [Range(0f, 1f)] [SerializeField] private float _lagIntensity = 1f;
        [Range(0f, 1f)] [SerializeField] private float _shakeIntensity = 1f;

        [Header("속도 기반 FOV")]
        [SerializeField] private float _baseFov = 60f;
        [SerializeField] private float _maxFov = 75f;
        [SerializeField] private float _fovResponse = 6f;

        [Header("뱅킹 (좌우 이동 시 롤)")]
        [SerializeField] private float _maxBankAngle = 10f;
        [SerializeField] private float _bankResponse = 8f;

        [Header("상승/하강 피치 오프셋")]
        [SerializeField] private float _maxPitchOffset = 6f;
        [SerializeField] private float _pitchResponse = 6f;

        [Header("스프링 암 지연")]
        [Tooltip("클수록 빨리 따라붙는다. 지연은 카메라에만 적용 — 기체는 이미 움직였다")]
        [SerializeField] private float _lagResponse = 14f;

        [Header("셰이크")]
        [SerializeField] private float _shakeFrequency = 28f;
        [SerializeField] private float _shakeDamping = 6f;

        public float BaseFov => _baseFov;

        private float _currentFov;
        private float _currentBank;
        private float _currentPitchOffset;
        private float _shakeAmplitude;
        private float _shakeSeed;

        private void Awake()
        {
            _currentFov = _baseFov;
            _shakeSeed = Random.value * 100f;
        }

        /// <summary>피격·대형 무기 발사 시 호출. 진폭은 감쇠로 자연 소멸한다.</summary>
        public void AddShake(float amplitude)
        {
            _shakeAmplitude = Mathf.Max(_shakeAmplitude, amplitude * _shakeIntensity);
        }

        /// <summary>매 프레임 상태 보간. speed01 = 현재 수평 속도 / 최고 속도.</summary>
        public void Tick(float speed01, float lateralInput, float verticalInput, float deltaTime)
        {
            _currentFov = Smooth(_currentFov,
                TargetFov(_baseFov, _maxFov, speed01, _fovIntensity), _fovResponse, deltaTime);
            _currentBank = Smooth(_currentBank,
                TargetBank(lateralInput, _maxBankAngle, _bankIntensity), _bankResponse, deltaTime);
            _currentPitchOffset = Smooth(_currentPitchOffset,
                TargetPitchOffset(verticalInput, _maxPitchOffset, _pitchIntensity), _pitchResponse, deltaTime);
            _shakeAmplitude = Mathf.Max(0f, _shakeAmplitude - _shakeAmplitude * _shakeDamping * deltaTime);
        }

        public float CurrentFov => _currentFov;
        public float CurrentBank => _currentBank;
        public float CurrentPitchOffset => _currentPitchOffset;

        /// <summary>지연 강도를 반영한 위치 추적 계수. 강도 0이면 즉시 따라붙는다(지연 없음).</summary>
        public float EffectiveLagResponse =>
            Mathf.Lerp(1000f, _lagResponse, Mathf.Clamp01(_lagIntensity));

        /// <summary>펄린 노이즈 기반 셰이크 오프셋 (짧고 부드러운 흔들림).</summary>
        public Vector3 EvaluateShakeOffset()
        {
            if (_shakeAmplitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            float t = Time.time * _shakeFrequency;
            return new Vector3(
                (Mathf.PerlinNoise(_shakeSeed, t) - 0.5f) * 2f,
                (Mathf.PerlinNoise(_shakeSeed + 17f, t) - 0.5f) * 2f,
                0f) * _shakeAmplitude;
        }

        // ── 순수 계산 (EditMode 테스트 대상) ─────────────────────────────

        public static float TargetFov(float baseFov, float maxFov, float speed01, float intensity)
        {
            float target = Mathf.Lerp(baseFov, maxFov, Mathf.Clamp01(speed01));
            return Mathf.Lerp(baseFov, target, Mathf.Clamp01(intensity));
        }

        /// <summary>우측 이동(+x) 시 음의 롤(우측으로 기울임). GDD 2.4 뱅킹.</summary>
        public static float TargetBank(float lateralInput, float maxAngle, float intensity)
        {
            return -Mathf.Clamp(lateralInput, -1f, 1f) * maxAngle * Mathf.Clamp01(intensity);
        }

        /// <summary>상승(+1) 시 음의 피치 오프셋(카메라가 올려다봄). GDD 2.4.</summary>
        public static float TargetPitchOffset(float verticalInput, float maxOffset, float intensity)
        {
            return -Mathf.Clamp(verticalInput, -1f, 1f) * maxOffset * Mathf.Clamp01(intensity);
        }

        /// <summary>프레임레이트 독립 지수 보간.</summary>
        public static float Smooth(float current, float target, float response, float deltaTime)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-response * deltaTime));
        }
    }
}
