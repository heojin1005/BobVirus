using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcStoreItemRowUI : MonoBehaviour
{
    [Header("Give UI")]
    [SerializeField] private Image giveIconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text giveCountText;

    [Header("Take UI")]
    [SerializeField] private Image takeIconImage;
    [SerializeField] private TMP_Text takeCountText;

    [Header("Button UI")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buttonText;

    public void Bind(
        Sprite giveIcon,
        string giveDisplayName,
        int giveCount,
        Sprite takeIcon,
        int takeCount,
        string actionLabel,
        System.Action onBuy)
    {
        if (giveIconImage != null)
        {
            giveIconImage.sprite = giveIcon;
            giveIconImage.enabled = giveIcon != null;
        }

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(giveDisplayName) ? "" : giveDisplayName;

        if (giveCountText != null)
            giveCountText.text = $"x{Mathf.Max(1, giveCount)}";

        if (takeIconImage != null)
        {
            takeIconImage.sprite = takeIcon;
            takeIconImage.enabled = takeIcon != null;
        }

        if (takeCountText != null)
            takeCountText.text = $"x{Mathf.Max(1, takeCount)}";

        if (buttonText != null)
            buttonText.text = string.IsNullOrEmpty(actionLabel) ? "교환" : actionLabel;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();

            if (onBuy == null)
            {
                buyButton.interactable = false;
            }
            else
            {
                buyButton.interactable = true;
                buyButton.onClick.AddListener(() => onBuy.Invoke());
            }
        }
    }
}