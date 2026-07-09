using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>적 정의 데이터. 코드 재빌드 없이 밸런싱하도록 ScriptableObject로 분리.</summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Mecha Survivor/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("식별")]
        public string DisplayName = "Enemy";

        [Header("스탯")]
        public float MaxHealth = 10f;
        public float MoveSpeed = 2f;
        public float ContactDamage = 5f;

        [Header("보상")]
        public int ExpReward = 1;

        [Header("프리팹")]
        public GameObject Prefab;
    }
}
