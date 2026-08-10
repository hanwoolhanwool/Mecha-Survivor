using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 필드 경계를 감싸는 구형 전기 케이지 시각. 가닥마다 기울어진 대원(great circle)이
    /// 각자의 축으로 천천히 공전하고, 링 위를 흐르는 파동으로 일렁인다.
    /// 무작위 리롤 없이 시간 연속 함수만 써서 프레임 간 움직임이 부드럽다.
    /// 좌표 생성은 ElectricRingMath, 버퍼는 Awake에서 한 번만 할당한다.
    /// </summary>
    public sealed class ElectricRingVfx : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] _strands;

        [Tooltip("링 폴리라인의 분할 수 — 많을수록 부드럽다")]
        [SerializeField] private int _segments = 96;

        [Tooltip("반경 대비 일렁임 진폭 비율")]
        [SerializeField] private float _radialJitterFraction = 0.04f;

        [Tooltip("링 평면에서 위아래로 일렁이는 진폭 (월드 단위)")]
        [SerializeField] private float _verticalJitter = 0.3f;

        [Tooltip("가닥이 구 표면을 도는 공전 속도 범위 (도/초)")]
        [SerializeField] private float _orbitSpeedMin = 20f;
        [SerializeField] private float _orbitSpeedMax = 55f;

        [Tooltip("일렁임 파동의 시간 배속")]
        [SerializeField] private float _waveSpeed = 2.2f;

        [Tooltip("선 폭 숨쉬기 진폭 (기본 폭 대비 비율)")]
        [SerializeField] private float _widthBreath = 0.12f;

        private Vector3[] _buffer;
        private float[] _baseWidths;
        private Quaternion[] _baseOrientations;
        private Vector3[] _orbitAxes;
        private float[] _orbitSpeeds;
        private float[] _phases;
        private float _radius;

        private void Awake()
        {
            _buffer = new Vector3[_segments];
            var rng = new System.Random(GetHashCode());

            int count = _strands != null ? _strands.Length : 0;
            _baseWidths = new float[count];
            _baseOrientations = new Quaternion[count];
            _orbitAxes = new Vector3[count];
            _orbitSpeeds = new float[count];
            _phases = new float[count];

            for (int i = 0; i < count; i++)
            {
                _baseWidths[i] = _strands[i].widthMultiplier;
                _strands[i].positionCount = _segments;
                _strands[i].loop = true;
                _strands[i].useWorldSpace = false;

                _baseOrientations[i] = ElectricRingMath.RandomRingOrientation(rng);
                _orbitAxes[i] = ElectricRingMath.RandomUnitVector(rng);
                _orbitSpeeds[i] = Mathf.Lerp(
                    _orbitSpeedMin, _orbitSpeedMax, (float)rng.NextDouble());
                _phases[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
            }
        }

        /// <summary>링 반경 갱신 — 실제 좌표 갱신은 Update가 매 프레임 한다.</summary>
        public void SetRadius(float radius)
        {
            _radius = radius;
        }

        private void Update()
        {
            float time = Time.time;
            float waveTime = time * _waveSpeed;
            float radialAmplitude = _radius * _radialJitterFraction;

            for (int i = 0; i < _baseWidths.Length; i++)
            {
                Quaternion orientation =
                    Quaternion.AngleAxis(_orbitSpeeds[i] * time, _orbitAxes[i]) *
                    _baseOrientations[i];
                ElectricRingMath.FillFlowingRing(_buffer, _radius, radialAmplitude,
                    _verticalJitter, waveTime, _phases[i], orientation);
                _strands[i].SetPositions(_buffer);
                _strands[i].widthMultiplier = _baseWidths[i] *
                    (1f + _widthBreath * Mathf.Sin(3f * time + _phases[i]));
            }
        }
    }
}
