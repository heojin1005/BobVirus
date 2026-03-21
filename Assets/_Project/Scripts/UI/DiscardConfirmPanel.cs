using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiscardConfirmPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirm;
    private Action onCancel;

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
    }

    public void Show(string message, Action onConfirm, Action onCancel)
    {
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;

        if (messageText != null)
            messageText.text = string.IsNullOrEmpty(message) ? "you drop item?" : message;

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void HideImmediate()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void Confirm()
    {
        HideImmediate();

        var cb = onConfirm;
        onConfirm = null;
        onCancel = null;
        cb?.Invoke();
    }

    private void Cancel()
    {
        HideImmediate();

        var cb = onCancel;
        onConfirm = null;
        onCancel = null;
        cb?.Invoke();
    }
}