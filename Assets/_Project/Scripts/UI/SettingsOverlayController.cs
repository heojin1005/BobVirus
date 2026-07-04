using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SettingsOverlayController : MonoBehaviour
{
    public static SettingsOverlayController Instance { get; private set; }

    private const string PauseReason = "SETTINGS_UI";

    [Header("Style")]
    [SerializeField] private StyleSheet settingsStyleSheet;

    [Header("uGUI Input Blocker")]
    [SerializeField] private GameObject uguiInputBlockerCanvas;

    [Header("Close Other UI On Open")]
    [SerializeField] private bool closeNpcUIOnOpen = true;
    [SerializeField] private bool closeInventoryOnOpen = true;

    private UIDocument uiDocument;

    private VisualElement overlayRoot;
    private VisualElement settingsOverlay;
    private VisualElement settingsMainPanel;
    private VisualElement soundPanel;

    private Button btnCloseSettings;
    private Button btnOpenSound;
    private Button btnCloseSound;
    private Button btnBackFromSound;

    private Slider masterSlider;
    private Slider bgmSlider;
    private Slider sfxSlider;

    private Label masterValue;
    private Label bgmValue;
    private Label sfxValue;

    private bool isOpen;
    private bool isBound;

    public bool IsOpen => isOpen;

    public static bool BlocksInput => Instance != null && Instance.IsOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[SettingsOverlayController] rootVisualElement가 없습니다.");
            return;
        }

        if (uguiInputBlockerCanvas != null)
            uguiInputBlockerCanvas.SetActive(false);

        ApplyStyleSheet(root);
        BindUI(root);
        ForceClosedVisual();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnbindCallbacks();

        if (uguiInputBlockerCanvas != null)
            uguiInputBlockerCanvas.SetActive(false);

        if (isOpen && PauseService.Instance != null)
            PauseService.Instance.Pop(PauseReason);
    }

    private void ApplyStyleSheet(VisualElement root)
    {
        if (settingsStyleSheet == null)
        {
            Debug.LogError("[SettingsOverlayController] settingsStyleSheet가 비어 있습니다.");
            return;
        }

        if (!root.styleSheets.Contains(settingsStyleSheet))
            root.styleSheets.Add(settingsStyleSheet);
    }

    private void BindUI(VisualElement root)
    {
        if (isBound)
            return;

        overlayRoot       = root.Q<VisualElement>("overlayRoot");
        settingsOverlay   = root.Q<VisualElement>("settingsOverlay");
        settingsMainPanel = root.Q<VisualElement>("settingsMainPanel");
        soundPanel        = root.Q<VisualElement>("soundPanel");

        btnCloseSettings = root.Q<Button>("btnCloseSettings");
        btnOpenSound     = root.Q<Button>("btnOpenSound");
        btnCloseSound    = root.Q<Button>("btnCloseSound");
        btnBackFromSound = root.Q<Button>("btnBackFromSound");

        masterSlider = root.Q<Slider>("masterSlider");
        bgmSlider    = root.Q<Slider>("bgmSlider");
        sfxSlider    = root.Q<Slider>("sfxSlider");

        masterValue = root.Q<Label>("masterValue");
        bgmValue    = root.Q<Label>("bgmValue");
        sfxValue    = root.Q<Label>("sfxValue");

        bool failed =
            overlayRoot == null ||
            settingsOverlay == null ||
            settingsMainPanel == null ||
            soundPanel == null ||
            btnCloseSettings == null ||
            btnOpenSound == null ||
            btnCloseSound == null ||
            btnBackFromSound == null ||
            masterSlider == null ||
            bgmSlider == null ||
            sfxSlider == null ||
            masterValue == null ||
            bgmValue == null ||
            sfxValue == null;

        if (failed)
        {
            Debug.LogError("[SettingsOverlayController] UXML 요소 바인딩 실패");
            return;
        }

        root.pickingMode = PickingMode.Ignore;
        overlayRoot.pickingMode = PickingMode.Ignore;

        settingsOverlay.pickingMode = PickingMode.Position;
        settingsMainPanel.pickingMode = PickingMode.Position;
        soundPanel.pickingMode = PickingMode.Position;

        btnCloseSettings.clicked += CloseSettings;
        btnOpenSound.clicked += ShowSoundPanel;
        btnCloseSound.clicked += CloseSettings;
        btnBackFromSound.clicked += ShowMainPanel;

        masterSlider.RegisterValueChangedCallback(OnMasterSliderChanged);
        bgmSlider.RegisterValueChangedCallback(OnBgmSliderChanged);
        sfxSlider.RegisterValueChangedCallback(OnSfxSliderChanged);

        isBound = true;
    }

    private void UnbindCallbacks()
    {
        if (!isBound)
            return;

        if (btnCloseSettings != null)
            btnCloseSettings.clicked -= CloseSettings;

        if (btnOpenSound != null)
            btnOpenSound.clicked -= ShowSoundPanel;

        if (btnCloseSound != null)
            btnCloseSound.clicked -= CloseSettings;

        if (btnBackFromSound != null)
            btnBackFromSound.clicked -= ShowMainPanel;

        if (masterSlider != null)
            masterSlider.UnregisterValueChangedCallback(OnMasterSliderChanged);

        if (bgmSlider != null)
            bgmSlider.UnregisterValueChangedCallback(OnBgmSliderChanged);

        if (sfxSlider != null)
            sfxSlider.UnregisterValueChangedCallback(OnSfxSliderChanged);

        isBound = false;
    }

    public void OpenSettings()
    {
        if (!isBound || isOpen)
            return;

        // 중요:
        // BlocksInput이 true가 되기 전에 기존 UI들을 먼저 닫는다.
        // 그래야 인벤토리 닫기, NPC UI 닫기가 입력 차단 상태에 영향받지 않는다.
        CloseOtherSceneUIsBeforeOpeningSettings();

        isOpen = true;

        RefreshFromGlobal();

        if (uguiInputBlockerCanvas != null)
            uguiInputBlockerCanvas.SetActive(true);

        settingsOverlay.RemoveFromClassList("hidden");
        ShowMainPanel();

        if (PauseService.Instance != null)
            PauseService.Instance.Push(PauseReason);

        if (InputBlockService.Instance != null)
            InputBlockService.Instance.SetBlocked(true);
    }

    public void CloseSettings()
    {
        if (!isBound || !isOpen)
            return;

        isOpen = false;

        ForceClosedVisual();

        if (uguiInputBlockerCanvas != null)
            uguiInputBlockerCanvas.SetActive(false);

        if (PauseService.Instance != null)
            PauseService.Instance.Pop(PauseReason);

        if (InputBlockService.Instance != null)
            InputBlockService.Instance.SetBlocked(false);
    }

    private void CloseOtherSceneUIsBeforeOpeningSettings()
    {
        CloseNpcUIIfNeeded();
        CloseInventoryIfNeeded();
    }

    private void CloseNpcUIIfNeeded()
    {
        if (!closeNpcUIOnOpen)
            return;

        // NpcUIManager는 싱글톤 구조이므로 Instance를 우선 사용.
        // CloseAllFromDimmer는 열린 직후 무시 시간이 있어서 ESC 강제 닫기에는 CloseAll이 더 적합함.
        if (NpcUIManager.Instance != null)
        {
            NpcUIManager.Instance.CloseAll();
            return;
        }

        // 혹시 Instance가 아직 없거나 씬 구조가 꼬였을 때를 위한 보조 탐색.
        var managers = FindObjectsByType<NpcUIManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null)
                managers[i].CloseAll();
        }
    }

    private void CloseInventoryIfNeeded()
    {
        if (!closeInventoryOnOpen)
            return;

        var controllers = FindObjectsByType<InventoryToggleController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
                controllers[i].CloseInventory();
        }
    }

    private void ForceClosedVisual()
    {
        settingsOverlay?.AddToClassList("hidden");
        settingsMainPanel?.AddToClassList("hidden");
        soundPanel?.AddToClassList("hidden");
    }

    private void ShowMainPanel()
    {
        if (settingsMainPanel == null || soundPanel == null)
            return;

        settingsMainPanel.RemoveFromClassList("hidden");
        soundPanel.AddToClassList("hidden");
    }

    private void ShowSoundPanel()
    {
        if (settingsMainPanel == null || soundPanel == null)
            return;

        settingsMainPanel.AddToClassList("hidden");
        soundPanel.RemoveFromClassList("hidden");
    }

    private void OnMasterSliderChanged(ChangeEvent<float> evt)
    {
        if (!TryGetGlobalData(out var data))
            return;

        data.masterVolume = Mathf.Clamp01(evt.newValue);
        UpdatePercentLabel(masterValue, data.masterVolume);
        SaveGlobal();
    }

    private void OnBgmSliderChanged(ChangeEvent<float> evt)
    {
        if (!TryGetGlobalData(out var data))
            return;

        data.bgmVolume = Mathf.Clamp01(evt.newValue);
        UpdatePercentLabel(bgmValue, data.bgmVolume);
        SaveGlobal();
    }

    private void OnSfxSliderChanged(ChangeEvent<float> evt)
    {
        if (!TryGetGlobalData(out var data))
            return;

        data.sfxVolume = Mathf.Clamp01(evt.newValue);
        UpdatePercentLabel(sfxValue, data.sfxVolume);
        SaveGlobal();
    }

    private bool TryGetGlobalData(out GlobalSaveData data)
    {
        data = null;

        if (GameManager.Instance == null)
            return false;

        if (GameManager.Instance.GlobalData == null)
            GameManager.Instance.ReloadGlobal();

        data = GameManager.Instance.GlobalData;
        return data != null;
    }

    private void RefreshFromGlobal()
    {
        if (!TryGetGlobalData(out var data))
            return;

        data.masterVolume = Mathf.Clamp01(data.masterVolume);
        data.bgmVolume = Mathf.Clamp01(data.bgmVolume);
        data.sfxVolume = Mathf.Clamp01(data.sfxVolume);

        masterSlider.SetValueWithoutNotify(data.masterVolume);
        bgmSlider.SetValueWithoutNotify(data.bgmVolume);
        sfxSlider.SetValueWithoutNotify(data.sfxVolume);

        UpdatePercentLabel(masterValue, data.masterVolume);
        UpdatePercentLabel(bgmValue, data.bgmVolume);
        UpdatePercentLabel(sfxValue, data.sfxVolume);
    }

    private void UpdatePercentLabel(Label label, float value01)
    {
        if (label == null)
            return;

        label.text = Mathf.RoundToInt(Mathf.Clamp01(value01) * 100f).ToString();
    }

    private void SaveGlobal()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SaveGlobalNow();
    }
}
