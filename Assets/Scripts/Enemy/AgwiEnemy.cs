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
    [SerializeField] private float wakeDelayMax = 1f;
    [SerializeField] private float rushDuration = 3f;
    [SerializeField] private float wakeSpeed = 0.4f;
    [SerializeField] private float sleepSpeed = 0.2f;

    // ✅ 사망 시 숨길 오브젝트들 (인스펙터에서 설정)
    [Header("Death Visual Settings")]
    [SerializeField] private GameObject eyebrowObject;  // EyeBrow 오브젝트
    [SerializeField] private GameObject eyesObject;     // Eyes 오브젝트
    [SerializeField] private GameObject itemObject;     // Item 오브젝트 (있다면)

    private float wakeDelay = 0f;
    private float rushTimer = 0f;
    private float timeInLight = 0f;

    protected override bool UseNavMeshMovement => false;

    protected override void Awake()
    {
        eyebrowMaxSize = eyebrows.localScale.x;
        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        if (visibilityModule != null)
        {
            visibilityModule.Initialize(EnemyType.Agwi);
        }

        currentState = state.Sleep;
        wakeDelay = 0f;
        rushTimer = 0f;
        timeInLight = 0f;
        isInLight = false;

        if (eyebrows != null)
        {
            eyebrows.localScale = new Vector2(eyebrowMaxSize, eyebrows.localScale.y);
        }
        if (wakeNotice != null)
        {
            wakeNotice.SetActive(true);
        }

        // ✅ 오브젝트 풀링 시 다시 보이게
        ShowAgwiParts();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
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
            rb.freezeRotation = true;

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
        if (currentState == state.Rush)
        {
            return true;
        }
        return false;
    }

    protected override bool ShouldRotate()
    {
        return currentState != state.Rush;
    }

    protected override bool IsStoppedByInversion()
    {
        return false;
    }

    protected override float GetCurrentSpeed()
    {
        return speed;
    }

    /// <summary>
    /// ✅ 사망 시 오버라이드 - 아귀 특유의 파츠들을 먼저 숨김
    /// </summary>
    public override void Die()
    {
        if (isDead) return;

        // ✅ 사망 애니메이션 전에 아귀 파츠 숨기기
        HideAgwiParts();

        // BaseEnemy의 Die() 호출 (애니메이션 재생)
        base.Die();
    }

    /// <summary>
    /// ✅ 아귀 특유의 파츠들 숨기기
    /// </summary>
    private void HideAgwiParts()
    {
        if (eyebrowObject != null)
        {
            eyebrowObject.SetActive(false);
        }

        if (eyesObject != null)
        {
            eyesObject.SetActive(false);
        }

        if (itemObject != null)
        {
            itemObject.SetActive(false);
        }

        // wakeNotice도 숨김
        if (wakeNotice != null)
        {
            wakeNotice.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ 아귀 특유의 파츠들 다시 보이기 (오브젝트 풀링 시)
    /// </summary>
    private void ShowAgwiParts()
    {
        if (eyebrowObject != null)
        {
            eyebrowObject.SetActive(true);
        }

        if (eyesObject != null)
        {
            eyesObject.SetActive(true);
        }

        if (itemObject != null)
        {
            itemObject.SetActive(true);
        }
    }
}