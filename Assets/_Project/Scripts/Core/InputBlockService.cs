using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputBlockService : MonoBehaviour
{
    public static InputBlockService Instance { get; private set; }
    public static bool IsBlocked => Instance != null && Instance._isBlocked;

    private readonly List<PlayerInput> gameplayInputs = new();
    private bool _isBlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterGameplayInput(PlayerInput playerInput)
    {
        if (playerInput == null)
            return;

        if (!gameplayInputs.Contains(playerInput))
            gameplayInputs.Add(playerInput);

        if (_isBlocked)
            playerInput.DeactivateInput();
    }

    public void UnregisterGameplayInput(PlayerInput playerInput)
    {
        if (playerInput == null)
            return;

        gameplayInputs.Remove(playerInput);
    }

    public void SetBlocked(bool blocked)
    {
        if (_isBlocked == blocked)
            return;

        _isBlocked = blocked;

        for (int i = gameplayInputs.Count - 1; i >= 0; i--)
        {
            var pi = gameplayInputs[i];

            if (pi == null)
            {
                gameplayInputs.RemoveAt(i);
                continue;
            }

            if (blocked) pi.DeactivateInput();
            else pi.ActivateInput();
        }
    }
}