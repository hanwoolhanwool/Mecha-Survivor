using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 무기 실험실 (WeaponLab 씬 전용). 모든 무기를 즉시 바꿔 끼우고 레벨을 조절하며
    /// 정지 표적 더미에 자유 사격한다. 발사는 슬롯 1(좌클릭)의 정상 경로를 그대로 쓴다 —
    /// HUD 쿨다운 게이지·홀드 연사 규칙이 본편과 동일하게 동작한다.
    ///
    /// 조작: ←/→ 무기 전환 · 숫자 1~9,0 무기 직접 선택 · ↑/↓ 강화 레벨 · R 표적 재배치.
    /// (WASD/Space/Shift 이동, 좌클릭 발사, V 시점 전환은 본편 조작 그대로)
    /// </summary>
    public sealed class WeaponLabController : MonoBehaviour
    {
        [Header("플레이어")]
        [SerializeField] private WeaponSlots _slots;
        [SerializeField] private Transform _weaponMount;
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerHealth _playerHealth;

        [Header("무기 목록 (전환 순서)")]
        [SerializeField] private Weapon[] _weaponPrefabs;

        [Header("표적 더미")]
        [SerializeField] private EnemyBrain _dummyPrefab;
        [SerializeField] private EnemyData _dummyData;
        [SerializeField] private int _dummyCount = 10;
        [SerializeField] private float _ringRadius = 28f;
        [SerializeField] private float _respawnDelay = 1.5f;

        [Tooltip("표적 접지용 지면 탐색 — Wall(13), Ground(14)")]
        [SerializeField] private LayerMask _groundMask = (1 << 13) | (1 << 14);

        private int _index;
        private int _level = 1;
        private Weapon _current;
        private readonly List<float> _pendingRespawns = new(32);
        private string _hudText = string.Empty;
        private GUIStyle _hudStyle;

        private void OnEnable() => EventBus<EnemyKilledEvent>.Subscribe(OnDummyKilled);
        private void OnDisable() => EventBus<EnemyKilledEvent>.Unsubscribe(OnDummyKilled);

        private void Start()
        {
            SelectWeapon(0);
            ResetDummies();
        }

        private void Update()
        {
            ReadHotkeys();

            // 처치된 표적 보충 (역순 제거 — 컬렉션 수정 안전).
            for (int i = _pendingRespawns.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _pendingRespawns[i])
                {
                    _pendingRespawns.RemoveAt(i);
                    SpawnDummy(Random.Range(0, _dummyCount));
                }
            }
        }

        private void ReadHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || Mathf.Approximately(Time.timeScale, 0f))
            {
                return;
            }

            if (kb.leftArrowKey.wasPressedThisFrame)
            {
                SelectWeapon(_index - 1);
            }

            if (kb.rightArrowKey.wasPressedThisFrame)
            {
                SelectWeapon(_index + 1);
            }

            if (kb.upArrowKey.wasPressedThisFrame)
            {
                SetWeaponLevel(_level + 1);
            }

            if (kb.downArrowKey.wasPressedThisFrame)
            {
                SetWeaponLevel(_level - 1);
            }

            if (kb.rKey.wasPressedThisFrame)
            {
                ResetDummies();
            }

            // 숫자 1~9,0 = 무기 1~10 직접 선택 (Key 열거형은 Digit1..Digit9,Digit0 순).
            for (int d = 0; d < 10; d++)
            {
                if (kb[Key.Digit1 + d].wasPressedThisFrame && d < _weaponPrefabs.Length)
                {
                    SelectWeapon(d);
                }
            }
        }

        /// <summary>무기 교체 — 이전 인스턴스는 풀로 회수하고 새 무기를 슬롯 1에 장착.</summary>
        public void SelectWeapon(int index)
        {
            if (_weaponPrefabs == null || _weaponPrefabs.Length == 0)
            {
                return;
            }

            _index = WeaponLabMath.WrapIndex(index, _weaponPrefabs.Length);

            if (_current != null)
            {
                _slots.ReplaceSlot(0, null);
                PoolManager.Instance.Despawn(_current);
                _current = null;
            }

            Weapon prefab = _weaponPrefabs[_index];
            if (prefab == null)
            {
                RefreshHud();
                return;
            }

            _current = (Weapon)PoolManager.Instance.Spawn(
                prefab, _weaponMount.position, _weaponMount.rotation);
            _current.transform.SetParent(_weaponMount, worldPositionStays: false);
            _current.transform.localPosition = Vector3.zero;
            _current.transform.localRotation = Quaternion.identity;
            _current.SetLevel(_level);
            _slots.ReplaceSlot(0, _current);

            RefreshHud();
        }

        public void SetWeaponLevel(int level)
        {
            _level = Mathf.Clamp(level, 1, 5);
            if (_current != null)
            {
                _current.SetLevel(_level);
            }

            RefreshHud();
        }

        /// <summary>표적 전체 재배치 — 살아있는 적을 전부 회수하고 링을 다시 깐다.</summary>
        public void ResetDummies()
        {
            _pendingRespawns.Clear();

            IReadOnlyList<EnemyBrain> active = EnemyBrain.ActiveEnemies;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                PoolManager.Instance.Despawn(active[i]);
            }

            for (int i = 0; i < _dummyCount; i++)
            {
                SpawnDummy(i);
            }
        }

        private void SpawnDummy(int ringSlot)
        {
            if (_dummyPrefab == null || _dummyData == null || _player == null)
            {
                return;
            }

            Vector3 center = _player.position;
            center.y = 0f;
            Vector3 position = WeaponLabMath.RingPosition(center, _ringRadius, ringSlot, _dummyCount);

            // 지형(고원 위 포함)에 접지 — 없으면 평지 y=1.
            Vector3 origin = position + Vector3.up * 90f;
            position.y = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f,
                _groundMask, QueryTriggerInteraction.Ignore)
                ? hit.point.y + 1f
                : 1f;

            var dummy = (EnemyBrain)PoolManager.Instance.Spawn(
                _dummyPrefab, position, Quaternion.identity);
            dummy.Init(_dummyData, _player, _playerHealth);
        }

        private void OnDummyKilled(EnemyKilledEvent evt)
        {
            _pendingRespawns.Add(Time.time + _respawnDelay);
        }

        private void RefreshHud()
        {
            string weaponName = _current != null && _current.Data != null
                ? _current.Data.DisplayName
                : "(없음)";

            _hudText =
                $"[무기 실험실]  {_index + 1}/{_weaponPrefabs.Length}  {weaponName}  Lv.{_level}\n" +
                "←/→ 무기 전환   숫자 1~0 직접 선택   ↑/↓ 레벨   R 표적 재배치\n" +
                "좌클릭 발사 · WASD/Space/Shift 이동 · V 시점 전환";
        }

        private void OnGUI()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _hudStyle.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(12f, 12f, 720f, 90f), _hudText, _hudStyle);
        }
    }
}
