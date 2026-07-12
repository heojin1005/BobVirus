using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 100f;
    private float currentHp;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    private EnemyShooterAI aiController; // 뇌

    private void Awake()
    {
        currentHp = maxHp;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        aiController = GetComponent<EnemyShooterAI>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void TakeDamage(float amount, Vector2 hitPoint, Vector2 hitNormal, GameObject attacker)
    {
        currentHp -= amount;

        // 피 튀기기
        if (BloodManager.Instance != null) BloodManager.Instance.SpawnBlood(hitPoint, hitNormal);

        // 피격 플래시
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            Invoke(nameof(ResetColor), 0.1f);
        }

        if (attacker != null)
        {
            var zombieAI = GetComponent<EnemyAI>();
            if (zombieAI != null) zombieAI.OnAttacked(attacker);

            var shooterAI = GetComponent<EnemyShooterAI>();
            if (shooterAI != null) shooterAI.OnAttacked(attacker);
        }

        if (currentHp <= 0) Die();
    }

    private void ResetColor()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        // 1. AI 뇌 끄기 (더 이상 안 움직임)
        if (aiController != null) aiController.enabled = false;
        
        // 2. 콜라이더 끄기 (더 이상 안 맞음)
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;

        // 3. 시체 스프라이트로 변경 (또는 래그돌)
        spriteRenderer.color = Color.gray; // 임시 시체 처리
        spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, 90f); // 쓰러짐 연출

        // 4. (옵션) 아이템 드롭 로직 호출
        // GetComponent<LootDrop>().DropItem();
    }
}