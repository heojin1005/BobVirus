using UnityEngine;
using UnityEngine.InputSystem;
using UI;

public class InventoryToggleController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InventoryUIController inventoryUI;

    [Header("Input Switching (PlayerInput)")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string hubActionMapName = "Hub";
    [SerializeField] private string uiActionMapName = "UI";

    [Header("World Tap Block")]
    [SerializeField] private WorldTapInteractor worldTapInteractor;
    [SerializeField] private bool disableWorldTapWhenOpen = true;

    [Header("Keyboard Toggle (PC)")]
    [SerializeField] private bool enableKeyboardToggle = true;
    [SerializeField] private Key toggleKey = Key.E;

    [Header("Time Control")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    [Header("Save")]
    [SerializeField] private bool saveOnClose = true;

    private void Awake()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>();

        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInput>();

        if (worldTapInteractor == null)
            worldTapInteractor = FindFirstObjectByType<WorldTapInteractor>();

        // 시작 시 닫힘 보장 + 허브 입력으로 고정
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.Close();

        SwitchToHub();
        if (pauseTimeWhenOpen) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!enableKeyboardToggle) return;

        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            Toggle();
    }

    // UI 버튼에서 연결
    public void Toggle()
    {
        if (inventoryUI == null) return;

        if (inventoryUI.IsOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        if (inventoryUI == null) return;

        inventoryUI.Open();

        if (pauseTimeWhenOpen)
            Time.timeScale = 0f;

        SwitchToUI();
    }

    public void CloseInventory()
    {
        if (inventoryUI == null) return;

        if (saveOnClose && GameManager.Instance != null)
            GameManager.Instance.SaveNow();

        inventoryUI.Close();

        if (pauseTimeWhenOpen)
            Time.timeScale = 1f;

        SwitchToHub();
    }

    private void SwitchToUI()
    {
        // 1) ActionMap 전환: Hub 입력 자체를 끈다 (OnTap 호출 방지의 핵심)
        if (playerInput != null && !string.IsNullOrEmpty(uiActionMapName))
        {
            if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != uiActionMapName)
                playerInput.SwitchCurrentActionMap(uiActionMapName);
        }

        // 2) 안전망: 월드 탭 스크립트 자체도 끈다
        if (disableWorldTapWhenOpen && worldTapInteractor != null)
            worldTapInteractor.enabled = false;
    }

    private void SwitchToHub()
    {
        if (playerInput != null && !string.IsNullOrEmpty(hubActionMapName))
        {
            if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != hubActionMapName)
                playerInput.SwitchCurrentActionMap(hubActionMapName);
        }

        if (worldTapInteractor != null)
            worldTapInteractor.enabled = true;
    }
}
