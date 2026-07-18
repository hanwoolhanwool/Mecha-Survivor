using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 적 아키타입 (GDD 5.3). 난이도 곡선은 "안전했던 선택지를 하나씩 빼앗는" 순서로
    /// 이들을 투입한다: 지상 → 비행(공중 박탈) → 포탑(착지 방해) → 엘리트 → 보스.
    /// </summary>
    public enum EnemyArchetype
    {
        Ground,  // 보행 잡병 — 물량, 경험치 공급원
        Flyer,   // 비행 드론 — 근접 자폭, 공중 안전지대 박탈
        Turret,  // 포탑형 — 고정, 예측 사격, 착지 방해
        Elite,   // 엘리트 — 높은 HP, 결정타 무기의 존재 이유
        Boss,    // 보스 — 20분 빌드의 최종 시험
    }

    /// <summary>적 정의 데이터. 코드 재빌드 없이 밸런싱하도록 ScriptableObject로 분리.</summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Mecha Survivor/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("도감·통계 집계용 고유 ID (예: walker). 출시 후 변경 금지")]
        public string Id = "enemy";

        public string DisplayName = "Enemy";
        public EnemyArchetype Archetype = EnemyArchetype.Ground;

        [Header("스탯")]
        public float MaxHealth = 10f;
        public float MoveSpeed = 2f;
        public float ContactDamage = 5f;

        [Header("접촉 공격")]
        [Tooltip("이 거리 안이면 접촉 판정 (물리 충돌 대신 거리 검사)")]
        public float ContactRadius = 1.5f;

        [Tooltip("접촉 데미지 반복 간격(초)")]
        public float ContactInterval = 1f;

        [Tooltip("접촉 시 자폭(비행 드론). 자폭 사망은 경험치를 주지 않는다")]
        public bool SelfDestructOnContact;

        [Header("원거리 공격 (포탑형)")]
        public float AttackRange = 45f;
        public float AttackInterval = 2.5f;
        public float ProjectileSpeed = 35f;
        public float ProjectileDamage = 6f;
        public Projectile ProjectilePrefab;

        [Header("스티어링 — 적끼리 물리 충돌 대신 분리 벡터 (SETUP 5장)")]
        public float SeparationRadius = 2f;

        [Header("보상")]
        public int ExpReward = 1;

        [Header("프리팹")]
        public EnemyBrain Prefab;
    }
}
