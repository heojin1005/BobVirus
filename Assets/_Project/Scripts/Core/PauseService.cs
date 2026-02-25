using System.Collections.Generic;
using UnityEngine;

public class PauseService : MonoBehaviour
{
    public static PauseService Instance { get; private set; }

    private readonly HashSet<string> reasons = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsPaused => reasons.Count > 0;

    public void Push(string reason)
    {
        reasons.Add(reason);
        Apply();
    }

    public void Pop(string reason)
    {
        reasons.Remove(reason);
        Apply();
    }

    private void Apply()
    {
        Time.timeScale = IsPaused ? 0f : 1f;
    }
}