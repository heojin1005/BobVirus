using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SettingsOverlayController : MonoBehaviour
{
    public static SettingsOverlayController Instance { get; private set; }

    private const string PauseReason = "SETTINGS_UI";

    [SerializeField] private StyleSheet settingsStyleSheet;

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

        ApplyStyleSheet(root);
        BindUI(root);
        ForceClosedVisual();
    }

    private void Update()
    {
        if (!isBound || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isOpen) CloseSettings();
            else OpenSettings();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnbindCallbacks();
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

        // 문서 루트 자체는 입력 무시
        root.pickingMode = PickingMode.Ignore;
        overlayRoot.pickingMode = PickingMode.Ignore;

        // 실제 오버레이는 열렸을 때 전체화면 클릭을 먹어서 뒤 UI 차단
        settingsOverlay.pickingMode = PickingMode.Position;
        settingsMainPanel.pickingMode = PickingMode.Position;
        soundPanel.pickingMode = PickingMode.Position;

        // 배경 클릭이 뒤로 전파되지 않게 막음
        settingsOverlay.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
        settingsOverlay.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
        settingsOverlay.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

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

        btnCloseSettings.clicked -= CloseSettings;
        btnOpenSound.clicked -= ShowSoundPanel;
        btnCloseSound.clicked -= CloseSettings;
        btnBackFromSound.clicked -= ShowMainPanel;

        masterSlider.UnregisterValueChangedCallback(OnMasterSliderChanged);
        bgmSlider.UnregisterValueChangedCallback(OnBgmSliderChanged);
        sfxSlider.UnregisterValueChangedCallback(OnSfxSliderChanged);
    }

    public void OpenSettings()
    {
        if (!isBound || isOpen)
            return;

        isOpen = true;

        RefreshFromGlobal();

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

        if (PauseService.Instance != null)
            PauseService.Instance.Pop(PauseReason);

        if (InputBlockService.Instance != null)
            InputBlockService.Instance.SetBlocked(false);
    }

    private void ForceClosedVisual()
    {
        settingsOverlay?.AddToClassList("hidden");
        settingsMainPanel?.AddToClassList("hidden");
        soundPanel?.AddToClassList("hidden");
    }

    private void ShowMainPanel()
    {
        settingsMainPanel.RemoveFromClassList("hidden");
        soundPanel.AddToClassList("hidden");
    }

    private void ShowSoundPanel()
    {
        settingsMainPanel.AddToClassList("hidden");
        soundPanel.RemoveFromClassList("hidden");
    }

    private void OnMasterSliderChanged(ChangeEvent<float> evt)
    {
        if (!TryGetGlobalData(out var data))
            return;

        data.masterVolume = evt.newValue;
        UpdatePercentLabel(masterValue, evt.newValue);
        SaveGlobal();
    }

    private void OnBgmSliderChanged(ChangeEvent<float> evt)
    {
        if (!TryGetGlobalData(out var data))
            return;

        data.bgmVolume = evt.newValue;
        UpdatePercentLabel(bgmValue, evt.newValue);
        SaveGlobal();
    }

    private void OnSfxSliderChanged(ChangeEvent<float> evt)
    {
        if (!TryGetGlobalData(out var data))
            return;

        data.sfxVolume = evt.newValue;
        UpdatePercentLabel(sfxValue, evt.newValue);
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