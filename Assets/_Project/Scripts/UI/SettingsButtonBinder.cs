using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SettingsButtonBinder : MonoBehaviour
{
    [SerializeField] private StyleSheet settingsStyleSheet;

    private UIDocument uiDocument;
    private VisualElement settingsButtonRoot;
    private Button btnOpenSettings;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[SettingsButtonBinder] rootVisualElement가 없습니다.");
            return;
        }

        ApplyStyleSheet(root);

        settingsButtonRoot = root.Q<VisualElement>("settingsButtonRoot");
        btnOpenSettings = root.Q<Button>("btnOpenSettings");

        if (settingsButtonRoot == null || btnOpenSettings == null)
        {
            Debug.LogError("[SettingsButtonBinder] UXML 요소 바인딩 실패");
            return;
        }

        // 전체 화면 루트는 입력을 먹지 않게
        root.pickingMode = PickingMode.Ignore;
        settingsButtonRoot.pickingMode = PickingMode.Ignore;

        // 실제 버튼만 입력 받기
        btnOpenSettings.pickingMode = PickingMode.Position;

        btnOpenSettings.clicked += HandleOpen;
    }

    private void OnDestroy()
    {
        if (btnOpenSettings != null)
            btnOpenSettings.clicked -= HandleOpen;
    }

    private void ApplyStyleSheet(VisualElement root)
    {
        if (settingsStyleSheet == null)
        {
            Debug.LogError("[SettingsButtonBinder] settingsStyleSheet가 비어 있습니다.");
            return;
        }

        if (!root.styleSheets.Contains(settingsStyleSheet))
            root.styleSheets.Add(settingsStyleSheet);
    }

    private void HandleOpen()
    {
        if (SettingsOverlayController.Instance == null)
        {
            Debug.LogWarning("[SettingsButtonBinder] SettingsOverlayController.Instance가 없습니다.");
            return;
        }

        SettingsOverlayController.Instance.OpenSettings();
    }
}