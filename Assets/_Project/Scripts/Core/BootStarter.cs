using UnityEngine;

public class BootStarter : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "Title";

    private void Start()
    {
        SceneLoader.Load(firstSceneName);
    }
}
