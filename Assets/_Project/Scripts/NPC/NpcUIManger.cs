using System.Collections;
using UnityEngine;

public class NpcUIManager : MonoBehaviour
{
    public static NpcUIManager Instance { get; private set; }

    [SerializeField] private GameObject npcInteractionRoot;
    [SerializeField] private NpcTopicPanel topicPanel;

    [Header("Dimmer Block")]
    [SerializeField] private CanvasGroup dimmerCanvasGroup; // Dimmer에 붙인 CanvasGroup
    [SerializeField] private float dimmerEnableDelay = 0.05f; // 1프레임이면 0f도 OK (하지만 0.05 추천)

    private const string PauseReason = "NPC_UI";
    private Coroutine dimmerRoutine;

    private void Awake()
    {
        Instance = this;
        if (npcInteractionRoot != null) npcInteractionRoot.SetActive(false);
    }

    public void OpenTopic(string npcId, string npcDisplayName,
        System.Action onTalk, System.Action onTrade, System.Action onQuest)
    {
        PauseService.Instance?.Push(PauseReason);

        npcInteractionRoot.SetActive(true);

        // ✅ 열자마자 같은 클릭으로 닫히는 거 방지
        ArmDimmerClickNextFrame();

        topicPanel.Open(
            npcId,
            npcDisplayName,
            onTalk,
            onTrade,
            onQuest,
            () => CloseAll()
        );
    }

    private void ArmDimmerClickNextFrame()
    {
        if (dimmerCanvasGroup == null) return;

        if (dimmerRoutine != null) StopCoroutine(dimmerRoutine);

        // 지금 프레임의 클릭 이벤트가 Dimmer로 번지는 걸 막음
        dimmerCanvasGroup.blocksRaycasts = false;
        dimmerCanvasGroup.interactable = false;

        dimmerRoutine = StartCoroutine(EnableDimmerAfterDelay());
    }

    private IEnumerator EnableDimmerAfterDelay()
    {
        // “진짜 1프레임 뒤”만 원하면: yield return null;
        // 시간 기준으로 안전하게 하려면 unscaled로 약간 기다림:
        float t = 0f;
        while (t < dimmerEnableDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        dimmerCanvasGroup.blocksRaycasts = true;
        dimmerCanvasGroup.interactable = true;
        dimmerRoutine = null;
    }

    public void CloseAll()
    {
        npcInteractionRoot.SetActive(false);
        PauseService.Instance?.Pop(PauseReason);
    }
}
