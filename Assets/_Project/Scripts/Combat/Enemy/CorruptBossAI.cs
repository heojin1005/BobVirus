using UnityEngine;

public class CorruptBossAI : SpecialZombieAI
{
    [Header("Moving poison trail")]
    [SerializeField] private PoisonPuddle poisonPuddlePrefab;
    [SerializeField] private float trailInterval = 1.1f;
    [SerializeField] private float trailRadius = 0.8f;
    [SerializeField] private float trailLifetime = 5f;
    [SerializeField] private float trailDamagePerTick = 2.5f;
    [SerializeField] private float trailTickInterval = 0.8f;

    [Header("Contact detonation")]
    [SerializeField] private float contactRadius = 0.8f;
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private CorruptExplosionController explosionController;

    private float nextTrailTime;

    protected override void Awake()
    {
        base.Awake();
        if (explosionController == null) explosionController = GetComponent<CorruptExplosionController>();
    }

    protected override void Update()
    {
        base.Update();
        if (!Health.IsAlive || poisonPuddlePrefab == null || Agent.velocity.sqrMagnitude < 0.1f || Time.time < nextTrailTime) return;

        nextTrailTime = Time.time + trailInterval;
        PoisonPuddle puddle = Instantiate(poisonPuddlePrefab, transform.position, Quaternion.identity);
        puddle.Initialize(trailRadius, trailLifetime, trailDamagePerTick, trailTickInterval, playerLayers, gameObject);
    }

    protected override void TickCombat(float distanceToTarget)
    {
        if (distanceToTarget <= contactRadius && explosionController != null)
        {
            StopMovement();
            explosionController.TriggerDetonation();
            return;
        }

        MoveTo(Target.position, chaseSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.25f, 1f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, contactRadius);
    }
}
