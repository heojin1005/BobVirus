using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcStorePanelUI : MonoBehaviour
{
    [System.Serializable]
    public class RowData
    {
        public string takeItemId;
        public int takeCount;

        public string giveItemId;
        public int giveCount;

        public string buttonLabel;
        public System.Action onClick;
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
        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Open(string npcDisplayName, List<RowData> rows, System.Action onClose)
    {
        if (root != null)
            root.SetActive(true);

        if (titleText != null)
            titleText.text = npcDisplayName + " Store";

        if (contentRoot == null)
        {
            Debug.LogError("[NpcStorePanelUI] contentRoot is null.");
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        if (itemDatabase != null)
            itemDatabase.BuildCacheIfNeeded();

        if (rows != null && rowPrefab != null)
        {
            foreach (var r in rows)
            {
                var row = Instantiate(rowPrefab, contentRoot);

                string giveDisplayName = r.giveItemId;
                Sprite giveIcon = null;
                Sprite takeIcon = null;

                if (itemDatabase != null)
                {
                    var giveDef = itemDatabase.GetOrNull(r.giveItemId);
                    if (giveDef != null)
                    {
                        giveDisplayName = string.IsNullOrEmpty(giveDef.displayName)
                            ? r.giveItemId
                            : giveDef.displayName;

                        giveIcon = giveDef.icon != null
                            ? giveDef.icon
                            : itemDatabase.defaultItemIcon;
                    }
                    else
                    {
                        giveIcon = itemDatabase.defaultItemIcon;
                    }

                    var takeDef = itemDatabase.GetOrNull(r.takeItemId);
                    if (takeDef != null)
                    {
                        takeIcon = takeDef.icon != null
                            ? takeDef.icon
                            : itemDatabase.defaultItemIcon;
                    }
                    else
                    {
                        takeIcon = itemDatabase.defaultItemIcon;
                    }
                }

                row.Bind(
                    giveIcon,
                    giveDisplayName,
                    r.giveCount,
                    takeIcon,
                    r.takeCount,
                    r.buttonLabel,
                    r.onClick
                );
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