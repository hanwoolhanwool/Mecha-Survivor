using UnityEngine;

namespace MechaSurvivor.Systems
{
    /// <summary>업적 판정 기준. 임계값 비교는 AchievementEvaluator가 수행한다.</summary>
    public enum AchievementMetric
    {
        SurvivalSeconds,    // 단일 런 생존 시간 ≥ Threshold
        KillsInRun,         // 단일 런 처치 수 ≥ Threshold
        TotalKills,         // 평생 누적 처치 ≥ Threshold
        ReachLevel,         // 단일 런 도달 레벨 ≥ Threshold
        MaxSingleHit,       // 단일 런 최고 한 방 ≥ Threshold
        ClearRun,           // 런 클리어 (Threshold 무시)
        TotalVictories,     // 평생 누적 클리어 ≥ Threshold
        WeaponKillsInRun,   // 특정 무기(WeaponId)로 단일 런 처치 ≥ Threshold
    }

    /// <summary>
    /// 업적 정의 (임계값은 코드가 아니라 에셋 — CLAUDE.md §2).
    /// 달성 여부 저장은 AchievementLog, 판정은 AchievementEvaluator.
    /// </summary>
    [CreateAssetMenu(fileName = "AchievementData", menuName = "Mecha Survivor/Achievement Data")]
    public sealed class AchievementData : ScriptableObject
    {
        [Tooltip("저장 키로 쓰이는 고유 ID (예: survive_10m). 출시 후 변경 금지")]
        public string Id = "achievement";

        public string Title = "업적";

        [TextArea]
        public string Description = "";

        public AchievementMetric Metric = AchievementMetric.SurvivalSeconds;

        public float Threshold = 1f;

        [Tooltip("Metric이 WeaponKillsInRun일 때만 사용")]
        public string WeaponId = "";
    }
}
