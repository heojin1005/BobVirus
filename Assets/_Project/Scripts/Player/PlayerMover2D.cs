using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMover2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Rigidbody2D rb;

    private Vector2 moveInput;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}
