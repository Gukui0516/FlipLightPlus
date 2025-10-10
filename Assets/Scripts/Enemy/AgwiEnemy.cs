using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Agwi 타입 적 - 플레이어 감지 시 깨어나서 돌진 후 죽음
/// </summary>
enum state
{
    Sleep, Wake, Rush
}
public class AgwiEnemy : BaseEnemy
{
    private state currentState = state.Sleep;

    [Header("Agwi Settings")]
    [SerializeField] private float detectionRange = 8.0f;
    [SerializeField] private Transform eyebrows;
    [SerializeField] private GameObject wakeNotice;
    [SerializeField] private float eyebrowMaxSize;
    [SerializeField] private float wakeDelayMax = 1f; //돌진 전 대기 시간
    [SerializeField] private float rushDuration = 3f; //돌진 지속 시간 (이 시간 후 죽음)
    [SerializeField] private float wakeSpeed = 0.4f;
    [SerializeField] private float sleepSpeed = 0.2f;

    private float wakeDelay = 0f;
    private float rushTimer = 0f;
    private float timeInLight = 0f;

    protected override void Awake()
    {
        eyebrowMaxSize = eyebrows.localScale.x;
        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        // Visibility 모듈 초기화
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

        // 눈썹 초기화
        if (eyebrows != null)
        {
            eyebrows.localScale = new Vector2(eyebrowMaxSize, eyebrows.localScale.y);
        }

        // 알림 오브젝트 초기화
        if (wakeNotice != null)
        {
            wakeNotice.SetActive(true);
        }
    }

    protected override void Update()
    {
        base.Update();

        // 죽었거나 플레이어가 없으면 실행 안 함
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
        // 감지거리 안이거나 손전등에 비춰지면
        if (Vector2.Distance(transform.position, player.transform.position) <= detectionRange || isInLight)
        {
            // 눈 뜨기
            eyebrows.localScale = new Vector2(
                eyebrows.localScale.x - wakeSpeed * Time.deltaTime,
                eyebrows.localScale.y
            );

            if (eyebrows.localScale.x <= 0)
            {
                wakeNotice.SetActive(false);
                currentState = state.Wake;
            }
        }
        else
        {
            // 눈 감기
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
            currentState = state.Rush;
            rushTimer = 0f;
        }
    }

    private void HandleRushState()
    {
        rushTimer += Time.deltaTime;

        // 돌진 시간이 끝나면 죽음 (한 번만 실행)
        if (rushTimer >= rushDuration && !isDead)
        {
            base.Die();
        }
    }

    protected override bool ShouldMove()
    {
        if (currentState == state.Rush)
        {
            rb.freezeRotation = true;
            return true;
        }
        return false;
    }

    protected override bool ShouldRotate()
    {
        if (currentState == state.Rush)
        {
            return false;
        }
        return true;
    }

    protected override bool IsStoppedByInversion()
    {
        return false;
    }

    protected override float GetCurrentSpeed()
    {
        return speed;
    }

}