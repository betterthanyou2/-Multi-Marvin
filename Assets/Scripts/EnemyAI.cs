using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("Settings")]
    [SerializeField] private float patrolWaitTime = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentPatrolIndex;
    private bool IsWaiting;

    private IEnumerator Start()
    {
        IsWaiting = true;
        agent.isStopped = true;
    }

    private void GoToNextPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

}
