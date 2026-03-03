using System;
using UnityEngine;
using UnityEngine.UI;

public class SplitDragPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;     // 패널 루트(Enable/Disable)
    [SerializeField] private Slider slider;       // 1 ~ (max-1)
    [SerializeField] private Text valueText;      // 선택 수량 표시
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action<int> onConfirm;
    private Action onCancel;
    private int maxCount;

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        if (slider != null) slider.onValueChanged.AddListener(_ => RefreshText());
        HideImmediate();
    }

    public void Show(int currentCount, Action<int> onConfirm, Action onCancel)
    {
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;

        maxCount = Mathf.Max(1, currentCount);

        // 분해는 최소 1개, 최대 (count-1)개까지
        int maxSplit = Mathf.Max(1, maxCount - 1);

        if (slider != null)
        {
            slider.minValue = 1;
            slider.maxValue = maxSplit;
            slider.wholeNumbers = true;
            slider.value = Mathf.Clamp(1, 1, maxSplit);
        }

        RefreshText();

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void HideImmediate()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void RefreshText()
    {
        if (valueText == null || slider == null) return;
        valueText.text = ((int)slider.value).ToString();
    }

    private void Confirm()
    {
        int v = 1;
        if (slider != null) v = Mathf.Max(1, (int)slider.value);

        HideImmediate();
        onConfirm?.Invoke(v);
        onConfirm = null;
        onCancel = null;
    }

    private void Cancel()
    {
        HideImmediate();
        onCancel?.Invoke();
        onConfirm = null;
        onCancel = null;
    }
}