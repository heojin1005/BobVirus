using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalTutorialBootstrap : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool developerForceResetTutorialOnPlay = false;
    [SerializeField] private string tutorialSceneName = "Tutorial";

    private bool executed = false;

    private void Start()
    {
        if (!runOnStart || executed)
            return;

        executed = true;

        if (GameManager.Instance == null)
        {
            Debug.LogError("[GlobalTutorialBootstrap] GameManager.Instance is null.");
            return;
        }

        if (developerForceResetTutorialOnPlay)
        {
            GameManager.Instance.ResetTutorialFlagForDev();
            Debug.Log("[GlobalTutorialBootstrap] Tutorial flag reset by developer option.");
        }

        var global = GameManager.Instance.GlobalData;
        if (global == null)
        {
            Debug.LogError("[GlobalTutorialBootstrap] GlobalData is null.");
            return;
        }

        if (!global.tutorialCompleted)
        {
            Debug.Log("[GlobalTutorialBootstrap] Tutorial not completed. Loading tutorial scene.");

            // 튜토리얼을 '실행 시작 시점'에 바로 true로 찍고 싶으면 여기서 저장
            // 클리어 시점에만 true로 찍고 싶으면 이 줄은 지우고, 클리어 처리 스크립트에서만 저장해라.
            //GameManager.Instance.MarkTutorialCompleted();

            SceneManager.LoadScene(tutorialSceneName);
            
        }
    }
}