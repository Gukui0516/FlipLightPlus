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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<NavMeshAgent>();

        // 2D 전환
        agent.updateRotation = false;
        agent.updateUpAxis   = false;

        // ★중요: NavMeshAgent가 포지션을 업데이트하지 않게 막는다.
        agent.updatePosition = false;

        // 불필요했던 과격 설정 되돌리기
        // agent.acceleration = 1000f;   // 제거
        // agent.angularSpeed = 0f;      // 필요하면 유지, 아니라면 기본값으로
        // agent.autoBraking  = false;   // 기본 동작으로
    }

    /// 에이전트 내부 좌표를 RB 위치와 동기화
    private void SyncAgentToRB()
    {
        if (agent != null && rb != null)
        {
            // nextPosition을 현재 실제 위치에 고정
            agent.nextPosition = rb.position;
        }
    }

    public void MoveTowardsPlayer(Transform player, float speed, float stoppingDistance)
    {
        if (!player || rb == null || agent == null) return;

        // 정지 상태면 해제
        if (agent.isStopped) agent.isStopped = false;

        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;

        // 목적지 갱신
        agent.SetDestination(player.position);

        // 먼저 내부 좌표 동기화
        SyncAgentToRB();

        // steeringTarget 쪽으로 RB만 이동
        Vector2 from = rb.position;
        Vector2 to   = (Vector2)agent.steeringTarget;
        if (Vector2.Distance(from, to) < arriveEpsilon)
            to = player.position;

        Vector2 dir = (to - from).normalized;
        rb.linearVelocity = dir * speed;

        // ★절대 세팅하지 말 것: agent.velocity = ...
        // 에이전트 이동은 막아놨으니, 내부 상태만 유지하면 된다.
    }

    public void StopImmediate()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            // 내부 좌표를 즉시 현재 위치로 고정
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
