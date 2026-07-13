using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>무기 역할. 역할이 겹치면 로테이션이 무너진다 (GDD 3.3).</summary>
    public enum WeaponRole
    {
        Sustain,   // 주력 — 로테이션의 바닥
        Burst,     // 화력 — 무리 정리
        Finisher,  // 결정타 — 엘리트/보스 삭제
        Control,   // 유틸 — 상황을 만든다
    }

    /// <summary>발사 궤적 스타일 (GDD 8.2 — 미사일 4단 연출은 VerticalPopup).</summary>
    public enum LaunchStyle
    {
        Direct,        // 조준선으로 곧장
        VerticalPopup, // 수직 팝업 → 정렬 → 곡선 유도 (미사일 포드)
    }

    /// <summary>
    /// 무기 정의 데이터 (GDD 8.2). 발사 자원 필드는 없다 — 쿨다운이 전부다 (GDD 3.1).
    /// 화려함(연출)은 코드가 아니라 이 데이터로 조절한다 (GDD 3.4).
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Mecha Survivor/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("통계·쿨감 집계용 고유 ID (예: missile_pod)")]
        public string Id = "weapon";
        public string DisplayName = "Weapon";

        [Header("역할 — 로테이션에서의 자리")]
        public WeaponRole Role = WeaponRole.Sustain;

        [Header("발사")]
        public float Damage = 5f;

        [Tooltip("기본 쿨다운(초). 실제 쿨다운은 CooldownModifier가 나눗셈 모델로 계산")]
        public float BaseCooldown = 1f;

        [Tooltip("주력(Sustain)만 홀드 연사 허용이 유력 (GDD §9-6)")]
        public bool HoldToFire;

        public float ProjectileSpeed = 60f;
        public int ProjectilesPerShot = 1;

        [Tooltip("탄 퍼짐(도) — 완전 정밀 조준을 요구하지 않는 관용도 (GDD 2.4)")]
        public float SpreadAngle;

        [Tooltip("유도 강도(도/초). 0 = 유도 없음")]
        public float HomingTurnRate;

        public float Range = 300f;

        [Header("연출 파라미터 (GDD 3.4 — 강화는 수치만이 아니라 연출을 키운다)")]
        [Tooltip("발사 전 차징 시간(초). 빔·레일건의 예고 동작")]
        public float ChargeTime;

        public LaunchStyle LaunchStyle = LaunchStyle.Direct;

        [Tooltip("다발 발사 시차(초) — 동시 발사 '펑'이 아니라 '두두두둥'을 만든다")]
        public float StaggerInterval;

        [Tooltip("빔 굵기 등 시각 스케일. 강화 시 커진다 — 성장이 눈에 보인다")]
        public float VisualScale = 1f;

        [Header("지속 무기 (빔)")]
        [Tooltip("빔 지속 시간(초). Damage는 초당 데미지로 해석된다")]
        public float BeamDuration = 1.5f;

        [Tooltip("초당 데미지 판정 횟수")]
        public float BeamTicksPerSecond = 10f;

        [Header("강화 성장 (레벨 1~5) — 강화는 수치만이 아니라 연출을 키운다 (GDD 3.4)")]
        [Tooltip("레벨당 데미지 증가율 (0.25 = +25%)")]
        public float DamageGrowth = 0.25f;

        [Tooltip("레벨당 추가 발사 수 (미사일: 4→8→16 같은 성장)")]
        public int ProjectilesPerLevel;

        [Tooltip("레벨당 시각 스케일 증가율 — 빔 굵기 성장 등")]
        public float VisualScaleGrowth = 0.2f;

        [Tooltip("이 레벨부터 유도 성능 획득 (0 = 항상 데이터값 사용)")]
        public int HomingUnlockLevel;

        [Header("프리팹 (전부 풀링 대상)")]
        public Projectile ProjectilePrefab;
        public GameObject MuzzleVfxPrefab;
        public GameObject ImpactVfxPrefab;

        // ── 레벨 반영 스탯 ────────────────────────────────────────────

        public float GetDamage(int level) =>
            Damage * (1f + DamageGrowth * Mathf.Max(0, level - 1));

        public int GetProjectileCount(int level) =>
            ProjectilesPerShot + ProjectilesPerLevel * Mathf.Max(0, level - 1);

        public float GetVisualScale(int level) =>
            VisualScale * (1f + VisualScaleGrowth * Mathf.Max(0, level - 1));

        public float GetHomingTurnRate(int level) =>
            HomingUnlockLevel > 0 && level < HomingUnlockLevel ? 0f : HomingTurnRate;
    }
}
