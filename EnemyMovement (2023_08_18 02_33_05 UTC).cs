using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    private Gun gun;
    public float runningMovementSpeed;
    public float detectionDistance;
    public LayerMask obstacleMask;
    public float loseSightDelay = 2f;
    public float wanderRadius = 10f;
    private float wanderInterval;

    private Transform player;
    private NavMeshAgent navAgent;
    private Animator animator;
    private bool follow = false;
    private float timeSinceLastSeen = 0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        gun = FindObjectOfType<Gun>();
        
        follow = false; // Ensure enemies start wandering
        StartCoroutine(Wander());
    }

    private void Update()
    {
        UpdateAnimation();

        if (gameObject.CompareTag("Zombie"))
        {
            if (gun.gungrabbed)
            {
                detectionDistance += 5;
                runningMovementSpeed += 5;
                loseSightDelay += 1000;
                navAgent.speed += 3;
            }
            else
            {
                HandleFollowing();
            }
        }

        if (gameObject.CompareTag("Skeleton"))
        {
            HandleFollowing();
        }
    }

    private void UpdateAnimation()
    {
        if (navAgent.velocity.magnitude > 0.1f)
        {
            animator.SetBool("Moving", true);
        }
        else
        {
            animator.SetBool("Moving", false);
        }
    }

    private void HandleFollowing()
    {
        if (Vector3.Distance(transform.position, player.position) <= detectionDistance && LOS())
        {
            AggressiveState();
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;

            if (timeSinceLastSeen > loseSightDelay && follow)
            {
                Debug.Log("Lost sight of player. Starting to wander.");
                follow = false;
                StartWandering();
            }
        }
    }

    private bool LOS()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        float fieldOfViewAngle = 135f;

        if (angleToPlayer < fieldOfViewAngle / 2)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, obstacleMask))
            {
                return true;
            }
        }

        return false;
    }

    private void AggressiveState()
    {
        Debug.Log("Entering Aggressive State");
        follow = true;
        timeSinceLastSeen = 0f;
        StopAllCoroutines();
        navAgent.speed = runningMovementSpeed;

        if (Vector3.Distance(transform.position, player.position) > navAgent.stoppingDistance)
        {
            navAgent.SetDestination(player.position);
        }
        else
        {
            navAgent.ResetPath();
            RotateTowards(player.position);
        }
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private void StartWandering()
    {
        StopAllCoroutines();
        StartCoroutine(Wander());
    }

    private IEnumerator Wander()
    {
        Debug.Log("Starting to wander.");

        while (!follow)
        {
            Vector3 randomTarget = GetRandomWanderTarget();
            navAgent.SetDestination(randomTarget);

            wanderInterval = Random.Range(5f, 10f);
            //Debug.Log($"Wandering to {randomTarget} for {wanderInterval} seconds.");

            yield return new WaitForSeconds(wanderInterval);
        }
    }

    private Vector3 GetRandomWanderTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            //Debug.Log($"Found valid wander target at {hit.position}");
            return hit.position;
        }

        Debug.Log("Failed to find a valid wander target. Staying in place.");
        return transform.position;
    }
}
