using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NpcTopicPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text greetingText;   // 선택 (없어도 됨)

    [SerializeField] private Button talkButton;
    [SerializeField] private Button tradeButton;
    [SerializeField] private Button questButton;
    [SerializeField] private Button closeButton;

    // 현재 연결된 콜백들
    private Action onTalk;
    private Action onTrade;
    private Action onQuest;
    private Action onClose;

    private void Awake()
    {
        // 인스펙터 연결 실수 방지 로그
        if (npcNameText == null) Debug.LogWarning("NpcTopicPanel: npcNameText not assigned");
        if (talkButton == null) Debug.LogWarning("NpcTopicPanel: talkButton not assigned");
        if (tradeButton == null) Debug.LogWarning("NpcTopicPanel: tradeButton not assigned");
        if (questButton == null) Debug.LogWarning("NpcTopicPanel: questButton not assigned");
        if (closeButton == null) Debug.LogWarning("NpcTopicPanel: closeButton not assigned");
    }

    /// <summary>
    /// Topic 패널 열기
    /// </summary>
    public void Open(
        string npcId,
        string npcDisplayName,
        Action onTalk,
        Action onTrade,
        Action onQuest,
        Action onClose,
        string greeting = null
    )
    {
        this.onTalk = onTalk;
        this.onTrade = onTrade;
        this.onQuest = onQuest;
        this.onClose = onClose;

        // 이름 표시
        if (npcNameText != null)
            npcNameText.text = npcDisplayName;

        // 인삿말(선택)
        if (greetingText != null)
        {
            if (string.IsNullOrEmpty(greeting))
            {
                greetingText.gameObject.SetActive(false);
            }
            else
            {
                greetingText.gameObject.SetActive(true);
                greetingText.text = greeting;
            }
        }

        // 기존 리스너 제거 (중복 방지 핵심)
        talkButton.onClick.RemoveAllListeners();
        tradeButton.onClick.RemoveAllListeners();
        questButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();

        // 새 리스너 연결
        talkButton.onClick.AddListener(() => this.onTalk?.Invoke());
        tradeButton.onClick.AddListener(() => this.onTrade?.Invoke());
        questButton.onClick.AddListener(() => this.onQuest?.Invoke());
        closeButton.onClick.AddListener(() => this.onClose?.Invoke());

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 패널 닫기 (Root는 NpcUIManager에서 끄는 걸 권장)
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }
}
