using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 100f;

    private float currentHp;
    private bool isDead;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float HealthRatio => maxHp > 0f ? currentHp / maxHp : 0f;
    public bool IsAlive => !isDead;

    private void Awake()
    {
        currentHp = maxHp;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void TakeDamage(float amount, Vector2 hitPoint, Vector2 hitNormal, GameObject attacker)
    {
        if (isDead || amount <= 0f) return;

        currentHp = Mathf.Max(0f, currentHp - amount);
        HealthChanged?.Invoke(currentHp, maxHp);

        if (BloodManager.Instance != null) BloodManager.Instance.SpawnBlood(hitPoint, hitNormal);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 0.1f);
        }

        if (attacker != null)
        {
            EnemyAI zombieAI = GetComponent<EnemyAI>();
            if (zombieAI != null) zombieAI.OnAttacked(attacker);

            EnemyShooterAI shooterAI = GetComponent<EnemyShooterAI>();
            if (shooterAI != null) shooterAI.OnAttacked(attacker);

            SpecialZombieAI specialAI = GetComponent<SpecialZombieAI>();
            if (specialAI != null) specialAI.OnAttacked(attacker);
        }

        if (currentHp <= 0f) Die();
    }

    public void Kill(GameObject attacker = null)
    {
        if (isDead) return;
        currentHp = 0f;
        HealthChanged?.Invoke(currentHp, maxHp);
        Die();
    }

    private void ResetColor()
    {
        if (!isDead && spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        CancelInvoke(nameof(ResetColor));

        // Death effects subscribe here and remain enabled after movement brains stop.
        Died?.Invoke();

        EnemyAI meleeAI = GetComponent<EnemyAI>();
        if (meleeAI != null) meleeAI.enabled = false;
        EnemyShooterAI shooterAI = GetComponent<EnemyShooterAI>();
        if (shooterAI != null) shooterAI.enabled = false;
        SpecialZombieAI specialAI = GetComponent<SpecialZombieAI>();
        if (specialAI != null) specialAI.enabled = false;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders) collider.enabled = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
            spriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }
    }
}
