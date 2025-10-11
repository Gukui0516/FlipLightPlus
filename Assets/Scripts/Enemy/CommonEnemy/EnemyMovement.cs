using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private NavMeshAgent agent;

    [Header("Steering")]
    [SerializeField] private float arriveEpsilon = 0.1f;

    // ✅ 정지거리 감속을 위한 설정
    [Header("Stopping Behavior")]
    [SerializeField] private float decelerationDistance = 0.5f; // 감속 시작 거리

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.updatePosition = false;
    }

    private void SyncAgentToRB()
    {
        if (agent != null && rb != null)
        {
            agent.nextPosition = rb.position;
        }
    }

    public void MoveTowardsPlayer(Transform player, float speed, float stoppingDistance)
    {
        if (!player || rb == null || agent == null) return;

        if (agent.isStopped) agent.isStopped = false;

        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(player.position);

        SyncAgentToRB();

        Vector2 from = rb.position;
        Vector2 to = (Vector2)agent.steeringTarget;

        if (Vector2.Distance(from, to) < arriveEpsilon)
            to = player.position;

        float distanceToTarget = Vector2.Distance(from, to);

        // ✅ 정지거리 내에서 감속 처리
        float actualSpeed = speed;

        if (distanceToTarget <= stoppingDistance + decelerationDistance)
        {
            if (distanceToTarget <= stoppingDistance)
            {
                // 정지거리 내: 완전 정지
                rb.linearVelocity = Vector2.zero;
                return;
            }
            else
            {
                // 감속 구간: 거리에 비례해서 속도 감소
                float decelerationRatio = (distanceToTarget - stoppingDistance) / decelerationDistance;
                actualSpeed = speed * decelerationRatio;
            }
        }

        Vector2 dir = (to - from).normalized;
        rb.linearVelocity = dir * actualSpeed;
    }

    public void StopImmediate()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            SyncAgentToRB();
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void MoveForwardRB(float speed)
    {
        if (rb == null) return;
        Vector2 forward = (transform.rotation) * Vector2.down;
        rb.linearVelocity = forward.normalized * speed;
    }
}