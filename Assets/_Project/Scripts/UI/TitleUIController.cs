using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TitleUIController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName = "Hub_main";

    [Header("Guide")]
    [SerializeField] private List<Texture2D> guideImages = new();

    private UIDocument doc;
    private VisualElement root;
    private VisualElement guideOverlay;
    private VisualElement guideImage;
    private Label guidePageLabel;
    private Label guideEmptyLabel;
    private int guideIndex;

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
        BindMenu();
        if (!enabled)
            return;

        BindGuide();
        if (!enabled)
            return;

        CloseGuide();
    }

    private void BindMenu()
    {
        var btnContinue = root.Q<Button>("btnContinue");
        var btnNewGame = root.Q<Button>("btnNewGame");
        var btnGuide = root.Q<Button>("btnGuide");
        var btnSettings = root.Q<Button>("btnSettings");
        var btnQuit = root.Q<Button>("btnQuit");

        if (btnContinue == null || btnNewGame == null || btnGuide == null || btnSettings == null || btnQuit == null)
        {
            Debug.LogError("[Title] Menu buttons not found. Check TitleView.uxml names.");
            enabled = false;
            return;
        }

        btnContinue.clicked += ContinueGame;
        btnNewGame.clicked += StartNewGame;
        btnGuide.clicked += OpenGuide;
        btnSettings.clicked += OpenSettings;
        btnQuit.clicked += Application.Quit;
    }

    private void BindGuide()
    {
        guideOverlay = root.Q<VisualElement>("guideOverlay");
        guideImage = root.Q<VisualElement>("guideImage");
        guidePageLabel = root.Q<Label>("guidePageLabel");
        guideEmptyLabel = root.Q<Label>("guideEmptyLabel");

        var btnPrevGuide = root.Q<Button>("btnPrevGuide");
        var btnNextGuide = root.Q<Button>("btnNextGuide");
        var btnCloseGuide = root.Q<Button>("btnCloseGuide");

        if (guideOverlay == null || guideImage == null || guidePageLabel == null ||
            guideEmptyLabel == null || btnPrevGuide == null || btnNextGuide == null || btnCloseGuide == null)
        {
            Debug.LogError("[Title] Guide UI elements not found. Check TitleView.uxml names.");
            enabled = false;
            return;
        }

        btnPrevGuide.clicked += ShowPreviousGuide;
        btnNextGuide.clicked += ShowNextGuide;
        btnCloseGuide.clicked += CloseGuide;
    }

    private void ContinueGame()
    {
        if (!EnsureGameManager())
            return;

        GameManager.Instance.ContinueDefaultProfile();
        SceneLoader.Load(nextSceneName);
    }

    private void StartNewGame()
    {
        if (!EnsureGameManager())
            return;

        GameManager.Instance.RestartDefaultProfile();
        SceneLoader.Load(nextSceneName);
    }

    private bool EnsureGameManager()
    {
        if (GameManager.Instance != null)
            return true;

        Debug.LogError("[Title] GameManager is missing. Start from Boot scene.");
        return false;
    }

    private void OpenSettings()
    {
        if (SettingsOverlayController.Instance == null)
        {
            Debug.LogWarning("[Title] SettingsOverlayController is missing.");
            return;
        }

        SettingsOverlayController.Instance.OpenSettings();
    }

    private void OpenGuide()
    {
        guideIndex = 0;
        guideOverlay.RemoveFromClassList("hidden");
        RefreshGuide();
    }

    private void CloseGuide()
    {
        guideOverlay?.AddToClassList("hidden");
    }

    private void ShowPreviousGuide()
    {
        if (guideImages == null || guideImages.Count == 0)
            return;

        guideIndex = (guideIndex - 1 + guideImages.Count) % guideImages.Count;
        RefreshGuide();
    }

    private void ShowNextGuide()
    {
        if (guideImages == null || guideImages.Count == 0)
            return;

        guideIndex = (guideIndex + 1) % guideImages.Count;
        RefreshGuide();
    }

    private void RefreshGuide()
    {
        bool hasImages = guideImages != null && guideImages.Count > 0;
        guideEmptyLabel.style.display = hasImages ? DisplayStyle.None : DisplayStyle.Flex;
        guideImage.style.display = hasImages ? DisplayStyle.Flex : DisplayStyle.None;

        if (!hasImages)
        {
            guidePageLabel.text = "0 / 0";
            guideImage.style.backgroundImage = new StyleBackground((Texture2D)null);
            return;
        }

        guideIndex = Mathf.Clamp(guideIndex, 0, guideImages.Count - 1);
        guideImage.style.backgroundImage = new StyleBackground(guideImages[guideIndex]);
        guidePageLabel.text = $"{guideIndex + 1} / {guideImages.Count}";
    }

    public void GoMain()
    {
        ContinueGame();
    }
}
