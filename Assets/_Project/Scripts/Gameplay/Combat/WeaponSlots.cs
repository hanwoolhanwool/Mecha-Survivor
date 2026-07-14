using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 무기 슬롯 보유·발사 요청 중계 (GDD 3.1 — 초기 2슬롯, 파츠로 최대 4).
    /// 슬롯 1~4 = 좌클릭/우클릭/Q/E. 홀드 연사는 무기 데이터가 허용할 때만.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class WeaponSlots : MonoBehaviour
    {
        public const int MaxSlots = 4;

        [Header("참조")]
        [SerializeField] private MechaInput _input;
        [SerializeField] private MechaAimer _aimer;
        [SerializeField] private CooldownModifier _cooldowns;

        [Header("슬롯")]
        [Tooltip("시작 시 사용 가능한 슬롯 수 (GDD §9-2: 2 → 파츠로 확장)")]
        [SerializeField] private int _unlockedSlots = 2;
        [SerializeField] private Weapon[] _slots = new Weapon[MaxSlots];

        [Tooltip("씬에 미리 배치된 자식 무기를 시작 시 순서대로 장착 (초기 로드아웃)")]
        [SerializeField] private bool _autoEquipChildren = true;

        private readonly bool[] _wasHeld = new bool[MaxSlots];

        private void Awake()
        {
            if (!_autoEquipChildren)
            {
                return;
            }

            Weapon[] children = GetComponentsInChildren<Weapon>(includeInactive: false);
            for (int i = 0; i < children.Length; i++)
            {
                if (!Has(children[i].Data))
                {
                    Equip(children[i]);
                }
            }
        }

        public int UnlockedSlots => _unlockedSlots;

        public Weapon GetWeapon(int index) =>
            index >= 0 && index < MaxSlots ? _slots[index] : null;

        private void Update()
        {
            if (_input == null)
            {
                return;
            }

            // 시간 정지(3택/결과/일시정지) 중 UI 클릭이 발사로 새지 않게 차단.
            if (Mathf.Approximately(Time.timeScale, 0f))
            {
                return;
            }

            MechaInputFrame frame = _input.Frame;

            for (int i = 0; i < MaxSlots; i++)
            {
                bool held = frame.IsFireHeld(i);
                Weapon weapon = _slots[i];

                if (i < _unlockedSlots && weapon != null && weapon.Data != null && held)
                {
                    // 홀드 연사 무기는 누르고 있는 동안, 그 외에는 누른 순간만.
                    if (weapon.Data.HoldToFire || !_wasHeld[i])
                    {
                        weapon.TryFire(_aimer, _cooldowns);
                    }
                }

                _wasHeld[i] = held;
            }
        }

        /// <summary>슬롯 확장 파츠가 호출. 최대치면 false.</summary>
        public bool UnlockSlot()
        {
            if (_unlockedSlots >= MaxSlots)
            {
                return false;
            }

            _unlockedSlots++;
            return true;
        }

        /// <summary>슬롯의 무기를 직접 교체하고 이전 무기를 반환 (무기 실험실·교체 기능용).</summary>
        public Weapon ReplaceSlot(int index, Weapon weapon)
        {
            if (index < 0 || index >= MaxSlots)
            {
                return null;
            }

            Weapon old = _slots[index];
            _slots[index] = weapon;
            return old;
        }

        /// <summary>빈 해금 슬롯에 무기를 장착하고 슬롯 번호를 반환. 자리가 없으면 -1.</summary>
        public int Equip(Weapon weapon)
        {
            for (int i = 0; i < _unlockedSlots; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = weapon;
                    return i;
                }
            }

            return -1;
        }

        /// <summary>해금된 슬롯 중 빈 자리가 있는지 (무기 파츠 제안 가능 여부 판정용).</summary>
        public bool HasFreeUnlockedSlot
        {
            get
            {
                for (int i = 0; i < _unlockedSlots; i++)
                {
                    if (_slots[i] == null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>이미 장착된 무기인지 (중복 획득 방지·강화 판정용).</summary>
        public bool Has(WeaponData data)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] != null && _slots[i].Data == data)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
