using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 로비에서 선택한 출격 로드아웃을 런 시작 시 적용한다.
    /// 무기를 직접 마운트하지 않고 UpgradeInventory의 초기 무기 파츠를 교체한다 —
    /// 정상 지급 경로(인벤토리)를 그대로 타야 강화안·조합 판정이 깨지지 않는다.
    /// 선택이 없거나 ID를 모르면 아무것도 하지 않는다 — 씬에 설정된 기본 로드아웃 그대로.
    /// 이 컴포넌트만 제거하면 시작 무기 선택 기능 전체가 게임에서 사라진다 (격리 설계).
    /// 차후 "기체 선택"으로 확장 시 이 클래스가 기체 모델 교체까지 맡는 자리다.
    /// Awake에서 교체해야 UpgradeInventory.Start의 초기 지급보다 앞선다.
    /// </summary>
    public sealed class StartLoadoutApplier : MonoBehaviour
    {
        [SerializeField] private LoadoutData _loadouts;
        [SerializeField] private UpgradeInventory _inventory;

        private void Awake()
        {
            StartLoadout selection = StartLoadout.Resolve();
            if (!selection.HasSelection || _loadouts == null || _inventory == null)
            {
                return;
            }

            LoadoutData.Entry entry = _loadouts.Find(selection.SelectedId);
            if (entry != null)
            {
                _inventory.OverrideInitialWeapons(entry.WeaponParts);
            }
        }
    }
}
