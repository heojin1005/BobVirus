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

    public void Toggle()
    {
        if (inventoryUI == null) return;

        if (inventoryUI.IsOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        if (inventoryUI == null) return;
        SwitchToUI();
        inventoryUI.Open();

        if (pauseTimeWhenOpen)
            Time.timeScale = 0f;
        Debug.Log($"[InventoryToggle] OpenInventory | time={Time.unscaledTime:F3}");
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
        // ActionMap 전환만으로도 Hub 탭 입력은 안 들어오게 됨
        if (playerInput != null && !string.IsNullOrEmpty(uiActionMapName))
        {
            if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != uiActionMapName)
                playerInput.SwitchCurrentActionMap(uiActionMapName);
        }
    }

    private void SwitchToHub()
    {
        if (playerInput != null && !string.IsNullOrEmpty(hubActionMapName))
        {
            if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != hubActionMapName)
                playerInput.SwitchCurrentActionMap(hubActionMapName);
        }
    }
}