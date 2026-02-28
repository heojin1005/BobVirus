using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcStorePanelUI : MonoBehaviour
{
    [System.Serializable]
    public class RowData
    {
        public string itemId;
        public int price;
    }

    [Header("Refs")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private NpcStoreItemRowUI rowPrefab;
    [SerializeField] private Button closeButton;

    [Header("Display DB")]
    [SerializeField] private ItemDatabase itemDatabase;

    private void Awake()
    {
        if (root != null) root.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Open(string npcDisplayName, List<RowData> rows, System.Action onClose)
    {
        if (root != null) root.SetActive(true);

        if (titleText != null)
            titleText.text = npcDisplayName + " Store";

        // Clear old
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        if (rows != null && rowPrefab != null)
        {
            if (itemDatabase != null)
                itemDatabase.BuildCacheIfNeeded();

            foreach (var r in rows)
            {
                var row = Instantiate(rowPrefab, contentRoot);

                string displayName = r.itemId;
                Sprite icon = null;

                if (itemDatabase != null)
                {
                    var def = itemDatabase.GetOrNull(r.itemId);
                    if (def != null)
                    {
                        displayName = string.IsNullOrEmpty(def.displayName)
                            ? r.itemId
                            : def.displayName;

                        icon = def.icon != null
                            ? def.icon
                            : itemDatabase.defaultItemIcon;
                    }
                    else
                    {
                        icon = itemDatabase.defaultItemIcon;
                    }
                }

                row.Bind(icon, displayName, r.price, null);
            }
        }

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
        if (root != null)
            root.SetActive(false);
    }
}