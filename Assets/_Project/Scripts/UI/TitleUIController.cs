using System;
using UnityEngine;
using UnityEngine.UIElements;

public class TitleUIController : MonoBehaviour
{
    private UIDocument doc;
    private VisualElement root;

    private VisualElement slotOverlay;
    private VisualElement slotList;
    private Label slotTitle;
    private Label slotMessage;

    private enum Mode { NewGame, LoadGame }
    private Mode currentMode;
    private int pendingOverwriteSlot = -1;

    private void Awake()
    {
        doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("[Title] UIDocument not found on same GameObject.");
            enabled = false;
            return;
        }

        root = doc.rootVisualElement;

        // UXML elements
        slotOverlay = root.Q<VisualElement>("slotOverlay");
        slotList = root.Q<VisualElement>("slotList");
        slotTitle = root.Q<Label>("slotTitle");
        slotMessage = root.Q<Label>("slotMessage");

        if (slotOverlay == null || slotList == null || slotTitle == null || slotMessage == null)
        {
            Debug.LogError("[Title] Slot UI elements not found. Check UXML name attributes: slotOverlay, slotList, slotTitle, slotMessage.");
            enabled = false;
            return;
        }

        root.Q<Button>("btnNewGame").clicked += () => OpenSlots(Mode.NewGame);
        root.Q<Button>("btnLoadGame").clicked += () => OpenSlots(Mode.LoadGame);
        root.Q<Button>("btnQuit").clicked += () => Application.Quit();

        root.Q<Button>("btnCloseSlots").clicked += CloseSlots;

        CloseSlots();
    }

    private void OpenSlots(Mode mode)
    {
        if (GameManager.Instance == null)
        {
            slotMessage.text = "GameManager가 없습니다. Boot 씬에서 시작하세요.";
            slotOverlay.RemoveFromClassList("hidden");
            return;
        }

        currentMode = mode;
        pendingOverwriteSlot = -1;
        slotMessage.text = "";

        slotTitle.text = (mode == Mode.NewGame) ? "새 게임 - 슬롯 선택" : "불러오기 - 슬롯 선택";

        slotList.Clear();
        var metas = SaveSystem.GetAllMetas();
        for (int i = 0; i < metas.Length; i++)
        {
            int idx = i;
            var meta = metas[i];

            var btn = new Button(() => OnClickSlot(idx))
            {
                text = BuildSlotText(meta)
            };
            btn.AddToClassList("btn");
            btn.AddToClassList("slotBtn");
            slotList.Add(btn);
        }

        slotOverlay.RemoveFromClassList("hidden");
    }

    private void CloseSlots()
    {
        slotOverlay.AddToClassList("hidden");
        slotMessage.text = "";
        pendingOverwriteSlot = -1;
    }

    private string BuildSlotText(SaveSlotMeta meta)
    {
        if (!meta.exists) return $"{meta.displayName} (비어있음)";
        if (meta.savedAtUnix <= 0) return $"{meta.displayName} (저장됨)";
        var dt = DateTimeOffset.FromUnixTimeSeconds(meta.savedAtUnix).ToLocalTime();
        return $"{meta.displayName} (마지막 저장: {dt:yyyy-MM-dd HH:mm})";
    }

    private void OnClickSlot(int slotIndex)
    {
        if (GameManager.Instance == null)
        {
            slotMessage.text = "GameManager가 없습니다. Boot 씬에서 시작하세요.";
            return;
        }

        if (currentMode == Mode.LoadGame)
        {
            if (!GameManager.Instance.LoadGame(slotIndex))
            {
                slotMessage.text = "이 슬롯에는 저장 데이터가 없습니다.";
                return;
            }
            SceneLoader.Load("Hub_Main");
            return;
        }

        // New Game
        var meta = SaveSystem.GetMeta(slotIndex);
        if (meta.exists && pendingOverwriteSlot != slotIndex)
        {
            pendingOverwriteSlot = slotIndex;
            slotMessage.text = "이미 저장 데이터가 있습니다. 한 번 더 누르면 덮어씁니다.";
            return;
        }

        GameManager.Instance.StartNewGame(slotIndex);
        SceneLoader.Load("Hub_Main");
    }
}
