// StorePanelUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorePanelUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject rowPrefab; // (Text 2개: itemName, price)

    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public class Row
    {
        public string itemId;
        public int price;
        public string displayName; // 지금은 itemId 그대로 써도 됨(나중에 ItemDatabase 연결)
    }

    public void Open(string npcDisplayName, List<Row> rows, System.Action onClose = null)
    {
        if (root != null) root.SetActive(true);

        if (titleText != null)
            titleText.text = $"{npcDisplayName} - 상점";

        // 기존 row 제거
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        // row 생성
        if (rows != null && rowPrefab != null)
        {
            foreach (var r in rows)
            {
                var go = Instantiate(rowPrefab, contentRoot);
                var texts = go.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length >= 2)
                {
                    texts[0].text = r.displayName;
                    texts[1].text = r.price.ToString();
                }
            }
        }

        // 닫기 콜백(원하면)
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                Close();
                onClose?.Invoke();
            });
        }
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }
}