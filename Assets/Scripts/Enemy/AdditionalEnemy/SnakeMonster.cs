using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeMonster : MonoBehaviour
{
    [Header("Snake Settings")]
    [SerializeField] private GameObject segmentPrefab; // 원형 스프라이트 프리팹
    [SerializeField] private int bodySegmentCount = 5; // 몸통 세그먼트 개수
    [SerializeField] private float segmentDistance = 0.5f; // 세그먼트 간 거리
    
    [Header("Movement Settings")]
    [SerializeField] private float aimSpeed = 2f; // 조준 시 회전 속도
    [SerializeField] private float chargeSpeed = 15f; // 돌진 속도
    [SerializeField] private float aimDuration = 1.5f; // 조준 시간
    
    [Header("References")]
    [SerializeField] private GameObject baitObject; // 꼬리의 미끼 오브젝트
    
    private List<Transform> segments = new List<Transform>();
    private List<Vector2> segmentPositions = new List<Vector2>();
    
    private enum State { Idle, Aiming, Charging }
    private State currentState = State.Idle;
    
    private Vector2 chargeTargetPosition;
    private Vector2 chargeDirection;
    private Transform playerTransform;

    void Start()
    {
        InitializeSnake();
    }

    void InitializeSnake()
    {
        // 머리는 현재 오브젝트
        segments.Add(transform);
        segmentPositions.Add(transform.position);
        
        // 몸통 세그먼트 생성
        for (int i = 0; i < bodySegmentCount; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position - Vector2.right * segmentDistance * (i + 1);
            GameObject segment = Instantiate(segmentPrefab, spawnPos, Quaternion.identity, transform.parent);
            segment.name = $"BodySegment_{i}";
            segments.Add(segment.transform);
            segmentPositions.Add(spawnPos);
        }
        
        // 꼬리 세그먼트 (미끼 부착)
        Vector2 tailPos = (Vector2)transform.position - Vector2.right * segmentDistance * (bodySegmentCount + 1);
        GameObject tail = Instantiate(segmentPrefab, tailPos, Quaternion.identity, transform.parent);
        tail.name = "TailSegment";
        segments.Add(tail.transform);
        segmentPositions.Add(tailPos);
        
        // 미끼를 꼬리에 부착
        if (baitObject != null)
        {
            baitObject.transform.SetParent(tail.transform);
            baitObject.transform.localPosition = Vector2.zero;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                // 대기 상태 - 필요시 순찰 로직 추가 가능
                break;
                
            case State.Aiming:
                AimAtPlayer();
                break;
                
            case State.Charging:
                ChargeAtPlayer();
                break;
        }
        
        UpdateSegments();
    }

    // 플레이어가 미끼에 손전등을 비췄을 때 호출
    public void OnBaitActivated()
    {
        if (currentState != State.Idle) return;
        
        // 플레이어 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (playerTransform == null)
        {
            // Tag로 못 찾으면 스크립트로 찾기
            var playerController = FindFirstObjectByType<PlayerControllerRB>();
            if (playerController != null)
                playerTransform = playerController.transform;
        }
        
        if (playerTransform != null)
        {
            StartCoroutine(AimAndCharge());
        }
    }

    IEnumerator AimAndCharge()
    {
        currentState = State.Aiming;
        
        // 조준 시간 동안 천천히 플레이어를 향해 회전
        float elapsed = 0f;
        Vector2 tailPosition = segments[segments.Count - 1].position; // 꼬리 위치 고정
        
        while (elapsed < aimDuration)
        {
            elapsed += Time.deltaTime;
            
            // 머리만 플레이어를 향해 회전 (꼬리 고정)
            Vector2 directionToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            float currentAngle = Mathf.Atan2(transform.up.y, transform.up.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle - 90f, Time.deltaTime * aimSpeed);
            
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
            
            yield return null;
        }
        
        // 돌진 준비
        chargeTargetPosition = playerTransform.position;
        chargeDirection = (chargeTargetPosition - (Vector2)transform.position).normalized;
        currentState = State.Charging;
    }

    void AimAtPlayer()
    {
        // 조준 중에는 꼬리 고정, 머리/몸통만 움직임
        // (코루틴에서 처리됨)
    }

    void ChargeAtPlayer()
    {
        // 머리를 돌진 방향으로 이동
        transform.position += (Vector3)(chargeDirection * chargeSpeed * Time.deltaTime);
        
        // 목표 지점을 지나쳤거나 충분히 멀어지면 정지
        float distanceToTarget = Vector2.Distance(transform.position, chargeTargetPosition);
        if (distanceToTarget > 20f) // 충분히 멀어지면
        {
            ResetSnake();
        }
    }

    void UpdateSegments()
    {
        // 머리 위치 업데이트
        segmentPositions[0] = transform.position;
        
        // 각 세그먼트가 앞 세그먼트를 따라가도록
        for (int i = 1; i < segments.Count; i++)
        {
            Vector2 targetPosition = segmentPositions[i - 1];
            Vector2 currentPosition = segments[i].position;
            
            // 앞 세그먼트와의 거리 계산
            float distance = Vector2.Distance(currentPosition, targetPosition);
            
            if (currentState == State.Aiming && i == segments.Count - 1)
            {
                // 조준 중에는 꼬리 고정
                continue;
            }
            
            // 일정 거리 이상 떨어지면 따라가기
            if (distance > segmentDistance)
            {
                Vector2 direction = (targetPosition - currentPosition).normalized;
                Vector2 newPosition = currentPosition + direction * (distance - segmentDistance);
                segments[i].position = newPosition;
                segmentPositions[i] = newPosition;
                
                // 세그먼트 회전 (이동 방향을 향하도록)
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                segments[i].rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                segmentPositions[i] = currentPosition;
            }
        }
    }

    void ResetSnake()
    {
        currentState = State.Idle;
        // 필요시 초기 위치로 복귀하는 로직 추가
    }

    // 충돌 감지 (플레이어와 충돌 시)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState == State.Charging && other.CompareTag("Player"))
        {
            // 플레이어 데미지 처리
            Debug.Log("Player Hit!");
            // other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }
}