using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(EnemyPerception), typeof(EnemyHealth))]
public abstract class SpecialZombieAI : MonoBehaviour, IZombieAlertReceiver
{
    protected enum State { Idle, Investigate, Chase, Ability, Dead }

    [Header("Shared movement")]
    [SerializeField] protected float patrolSpeed = 1.5f;
    [SerializeField] protected float chaseSpeed = 3f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private Vector2 wanderWaitRange = new Vector2(2f, 5f);
    [SerializeField] private float targetMemory = 2f;
    [SerializeField] private float investigateWait = 2f;
    [SerializeField] private float footstepInterval = 0.65f;
    [SerializeField] private float footstepNoiseRange = 5f;

    [Header("Shared references")]
    [SerializeField] protected EnemyPerception perception;
    [SerializeField] protected SpriteRenderer bodyRenderer;

    protected NavMeshAgent Agent { get; private set; }
    protected EnemyHealth Health { get; private set; }
    protected Transform Target { get; private set; }
    protected State CurrentState { get; private set; }

    private Vector3 homePosition;
    private Vector3 investigatePosition;
    private float stateTimer;
    private float lostTargetTimer;
    private float footstepTimer;
    private float nextSenseTime;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Health = GetComponent<EnemyHealth>();
        if (perception == null) perception = GetComponent<EnemyPerception>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        Agent.updateRotation = false;
        Agent.updateUpAxis = false;
        homePosition = transform.position;
    }

    protected virtual void OnEnable()
    {
        NoiseManager.OnNoiseGenerated += OnNoiseGenerated;
        SetState(State.Idle);
    }

    protected virtual void OnDisable()
    {
        NoiseManager.OnNoiseGenerated -= OnNoiseGenerated;
    }

    protected virtual void Update()
    {
        if (!Health.IsAlive)
        {
            SetState(State.Dead);
            return;
        }

        if (CurrentState != State.Ability)
            SenseTarget();

        switch (CurrentState)
        {
            case State.Idle: TickIdle(); break;
            case State.Investigate: TickInvestigate(); break;
            case State.Chase: TickChase(); break;
        }

        UpdateFacing();
        EmitFootsteps();
    }

    public virtual void OnAttacked(GameObject attacker)
    {
        if (attacker == null || IsZombie(attacker.transform)) return;
        Target = FindTaggedRoot(attacker.transform, "Player");
        if (Target == null) Target = attacker.transform;
        lostTargetTimer = 0f;
        if (CurrentState != State.Ability) SetState(State.Chase);
    }

    public void ReceiveZombieAlert(Vector3 alertPosition, Transform targetHint)
    {
        if (!enabled || !Health.IsAlive || CurrentState == State.Ability) return;

        if (targetHint != null)
        {
            Target = targetHint;
            lostTargetTimer = 0f;
            SetState(State.Chase);
        }
        else
        {
            investigatePosition = alertPosition;
            SetState(State.Investigate);
        }
    }

    protected abstract void TickCombat(float distanceToTarget);

    protected bool BeginAbility()
    {
        if (CurrentState == State.Ability || CurrentState == State.Dead) return false;
        SetState(State.Ability);
        return true;
    }

    protected void EndAbility()
    {
        if (!Health.IsAlive) SetState(State.Dead);
        else if (Target != null) SetState(State.Chase);
        else SetState(State.Idle);
    }

    protected void StopMovement()
    {
        if (!Agent.enabled || !Agent.isOnNavMesh) return;
        Agent.isStopped = true;
        Agent.ResetPath();
        Agent.velocity = Vector3.zero;
    }

    protected void ResumeMovement(float speed)
    {
        if (!Agent.enabled || !Agent.isOnNavMesh) return;
        Agent.isStopped = false;
        Agent.speed = speed;
    }

    protected void MoveTo(Vector3 position, float speed)
    {
        if (!Agent.enabled || !Agent.isOnNavMesh) return;
        Agent.isStopped = false;
        Agent.speed = speed;
        Agent.SetDestination(position);
    }

    private void TickIdle()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f || (Agent.hasPath && Agent.remainingDistance > Agent.stoppingDistance)) return;

        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = homePosition + new Vector3(offset.x, offset.y, 0f);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            MoveTo(hit.position, patrolSpeed);
        stateTimer = Random.Range(wanderWaitRange.x, wanderWaitRange.y);
    }

    private void TickInvestigate()
    {
        MoveTo(investigatePosition, chaseSpeed);
        if (Agent.pathPending || Agent.remainingDistance > Agent.stoppingDistance + 0.2f) return;
        stateTimer += Time.deltaTime;
        if (stateTimer >= investigateWait) SetState(State.Idle);
    }

    private void TickChase()
    {
        if (Target == null || !Target.gameObject.activeInHierarchy)
        {
            investigatePosition = transform.position;
            Target = null;
            SetState(State.Investigate);
            return;
        }

        Transform visible = GetVisibleTargetThrottled();
        if (visible != null)
        {
            Target = visible;
            lostTargetTimer = 0f;
        }
        else
        {
            lostTargetTimer += Time.deltaTime;
            if (lostTargetTimer >= targetMemory)
            {
                investigatePosition = Target.position;
                Target = null;
                SetState(State.Investigate);
                return;
            }
        }

        TickCombat(Vector2.Distance(transform.position, Target.position));
    }

    private void SenseTarget()
    {
        Transform visible = GetVisibleTargetThrottled();
        if (visible == null) return;
        Target = visible;
        lostTargetTimer = 0f;
        if (CurrentState == State.Idle || CurrentState == State.Investigate)
        {
            SetState(State.Chase);
            NoiseManager.MakeNoise(transform.position, 10f, gameObject);
        }
    }

    private Transform GetVisibleTargetThrottled()
    {
        if (Time.time < nextSenseTime) return perception.currentTarget;
        nextSenseTime = Time.time + 0.1f;
        return perception.GetVisibleTarget();
    }

    private void OnNoiseGenerated(Vector3 position, float range, GameObject source)
    {
        if (CurrentState == State.Chase || CurrentState == State.Ability || CurrentState == State.Dead) return;
        if (source != null && IsZombie(source.transform)) return;
        if (Vector2.Distance(transform.position, position) > range) return;
        investigatePosition = position;
        SetState(State.Investigate);
    }

    private void SetState(State next)
    {
        if (CurrentState == next) return;
        CurrentState = next;
        stateTimer = next == State.Idle ? Random.Range(wanderWaitRange.x, wanderWaitRange.y) : 0f;
        if (next == State.Idle) ResumeMovement(patrolSpeed);
        else if (next == State.Investigate || next == State.Chase) ResumeMovement(chaseSpeed);
        else StopMovement();
    }

    private void UpdateFacing()
    {
        if (bodyRenderer == null) return;
        Vector3 direction = Agent.velocity;
        if (direction.sqrMagnitude < 0.01f && Target != null) direction = Target.position - transform.position;
        if (Mathf.Abs(direction.x) > 0.01f) bodyRenderer.flipX = direction.x < 0f;
    }

    private void EmitFootsteps()
    {
        if (CurrentState == State.Ability || Agent.velocity.sqrMagnitude < 0.1f) return;
        footstepTimer += Time.deltaTime;
        if (footstepTimer < footstepInterval) return;
        footstepTimer = 0f;
        NoiseManager.MakeNoise(transform.position, footstepNoiseRange, gameObject);
    }

    private static bool IsZombie(Transform candidate)
    {
        return FindTaggedRoot(candidate, "Zombie") != null;
    }

    private static Transform FindTaggedRoot(Transform candidate, string tag)
    {
        while (candidate != null)
        {
            if (candidate.CompareTag(tag)) return candidate;
            candidate = candidate.parent;
        }
        return null;
    }
}
