using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class RegisterGameplayInput : MonoBehaviour
{
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (InputBlockService.Instance != null)
            InputBlockService.Instance.RegisterGameplayInput(playerInput);
    }

    private void OnDisable()
    {
        if (InputBlockService.Instance != null)
            InputBlockService.Instance.UnregisterGameplayInput(playerInput);
    }

    private void Start()
    {
        if (InputBlockService.Instance != null)
            InputBlockService.Instance.RegisterGameplayInput(playerInput);
    }
}