using UnityEngine;

public class TestDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 100f;
    private float currentHp;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        currentHp = maxHp;
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 만약 자식에 스프라이트가 있다면 아래꺼 사용
        if(spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TakeDamage(float amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        currentHp -= amount;
        //Debug.Log($"[샌드백] 으악! {amount} 데미지! (남은 체력: {currentHp}/{maxHp})");

        // 시각 효과: 맞으면 잠깐 빨간색으로 번쩍임
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            Invoke(nameof(ResetColor), 0.1f);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void ResetColor()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        //Debug.Log("[샌드백] 사망...");
        gameObject.SetActive(false); // 비활성화 (나중에 시체로 바꾸는 로직 들어갈 곳)
    }
}