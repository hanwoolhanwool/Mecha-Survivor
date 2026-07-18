using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 시간 기반 전역 난이도 곡선. WaveData 규칙별 기본값 위에 곱해지는 배율이라
    /// 곡선만 만져도 런 전체의 압박감을 재빌드 없이 조정할 수 있다 (QA: SpawnLab 씬).
    /// 곡선 X축은 경과 '분', Y축은 배율. 곡선이 비어 있으면 배율 1로 동작한다.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyData", menuName = "Mecha Survivor/Difficulty Data")]
    public sealed class DifficultyData : ScriptableObject
    {
        [Header("전역 배율 곡선 — X: 경과 분, Y: 배율")]
        [Tooltip("스폰 간격을 이 값으로 나눈다. 2 = 두 배 자주 스폰")]
        public AnimationCurve SpawnRateByMinute = AnimationCurve.Linear(0f, 1f, 20f, 2.2f);

        [Tooltip("규칙별 MaxAlive에 곱한다. 1.8 = 동시 생존 상한 1.8배")]
        public AnimationCurve MaxAliveByMinute = AnimationCurve.Linear(0f, 1f, 20f, 1.8f);

        [Tooltip("적 최대 HP에 곱한다 — 후반 물량이 순삭되지 않게")]
        public AnimationCurve HealthByMinute = AnimationCurve.Linear(0f, 1f, 20f, 2.5f);

        [Header("무리(Burst) 스폰")]
        [Tooltip("무리 스폰 시 기준점 주변 흩뿌림 반경(m)")]
        [Min(0f)] public float BurstSpreadRadius = 5f;

        public float SpawnRateAt(float elapsedSeconds) =>
            Evaluate(SpawnRateByMinute, elapsedSeconds);

        public float MaxAliveMultiplierAt(float elapsedSeconds) =>
            Evaluate(MaxAliveByMinute, elapsedSeconds);

        public float HealthMultiplierAt(float elapsedSeconds) =>
            DifficultyMath.SafeHealthMultiplier(Evaluate(HealthByMinute, elapsedSeconds));

        private static float Evaluate(AnimationCurve curve, float elapsedSeconds)
        {
            if (curve == null || curve.length == 0)
            {
                return 1f;
            }

            return curve.Evaluate(elapsedSeconds / 60f);
        }
    }
}
