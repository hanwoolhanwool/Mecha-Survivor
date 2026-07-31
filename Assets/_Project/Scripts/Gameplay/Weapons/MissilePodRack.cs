using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 트윈 로켓 캐논 장전 연출 — 포드 안의 미사일 모델(Missile_L/R_Inner/Outer_1~3)을
    /// 장전 수만큼 표시하고, 발사 시 한 발씩 소비(숨김)한다. 발사 위치도 소비된 모델
    /// 위치를 쓴다 (MissilePodWeapon이 호출). FBX의 Fire 클립이 있으면 발사마다 재생.
    /// </summary>
    public sealed class MissilePodRack : MonoBehaviour
    {
        [Tooltip("장전 미사일 모델 이름 접두사")]
        [SerializeField] private string _missilePrefix = "Missile_";

        [Tooltip("Fire 클립 재생용 (모델에 클립이 없으면 비움)")]
        [SerializeField] private Animator _animator;

        private static readonly int FireStateHash = Animator.StringToHash("Fire");

        private Transform[] _slots;   // 발사 순서 정렬 (R/L 교대, Inner → Outer)
        private int _loaded;          // 표시 중인 장전 수
        private int _nextFire;        // 다음 소비 인덱스

        /// <summary>장전 슬롯 수 (모델의 미사일 개수 — 트윈 로켓 캐논은 12).</summary>
        public int Capacity => _slots != null ? _slots.Length : 0;

        private void Awake()
        {
            CollectSlots();
        }

        /// <summary>자식에서 미사일 슬롯 수집·정렬. Awake가 호출하며 EditMode 테스트는 직접 부른다.</summary>
        public void CollectSlots()
        {
            var found = new List<Transform>();
            Transform[] all = GetComponentsInChildren<Transform>(includeInactive: true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name.StartsWith(_missilePrefix, System.StringComparison.Ordinal))
                {
                    found.Add(all[i]);
                }
            }

            // 발사 순서: Inner 먼저, 같은 층은 번호순, 같은 번호는 R → L 교대.
            found.Sort((a, b) => SortKey(a.name).CompareTo(SortKey(b.name)));
            _slots = found.ToArray();
        }

        /// <summary>Missile_{L|R}_{Inner|Outer}_{n} → 정렬 키. 형식이 다르면 뒤로 보낸다.</summary>
        private static int SortKey(string name)
        {
            string[] parts = name.Split('_');
            if (parts.Length < 4)
            {
                return int.MaxValue;
            }

            int tier = parts[2] == "Inner" ? 0 : 1;
            int side = parts[1] == "R" ? 0 : 1;
            int.TryParse(parts[3], out int index);
            return tier * 100 + index * 10 + side;
        }

        /// <summary>앞에서부터 count발을 표시하고 나머지는 숨긴다. 소비 인덱스 리셋.</summary>
        public void SetLoaded(int count)
        {
            if (_slots == null)
            {
                return;
            }

            _loaded = Mathf.Clamp(count, 0, _slots.Length);
            _nextFire = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                bool show = i < _loaded;
                if (_slots[i].gameObject.activeSelf != show)
                {
                    _slots[i].gameObject.SetActive(show);
                }
            }
        }

        /// <summary>장전된 다음 미사일을 소비(숨김)하고 그 트랜스폼을 준다. 다 떨어지면 false.</summary>
        public bool TryConsumeNext(out Transform slot)
        {
            if (_slots != null && _nextFire < _loaded)
            {
                slot = _slots[_nextFire];
                _nextFire++;
                slot.gameObject.SetActive(false);
                return true;
            }

            slot = null;
            return false;
        }

        /// <summary>포드 발사 반동 애니메이션 (FBX Fire 클립). 발사마다 처음부터 재생.</summary>
        public void PlayFire()
        {
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.Play(FireStateHash, 0, 0f);
            }
        }
    }
}
