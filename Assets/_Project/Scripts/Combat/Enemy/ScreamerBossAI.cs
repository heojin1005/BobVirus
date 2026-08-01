using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreamerBossAI : SpecialZombieAI
{
    [Header("Scream")]
    [SerializeField] private float screamRadius = 8f;
    [SerializeField] private float screamWindup = 0.7f;
    [SerializeField] private float screamCooldown = 8f;
    [SerializeField] private float callRadius = 12f;
    [SerializeField] private float screenShakeDuration = 0.7f;
    [SerializeField] private float screenShakeMagnitude = 0.45f;

    [Header("Spit fan")]
    [SerializeField] private ZombieSpitProjectile spitProjectilePrefab;
    [SerializeField] private Transform spitOrigin;
    [SerializeField, Min(1)] private int projectileCount = 5;
    [SerializeField, Range(0f, 180f)] private float fanAngle = 55f;
    [SerializeField] private float spitRange = 7f;
    [SerializeField] private float spitSpeed = 3.5f;
    [SerializeField] private float spitDamage = 9f;
    [SerializeField] private float spitLifetime = 4f;
    [SerializeField] private float spitWindup = 0.45f;
    [SerializeField] private float spitCooldown = 4f;

    [Header("Targets")]
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private LayerMask zombieLayers;

    private float nextScreamTime;
    private float nextSpitTime;

    protected override void TickCombat(float distanceToTarget)
    {
        if (Time.time >= nextScreamTime && distanceToTarget <= screamRadius)
        {
            nextScreamTime = Time.time + screamCooldown;
            StartCoroutine(ScreamRoutine());
            return;
        }

        if (Time.time >= nextSpitTime && distanceToTarget <= spitRange && spitProjectilePrefab != null)
        {
            nextSpitTime = Time.time + spitCooldown;
            StartCoroutine(SpitRoutine());
            return;
        }

        MoveTo(Target.position, chaseSpeed);
    }

    private IEnumerator ScreamRoutine()
    {
        if (!BeginAbility()) yield break;
        StopMovement();
        yield return new WaitForSeconds(screamWindup);
        if (!Health.IsAlive) { EndAbility(); yield break; }

        if (ZombieCombatUtility.ContainsDamageable(transform.position, screamRadius, playerLayers) && CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(screenShakeDuration, screenShakeMagnitude);

        Transform targetHint = Target;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, callRadius, zombieLayers);
        HashSet<IZombieAlertReceiver> alerted = new HashSet<IZombieAlertReceiver>();
        foreach (Collider2D hit in nearby)
        {
            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                IZombieAlertReceiver receiver = behaviour as IZombieAlertReceiver;
                if (receiver == null || ReferenceEquals(receiver, this) || !alerted.Add(receiver)) continue;
                receiver.ReceiveZombieAlert(transform.position, targetHint);
            }
        }

        NoiseManager.MakeNoise(transform.position, callRadius, gameObject);
        yield return new WaitForSeconds(0.2f);
        EndAbility();
    }

    private IEnumerator SpitRoutine()
    {
        if (!BeginAbility()) yield break;
        StopMovement();
        yield return new WaitForSeconds(spitWindup);
        if (!Health.IsAlive || Target == null) { EndAbility(); yield break; }

        Vector3 origin = spitOrigin != null ? spitOrigin.position : transform.position;
        Vector2 centerDirection = (Target.position - origin).normalized;
        float startAngle = -fanAngle * 0.5f;
        float step = projectileCount > 1 ? fanAngle / (projectileCount - 1) : 0f;
        for (int i = 0; i < projectileCount; i++)
        {
            Vector2 direction = Quaternion.Euler(0f, 0f, startAngle + step * i) * centerDirection;
            ZombieSpitProjectile projectile = Instantiate(spitProjectilePrefab, origin, Quaternion.identity);
            projectile.Initialize(direction, spitSpeed, spitDamage, spitLifetime, playerLayers, gameObject);
        }

        yield return new WaitForSeconds(0.15f);
        EndAbility();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.2f, 0.9f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, screamRadius);
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, callRadius);
    }
}
