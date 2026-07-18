using System;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 도감·전적 화면이 쓰는 전체 콘텐츠 목록 (미발견 항목을 "???"로 보여주기 위한 전집).
    /// 런타임 수집 데이터(EnemyCodex/WeaponArchive)는 ID만 알므로,
    /// ID → 표시 이름/정의를 잇는 다리가 이 에셋이다.
    /// </summary>
    [CreateAssetMenu(fileName = "MetaCatalog", menuName = "Mecha Survivor/Meta Catalog")]
    public sealed class MetaCatalog : ScriptableObject
    {
        public EnemyData[] Enemies = Array.Empty<EnemyData>();
        public WeaponData[] Weapons = Array.Empty<WeaponData>();
    }
}
