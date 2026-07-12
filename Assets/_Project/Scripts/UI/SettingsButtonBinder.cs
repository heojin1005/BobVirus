using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SettingsButtonBinder : MonoBehaviour
{
    private void Awake()
    {
        HideRoot();
    }

    private void OnEnable()
    {
        HideRoot();
    }

    private void Start()
    {
        HideRoot();
    }

    private void HideRoot()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
            return;

        var root = uiDocument.rootVisualElement;
        if (root != null)
            root.style.display = DisplayStyle.None;

        uiDocument.enabled = false;
    }
}
