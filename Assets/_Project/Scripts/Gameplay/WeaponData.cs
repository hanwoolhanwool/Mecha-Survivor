using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>무기 정의 데이터. 발사 파라미터와 투사체 프리팹을 데이터로 관리.</summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Mecha Survivor/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("식별")]
        public string DisplayName = "Weapon";

        [Header("발사")]
        public float Damage = 5f;
        public float Cooldown = 1f;
        public float ProjectileSpeed = 10f;
        public int ProjectilesPerShot = 1;

        [Header("프리팹")]
        public GameObject ProjectilePrefab;
    }
}
