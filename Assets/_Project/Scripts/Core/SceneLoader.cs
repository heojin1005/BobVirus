using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
