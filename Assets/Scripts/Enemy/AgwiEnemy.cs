using UnityEngine;
using UnityEngine.UIElements;

enum state { Sleep, Wake, Rush }

public class AgwiEnemy : BaseEnemy
{
    private state currentState = state.Sleep;

    [Header("Agwi Settings")]
    [SerializeField] private float detectionRange = 8.0f;
    [SerializeField] private Transform eyebrows;
    [SerializeField] private GameObject wakeNotice;
    [SerializeField] private float eyebrowMaxSize;
    [SerializeField] private float wakeDelayMax = 1f;   // 돌진 전 대기
    [SerializeField] private float rushDuration = 3f;   // 돌진 지속 후 사망
    [SerializeField] private float wakeSpeed = 0.4f;
    [SerializeField] private float sleepSpeed = 0.2f;

    private float wakeDelay = 0f;
    private float rushTimer = 0f;
    private float timeInLight = 0f;

    // ★ 아귀는 NavMesh 이동을 사용하지 않는다. (BaseEnemy.FixedUpdate 분기 제어)
    protected override bool UseNavMeshMovement => false;

    protected override void Awake()
    {
        eyebrowMaxSize = eyebrows.localScale.x;
        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        // Visibility 초기화
        if (visibilityModule != null)
        {
            visibilityModule.Initialize(EnemyType.Agwi);
        }

        // 상태 초기화
        currentState = state.Sleep;
        wakeDelay = 0f;
        rushTimer = 0f;
        timeInLight = 0f;
        isInLight = false;

        // 눈썹/알림 초기화
        if (eyebrows != null)
        {
            eyebrows.localScale = new Vector2(eyebrowMaxSize, eyebrows.localScale.y);
        }
        if (wakeNotice != null)
        {
            wakeNotice.SetActive(true);
        }

        // ★ NavMeshAgent는 “달고만” 있고, 실제 이동엔 관여하지 않도록 완전 정지
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            // 굳이 비활성까지는 필요 없지만, 확실하게 차단하고 싶다면 주석 해제
            // agent.enabled = false;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isDead || player == null) return;

        switch (currentState)
        {
            case state.Sleep:
                HandleSleepState();
                break;
            case state.Wake:
                HandleWakeState();
                break;
            case state.Rush:
                HandleRushState();
                break;
        }
    }

    private void HandleSleepState()
    {
        // 감지거리 안이거나 손전등에 비춰지면 눈 뜨기
        if (Vector2.Distance(transform.position, player.transform.position) <= detectionRange || isInLight)
        {
            eyebrows.localScale = new Vector2(
                eyebrows.localScale.x - wakeSpeed * Time.deltaTime,
                eyebrows.localScale.y
            );

            if (eyebrows.localScale.x <= 0)
            {
                if (wakeNotice) wakeNotice.SetActive(false);
                currentState = state.Wake;
            }
        }
        else
        {
            // 다시 감기
            eyebrows.localScale = new Vector2(
                eyebrows.localScale.x + sleepSpeed * Time.deltaTime,
                eyebrows.localScale.y
            );

            if (eyebrows.localScale.x >= eyebrowMaxSize)
            {
                eyebrows.localScale = new Vector2(eyebrowMaxSize, eyebrows.localScale.y);
            }
        }
    }

    private void HandleWakeState()
    {
        wakeDelay += Time.deltaTime;

        if (wakeDelay >= wakeDelayMax)
        {
            // 돌진 시작: 이 시점의 바라보는 방향을 ‘고정’하고 전진만 한다.
            currentState = state.Rush;
            rushTimer = 0f;

            // 회전 고정
            rb.freezeRotation = true;

            // 혹시 NavMeshAgent가 켜져 있다면 다시 한번 확실히 정지
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }
    }

    private void HandleRushState()
    {
        rushTimer += Time.deltaTime;

        if (rushTimer >= rushDuration && !isDead)
        {
            base.Die();
        }
    }

    protected override bool ShouldMove()
    {
        // 돌진 중에만 이동
        if (currentState == state.Rush)
        {
            return true;
        }
        return false;
    }

    protected override bool ShouldRotate()
    {
        // 돌진 시작 이후에는 더 이상 회전하지 않음 → 방향 고정
        return currentState != state.Rush;
    }

    protected override bool IsStoppedByInversion()
    {
        // 아귀는 반전에도 멈추지 않음
        return false;
    }

    protected override float GetCurrentSpeed()
    {
        return speed;
    }
}
