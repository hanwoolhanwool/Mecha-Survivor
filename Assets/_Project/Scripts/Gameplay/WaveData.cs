using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>스테이지의 시간대별 적 스폰 스케줄. 스포너가 이 데이터를 따라 스폰한다.</summary>
    [CreateAssetMenu(fileName = "WaveData", menuName = "Mecha Survivor/Wave Data")]
    public sealed class WaveData : ScriptableObject
    {
        [Serializable]
        public struct Spawn
        {
            public EnemyData Enemy;

            [Tooltip("스테이지 시작 기준, 이 스폰 규칙이 활성화되는 시각(초)")]
            public float StartTime;

            [Tooltip("스폰 간격(초)")]
            public float SpawnInterval;

            [Tooltip("이 규칙으로 동시에 생존 가능한 최대 수")]
            public int MaxAlive;
        }

        public List<Spawn> Spawns = new();
    }
}
