using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldDebugLabel : MonoBehaviour
{
    [Header("World Offset")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.5f, 0);

    [Header("Text Settings")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 28;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Camera cam;
    private Canvas canvas;
    private TextMeshProUGUI textUI;
    private CanvasGroup canvasGroup;
    private Coroutine showRoutine;

    private void Awake()
    {
        cam = Camera.main;

        GameObject canvasGO = new GameObject($"WorldDebugLabelCanvas_{name}");
        canvasGO.transform.SetParent(null);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(canvasGO.transform);

        textUI = textGO.AddComponent<TextMeshProUGUI>();
        textUI.alignment = TextAlignmentOptions.Center;
        textUI.color = textColor;
        textUI.fontSize = fontSize;
        textUI.raycastTarget = false;

        RectTransform rect = textUI.rectTransform;
        rect.sizeDelta = new Vector2(400, 120);

        canvasGroup.alpha = 0f;
        canvasGO.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!canvas.gameObject.activeSelf) return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Vector3 worldPos = transform.position + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z > 0)
        {
            textUI.rectTransform.position = screenPos;
        }
        else
        {
            canvas.gameObject.SetActive(false);
        }
    }

    public void Show(string message, float duration)
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        textUI.text = message;
        canvas.gameObject.SetActive(true);

        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
    }
}