using System.Collections;
using UnityEngine;

public class CorruptExplosionController : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float delay = 1f;
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private float explosionDamage = 28f;
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private float shakeDuration = 0.28f;
    [SerializeField] private float shakeMagnitude = 0.45f;

    [Header("Death poison")]
    [SerializeField] private PoisonPuddle poisonPuddlePrefab;
    [SerializeField] private float deathPuddleRadius = 2.2f;
    [SerializeField] private float deathPuddleLifetime = 9f;
    [SerializeField] private float poisonDamagePerTick = 4f;
    [SerializeField] private float poisonTickInterval = 0.7f;

    private EnemyHealth health;
    private bool triggered;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Died -= OnDied;
    }

    public void TriggerDetonation()
    {
        if (triggered) return;
        triggered = true;
        if (health.IsAlive) health.Kill(gameObject);
        StartCoroutine(DetonationRoutine());
    }

    private void OnDied()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(DetonationRoutine());
    }

    private IEnumerator DetonationRoutine()
    {
        yield return new WaitForSeconds(delay);

        bool playerHit = ZombieCombatUtility.DamageCircle(transform.position, explosionRadius, playerLayers, explosionDamage, gameObject) > 0;
        if (playerHit && CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(shakeDuration, shakeMagnitude);

        if (poisonPuddlePrefab != null)
        {
            PoisonPuddle puddle = Instantiate(poisonPuddlePrefab, transform.position, Quaternion.identity);
            puddle.Initialize(deathPuddleRadius, deathPuddleLifetime, poisonDamagePerTick, poisonTickInterval, playerLayers, gameObject);
        }

        NoiseManager.MakeNoise(transform.position, 14f, gameObject);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.45f, 1f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
