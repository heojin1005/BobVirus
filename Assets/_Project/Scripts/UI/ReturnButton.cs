using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReturnButton : MonoBehaviour
{
    private const string HubSceneName = "Hub_main";
    private const string MainSceneName = "Main";
    private const string LobbySceneName = "Title";
    private const string PauseReason = "RETURN_CONFIRM";

    private GameObject confirmOverlay;
    private bool cursorWasVisible;
    private CursorLockMode previousLockMode;
    private bool isBuilt;
    private static TMP_FontAsset regularFont;
    private static TMP_FontAsset boldFont;

    public static bool BlocksInput { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (scene.name != HubSceneName && scene.name != MainSceneName)
            return;

        var existingButtons = FindObjectsByType<ReturnButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existingButtons.Length; i++)
        {
            var existing = existingButtons[i];
            if (existing == null)
                continue;

            if (existing.isBuilt)
                return;

            Destroy(existing.gameObject);
        }

        var root = new GameObject("ReturnButton", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ReturnButton));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        try
        {
            root.GetComponent<ReturnButton>().Build(scene.name);
        }
        catch
        {
            Destroy(root);
            throw;
        }
    }

    private void Build(string sceneName)
    {
        string buttonText = sceneName == HubSceneName
            ? "\uB85C\uBE44\uB85C \uB3CC\uC544\uAC00\uAE30"
            : "\uD0D0\uC0AC \uD3EC\uAE30";
        string message = sceneName == HubSceneName
            ? "\uB85C\uBE44\uB85C \uB3CC\uC544\uAC08\uAE4C\uC694?\n\uD604\uC7AC \uC9C4\uD589 \uC0C1\uD669\uC744 \uC800\uC7A5\uD569\uB2C8\uB2E4."
            : "\uD0D0\uC0AC\uB97C \uD3EC\uAE30\uD558\uACE0 \uB85C\uBE44\uB85C \uB3CC\uC544\uAC08\uAE4C\uC694?\n\uD604\uC7AC \uC138\uC774\uBE0C\uB97C \uC800\uC7A5\uD55C \uB4A4 \uC774\uB3D9\uD569\uB2C8\uB2E4.";

        var button = CreateButton("Return", transform, buttonText, new Vector2(190f, 48f));
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -18f);
        button.onClick.AddListener(() => ShowConfirm(message));

        BuildConfirmOverlay();
        confirmOverlay.SetActive(false);
        isBuilt = true;
    }

    private void BuildConfirmOverlay()
    {
        confirmOverlay = new GameObject("ReturnConfirmOverlay", typeof(RectTransform), typeof(Image));
        confirmOverlay.transform.SetParent(transform, false);

        var overlayRect = confirmOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var overlayImage = confirmOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
        overlayImage.raycastTarget = true;

        var panel = new GameObject("Dialog", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(confirmOverlay.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460f, 220f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.11f, 0.11f, 0.14f, 0.98f);

        var title = CreateText("Title", panel.transform, "\uD655\uC778", 26, FontStyle.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -22f);
        titleRect.sizeDelta = new Vector2(-40f, 34f);

        var message = CreateText("Message", panel.transform, "", 20, FontStyle.Normal);
        message.name = "ReturnConfirmMessage";
        var messageRect = message.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0f, 0f);
        messageRect.anchorMax = new Vector2(1f, 1f);
        messageRect.offsetMin = new Vector2(36f, 78f);
        messageRect.offsetMax = new Vector2(-36f, -62f);

        var cancel = CreateButton("Cancel", panel.transform, "\uCDE8\uC18C", new Vector2(120f, 44f));
        var cancelRect = cancel.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.pivot = new Vector2(0.5f, 0f);
        cancelRect.anchoredPosition = new Vector2(-72f, 22f);
        cancel.onClick.AddListener(CloseConfirm);

        var confirm = CreateButton("Confirm", panel.transform, "\uD655\uC778", new Vector2(120f, 44f));
        var confirmRect = confirm.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0f);
        confirmRect.anchorMax = new Vector2(0.5f, 0f);
        confirmRect.pivot = new Vector2(0.5f, 0f);
        confirmRect.anchoredPosition = new Vector2(72f, 22f);
        confirm.onClick.AddListener(ReturnToLobby);
    }

    private void ShowConfirm(string message)
    {
        if (confirmOverlay.activeSelf)
            return;

        CloseOtherSceneUIs();

        cursorWasVisible = Cursor.visible;
        previousLockMode = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        var messageText = confirmOverlay.transform.Find("Dialog/ReturnConfirmMessage")?.GetComponent<TMP_Text>();
        if (messageText != null)
            messageText.text = message;

        BlocksInput = true;
        confirmOverlay.SetActive(true);
        PauseService.Instance?.Push(PauseReason);
        InputBlockService.Instance?.SetBlocked(true);
    }

    private void CloseConfirm()
    {
        confirmOverlay.SetActive(false);
        BlocksInput = false;
        PauseService.Instance?.Pop(PauseReason);
        InputBlockService.Instance?.SetBlocked(false);
        Cursor.visible = cursorWasVisible;
        Cursor.lockState = previousLockMode;
    }

    private void ReturnToLobby()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SaveNow();

        PauseService.Instance?.Pop(PauseReason);
        InputBlockService.Instance?.SetBlocked(false);
        BlocksInput = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneLoader.Load(LobbySceneName);
    }

    private void OnDestroy()
    {
        if (!BlocksInput)
            return;

        BlocksInput = false;
        PauseService.Instance?.Pop(PauseReason);
        InputBlockService.Instance?.SetBlocked(false);
    }

    private static void CloseOtherSceneUIs()
    {
        var inventoryToggles = FindObjectsByType<InventoryToggleController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < inventoryToggles.Length; i++)
            inventoryToggles[i]?.CloseInventory();

        if (NpcUIManager.Instance != null)
        {
            NpcUIManager.Instance.CloseAll();
            return;
        }

        var npcManagers = FindObjectsByType<NpcUIManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < npcManagers.Length; i++)
            npcManagers[i]?.CloseAll();
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.28f, 0.96f);

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.25f, 0.30f, 0.38f, 0.98f);
        colors.pressedColor = new Color(0.13f, 0.16f, 0.21f, 0.98f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var label = CreateText("Label", go.transform, text, 20, FontStyle.Bold);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var uiText = go.GetComponent<TextMeshProUGUI>();
        uiText.text = text;
        var fontAsset = ResolveFont(fontStyle);
        if (fontAsset != null)
            uiText.font = fontAsset;
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle == FontStyle.Bold && boldFont == null ? FontStyles.Bold : FontStyles.Normal;
        uiText.alignment = TextAlignmentOptions.Center;
        uiText.textWrappingMode = TextWrappingModes.Normal;
        uiText.color = new Color(0.96f, 0.96f, 0.96f, 1f);
        uiText.raycastTarget = false;

        return uiText;
    }

    private static TMP_FontAsset ResolveFont(FontStyle fontStyle)
    {
        if (regularFont == null || boldFont == null)
            CachePretendardFonts();

        if (fontStyle == FontStyle.Bold && boldFont != null)
            return boldFont;

        return regularFont;
    }

    private static void CachePretendardFonts()
    {
#if UNITY_EDITOR
        regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Pretendard-Regular SDF.asset");
        boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Pretendard-SemiBold SDF.asset");
        if (boldFont == null)
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Pretendard-Bold SDF.asset");
#endif

        if (regularFont != null && boldFont != null)
            return;
    }
}
