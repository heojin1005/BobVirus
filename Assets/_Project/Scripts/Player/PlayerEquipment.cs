// Scripts/Player/PlayerEquipment.cs 신규 생성
using UnityEngine;
using UnityEngine.InputSystem; // 단축키 감지용

public class PlayerEquipment : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private SaveDataProvider saveDataProvider;
    [SerializeField] private ItemDatabase itemDatabase;

    private void Start()
    {
        if (saveDataProvider == null) saveDataProvider = FindFirstObjectByType<SaveDataProvider>();
        if (weaponSystem == null) weaponSystem = GetComponentInChildren<WeaponSystem>();

        // 게임 시작 시 세이브된 무기를 꺼내 듦
        UpdateWeaponEquipment();
    }

    private void Update()
    {
        // 마우스/키보드가 연결되어 있지 않으면 리턴
        if (Keyboard.current == null) return;

        // 키보드 1~8번을 누르면 인벤토리의 0~7번째 칸을 확인하여 장착 시도
        if (Keyboard.current[Key.Digit1].wasPressedThisFrame) TryEquipFromHotbar(0);
        if (Keyboard.current[Key.Digit2].wasPressedThisFrame) TryEquipFromHotbar(1);
        if (Keyboard.current[Key.Digit3].wasPressedThisFrame) TryEquipFromHotbar(2);
        if (Keyboard.current[Key.Digit4].wasPressedThisFrame) TryEquipFromHotbar(3);
        if (Keyboard.current[Key.Digit5].wasPressedThisFrame) TryEquipFromHotbar(4);
        if (Keyboard.current[Key.Digit6].wasPressedThisFrame) TryEquipFromHotbar(5);
        if (Keyboard.current[Key.Digit7].wasPressedThisFrame) TryEquipFromHotbar(6);
        if (Keyboard.current[Key.Digit8].wasPressedThisFrame) TryEquipFromHotbar(7);
    }

    private void TryEquipFromHotbar(int slotIndex)
    {
        SaveGameData data = saveDataProvider.GetCurrentData();
        // 인벤토리 칸수를 넘어가면 무시
        if (data == null || slotIndex >= data.inventoryCapacity) return; 

        string itemId = data.inventoryItems[slotIndex]; // 해당 칸에 있는 아이템 ID

        // 빈 칸을 눌렀을 경우 (선택: 맨손으로 만들고 싶다면 아래 로직 활성화)
        if (string.IsNullOrEmpty(itemId))
        {
            // data.weaponId = "";
            // UpdateWeaponEquipment();
            return;
        }

        ItemDefinition itemDef = itemDatabase.GetOrNull(itemId);
        
        // 누른 아이템이 존재하고, 그 타입이 '무기(Weapon)'일 때만 장착
        if (itemDef != null && itemDef.equipSlot == EquipSlotType.Weapon)
        {
            data.weaponId = itemId; // 세이브 데이터에 현재 든 무기 기록
            UpdateWeaponEquipment(); // 실제 손에 쥐어주기
            
            // 데이터가 변경되었으므로 자동 저장 (선택 사항)
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
        }
    }

    // 실제로 WeaponSystem에 데이터를 밀어넣어 총을 바꾸는 함수
    public void UpdateWeaponEquipment()
    {
        if (weaponSystem == null || saveDataProvider == null || itemDatabase == null) return;

        SaveGameData data = saveDataProvider.GetCurrentData();
        if (data == null) return;

        string currentWeaponId = data.weaponId;

        // 무기가 없거나 맨손일 때
        if (string.IsNullOrEmpty(currentWeaponId))
        {
            weaponSystem.weaponData = null;
            if (weaponSystem.weaponRenderer != null) weaponSystem.weaponRenderer.sprite = null;
            return;
        }

        ItemDefinition itemDef = itemDatabase.GetOrNull(currentWeaponId);
        if (itemDef != null && itemDef.weaponData != null)
        {
            // WeaponSystem에 전투 데이터를 덮어씌우고 재초기화
            weaponSystem.weaponData = itemDef.weaponData;
            weaponSystem.InitializeWeapon(); 
            Debug.Log($"[PlayerEquipment] 핫키 장착 완료: {itemDef.displayName}");
        }
    }
}