using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcStoreItemRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    public void Bind(Sprite icon, string displayName, int price, System.Action onBuy)
    {
        if (iconImage != null) iconImage.sprite = icon;
        if (nameText != null) nameText.text = string.IsNullOrEmpty(displayName) ? "" : displayName;
        if (priceText != null) priceText.text = price.ToString();

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