using UnityEngine;
using UnityEngine.AI;

public interface IDamageable
{
    void TakeDamage(float amount);
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    private enum State { Idle, Chase, Attack, Dead }

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
    private State state = State.Idle;
    private float health;
    private float lastAttackTime = -999f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        health = maxHealth;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    private void Update()
    {
    //    if (state == State.Dead || target == null) return;

    //    float distance = Vector3.Distance(transform.position, target.position);

    //    if (distance <= attackRange) state = State.Attack;
    //    else if (distance <= detectionRange) state = State.Chase;
    //    else state = State.Idle;

    //    // CRITICAL FIX: Ensure agent is initialized, enabled, and physically standing on a baked NavMesh floor
    //    if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

    //    switch (state)
    //    {
    //        case State.Idle:
    //            // Only reset path if the agent is actively calculating or moving to prevent spamming
    //            if (agent.hasPath || agent.pathPending)
    //            {
    //                agent.ResetPath();
    //            }
    //            break;

    //        case State.Chase:
    //            agent.SetDestination(target.position);
    //            break;

    //        case State.Attack:
    //            if (agent.hasPath || agent.pathPending)
    //            {
    //                agent.ResetPath();
    //            }
    //            FaceTarget();
    //            TryAttack();
    //            break;
    //    }
    }


    private void FaceTarget()
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
    }

    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        if (target.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;

        health -= amount;
        if (health <= 0f) Die();
    }

    private void Die()
    {
        state = State.Dead;
        agent.enabled = false;
        if (TryGetComponent(out Collider col)) col.enabled = false;
        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}