using System.Collections;
using UnityEngine;

public class BruteZombieAI : SpecialZombieAI
{
    [Header("Rage")]
    [SerializeField, Range(0.05f, 0.95f)] private float rageHealthRatio = 0.35f;
    [SerializeField] private float rageSpeedMultiplier = 1.35f;
    [SerializeField] private float rageCooldownMultiplier = 0.65f;
    [SerializeField] private float rageTransitionDuration = 2f;

    [Header("Charge")]
    [SerializeField] private float chargeMinRange = 2.5f;
    [SerializeField] private float chargeMaxRange = 7f;
    [SerializeField] private float chargeSpeed = 9f;
    [SerializeField] private float chargeWindup = 0.45f;
    [SerializeField] private float chargeDuration = 0.9f;
    [SerializeField] private float chargeHitRadius = 0.65f;
    [SerializeField] private float chargeDamage = 24f;
    [SerializeField] private float chargeCooldown = 4.5f;

    [Header("Ground slam")]
    [SerializeField] private float slamRadius = 2.1f;
    [SerializeField] private float slamDamage = 18f;
    [SerializeField] private float slamWindup = 0.65f;
    [SerializeField] private float slamCooldown = 3.5f;
    [SerializeField] private float slamShakeDuration = 0.22f;
    [SerializeField] private float slamShakeMagnitude = 0.35f;

    [Header("Targets")]
    [SerializeField] private LayerMask playerLayers;

    [Header("Presentation")]
    [SerializeField] private BruteSpriteAnimator spriteAnimator;
    [SerializeField] private BruteRageEffect rageEffect;

    private float nextChargeTime;
    private float nextSlamTime;
    private bool enraged;
    private bool ragePending;
    private bool rageTransitioning;

    protected override void Awake()
    {
        base.Awake();
        if (spriteAnimator == null) spriteAnimator = GetComponent<BruteSpriteAnimator>();
        if (rageEffect == null) rageEffect = GetComponent<BruteRageEffect>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Health.HealthChanged += OnHealthChanged;
    }

    protected override void OnDisable()
    {
        Health.HealthChanged -= OnHealthChanged;
        if (!Health.IsAlive)
        {
            if (spriteAnimator != null) spriteAnimator.enabled = false;
            if (rageEffect != null) rageEffect.Deactivate();
        }
        base.OnDisable();
    }

    protected override void Update()
    {
        base.Update();
        if (!Health.IsAlive) return;

        if (ragePending && !enraged && !rageTransitioning && CurrentState != State.Ability)
        {
            StartCoroutine(RageRoutine());
            return;
        }

        if (spriteAnimator != null && CurrentState != State.Ability)
            spriteAnimator.SetLocomotion(Agent.velocity.sqrMagnitude > 0.1f);
    }

    protected override void TickCombat(float distanceToTarget)
    {
        float cooldownScale = enraged ? rageCooldownMultiplier : 1f;
        if (distanceToTarget <= slamRadius && Time.time >= nextSlamTime)
        {
            nextSlamTime = Time.time + slamCooldown * cooldownScale;
            StartCoroutine(SlamRoutine());
            return;
        }

        if (distanceToTarget >= chargeMinRange && distanceToTarget <= chargeMaxRange && Time.time >= nextChargeTime)
        {
            nextChargeTime = Time.time + chargeCooldown * cooldownScale;
            StartCoroutine(ChargeRoutine());
            return;
        }

        MoveTo(Target.position, chaseSpeed * (enraged ? rageSpeedMultiplier : 1f));
    }

    private void OnHealthChanged(float current, float maximum)
    {
        if (enraged || maximum <= 0f || current / maximum > rageHealthRatio) return;
        ragePending = true;
    }

    private IEnumerator RageRoutine()
    {
        if (!BeginAbility()) yield break;
        rageTransitioning = true;
        StopMovement();
        if (spriteAnimator != null) spriteAnimator.PlayRage(rageTransitionDuration);

        yield return new WaitForSeconds(rageTransitionDuration);
        if (!Health.IsAlive) yield break;

        enraged = true;
        ragePending = false;
        rageTransitioning = false;
        if (rageEffect != null) rageEffect.Activate();
        if (spriteAnimator != null) spriteAnimator.ClearAbility();
        NoiseManager.MakeNoise(transform.position, 12f, gameObject);
        EndAbility();
    }

    private IEnumerator ChargeRoutine()
    {
        if (!BeginAbility()) yield break;
        StopMovement();
        if (spriteAnimator != null) spriteAnimator.PlayChargePreparation();
        yield return new WaitForSeconds(chargeWindup);
        if (!Health.IsAlive || Target == null)
        {
            if (spriteAnimator != null) spriteAnimator.ClearAbility();
            EndAbility();
            yield break;
        }

        Vector2 direction = (Target.position - transform.position).normalized;
        Vector3 destination = transform.position + (Vector3)(direction * chargeMaxRange);
        if (spriteAnimator != null) spriteAnimator.PlayChargeLoop();
        ResumeMovement(chargeSpeed * (enraged ? rageSpeedMultiplier : 1f));
        Agent.SetDestination(destination);

        float timer = 0f;
        bool dealtDamage = false;
        while (timer < chargeDuration && Health.IsAlive)
        {
            timer += Time.deltaTime;
            if (!dealtDamage && ZombieCombatUtility.DamageCircle(transform.position, chargeHitRadius, playerLayers, chargeDamage, gameObject) > 0)
                dealtDamage = true;
            if (!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance) break;
            yield return null;
        }

        if (spriteAnimator != null) spriteAnimator.ClearAbility();
        EndAbility();
    }

    private IEnumerator SlamRoutine()
    {
        if (!BeginAbility()) yield break;
        StopMovement();
        if (spriteAnimator != null) spriteAnimator.PlaySlamWindup();
        yield return new WaitForSeconds(slamWindup);
        if (!Health.IsAlive)
        {
            if (spriteAnimator != null) spriteAnimator.ClearAbility();
            EndAbility();
            yield break;
        }

        if (spriteAnimator != null) spriteAnimator.PlaySlamImpact();
        bool playerInArea = ZombieCombatUtility.ContainsDamageable(transform.position, slamRadius, playerLayers);
        ZombieCombatUtility.DamageCircle(transform.position, slamRadius, playerLayers, slamDamage, gameObject);
        if (playerInArea && CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(slamShakeDuration, slamShakeMagnitude);
        NoiseManager.MakeNoise(transform.position, 14f, gameObject);
        yield return new WaitForSeconds(0.15f);
        if (spriteAnimator != null) spriteAnimator.ClearAbility();
        EndAbility();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}
