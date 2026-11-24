using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrolling,
    Following,
    Attacking
}

public class EnemyAI : MonoBehaviour
{
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

    [Header("Reference")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Settings")]
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float losePlayerTime = 3f;
    [SerializeField] private float attackRange = 1.2f;

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyState state = EnemyState.Patrolling;
    private int currentPatrolIndex;
    private bool IsWaiting;
    private float timeSincePlayerLost;
    private bool _isBiting;


public interface IDamageable
{
    void TakeDamage(float amount);
}

[RequireComponent(typeof(NavMeshAgent))]
public class enemyAI : MonoBehaviour, IDamageable
{
    private float currentHealth;
    private bool isDead;
    public enum State 
    { 
            Idle, 
            Chase, 
            Attack, 
            Dead 
    }

    [Header("Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private Transform target; 

    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.2f;

    private NavMeshAgent agent;
        private Animator animator;
        private State state = State.Idle;
    private float health;
    private float lastAttackTime = -999f;
        private float timeSincePlayerLost;
        private float losePlayerTime;
        private bool _isBiting;

        public bool IsWaiting { get; private set; }

        public enemyAI(float currentHealth, bool isDead, float maxHealth, float moveSpeed, float detectionRange, float attackRange, Transform target, float attackDamage, float attackCooldown, NavMeshAgent agent, Animator animator, State state, float health, float lastAttackTime, float timeSincePlayerLost, float losePlayerTime)
        {
            this.currentHealth = currentHealth;
            this.isDead = isDead;
            this.maxHealth = maxHealth;
            this.moveSpeed = moveSpeed;
            this.detectionRange = detectionRange;
            this.attackRange = attackRange;
            this.target = target;
            this.attackDamage = attackDamage;
            this.attackCooldown = attackCooldown;
            this.agent = agent;
            this.animator = animator;
            this.state = state;
            this.health = health;
            this.lastAttackTime = lastAttackTime;
            this.timeSincePlayerLost = timeSincePlayerLost;
            this.losePlayerTime = losePlayerTime;
        }

        private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        var distanceToPlayer = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case (State)EnemyState.Patrolling:
                Patrol();
                if (distanceToPlayer <= detectionRange && CanSeePlayer())
                {
                    state = (State)EnemyState.Following;
                }
                break;

            case (State)EnemyState.Following:
                FollowPlayer();
                if (distanceToPlayer <= attackRange)
                {
                    state = (State)EnemyState.Attacking;
                    StartAttack();
                }
                if (!CanSeePlayer())
                {
                        timeSincePlayerLost += Time.deltaTime;
                    if (timeSincePlayerLost >= losePlayerTime)
                    {
                        state = (State)EnemyState.Patrolling;
                        GoToClosestPatrolPoint();
                    }          
                }
                else
                {
                    timeSincePlayerLost = 0f;
                }

                break;

            case (State)EnemyState.Attacking:
                Attack();
                if (!_isBiting && distanceToPlayer > attackRange)
                {
                    state = (State)EnemyState.Following;
                    agent.isStopped = false;
                }

                break;
        }

        UpdateAnimation();
    }

    private void StartAttack()
    {
        agent.isStopped = true;
        _isBiting = true;
        animator.SetTrigger("Bite");
    }

    private void Attack()
    {
        agent.isStopped = true;
        var direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnBiteAnimationEnd()
    {
        _isBiting = false;
    }

    private void FollowPlayer()
    {
        agent.SetDestination(player.position);
    }

    private void Patrol()
    {
        if (IsWaiting) return;
        if (!agent.pathPending && agent.remainingDistance <= stopAtDistance)
        {
            StartCoroutine(WaitForPatrolPoint());
        }
    }

    private IEnumerator WaitForPatrolPoint()
    {
        IsWaiting = true;
        agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);

        agent.isStopped = false;
        GoToNextPatrolPoint();
            IsWaiting = false;
    }

    private void GoToClosestPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        var closestIndex = 0;
        var closestDistance = float.MaxValue;

        for (var i = 0; i < patrolPoints.Length; i++)
        {
            var distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        currentPatrolIndex = closestIndex;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void UpdateAnimation()
    {
        var isWalking = agent.velocity.sqrMagnitude > 0.01f;
        animator.SetBool("isWalking", isWalking);
    }

    private bool CanSeePlayer()
    {
        return IsFacingPlayer() && HasClearPathToPlayer();
    }

    private bool IsFacingPlayer()
    {
        var dirToPlayer = (player.position - transform.position).normalized;
        var angle = Vector3.Angle(transform.forward, dirToPlayer);
        return angle <= viewAngle / 2f;
    }

    private bool HasClearPathToPlayer()
    {
        var dirToPlayer = player.position - transform.position;
        if (Physics.Raycast(transform.position, dirToPlayer.normalized, out RaycastHit hit, dirToPlayer.magnitude))
        {
            return hit.transform == player;
        }

        return true;
    }

        public void TakeDamage(float amount)
        {
            throw new System.NotImplementedException();
        }
}