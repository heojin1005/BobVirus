using UnityEngine;

public class TutorialCompletionReporter : MonoBehaviour
{
    public void CompleteTutorial()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[TutorialCompletionReporter] GameManager.Instance is null.");
            return;
        }

        GameManager.Instance.MarkTutorialCompleted();
        Debug.Log("[TutorialCompletionReporter] Tutorial completion saved.");
    }

    public void ResetTutorialForDev()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[TutorialCompletionReporter] GameManager.Instance is null.");
            return;
        }

        GameManager.Instance.ResetTutorialFlagForDev();
        Debug.Log("[TutorialCompletionReporter] Tutorial flag reset.");
    }
}