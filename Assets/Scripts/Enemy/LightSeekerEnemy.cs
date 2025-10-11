using UnityEngine;

/// <summary>
/// LightSeeker 타입 적 - 손전등 비춰질 때만 움직임 (평소), 반전시 패턴 변경
/// </summary>
public class LightSeekerEnemy : BaseEnemy
{
    [Header("LightSeeker Speed Settings")]
    [SerializeField] private float lightSeekerBaseSpeed = 2f;
    [SerializeField] private float speedIncreaseRate = 2f;
    [SerializeField] private float speedIncreaseInterval = 2f;
    [SerializeField] private float maxSpeed = 8f;

    private float currentSpeed;
    private float timeInLight = 0f;

    protected override void Awake()
    {
        base.Awake();
        currentSpeed = lightSeekerBaseSpeed;
    }

    protected override void InitializeEnemy()
    {
        ResetColor();

        if (visibilityModule != null)
        {
            visibilityModule.Initialize(EnemyType.LightSeeker);
        }

        // ✅ 초기화 시 속도 리셋 보장
        ResetSpeed();
        isInLight = false;
    }

    protected override void Update()
    {
        base.Update();
        // ⚠️ Update 대신 FixedUpdate로 이동 제안
    }

    // ✅ 속도 계산을 FixedUpdate로 이동하여 이동과 동기화
    protected override void FixedUpdate()
    {
        if (!isDead)
        {
            UpdateSpeed();
        }
        base.FixedUpdate();
    }

    protected override bool ShouldMove()
    {
        if (isInverted)
        {
            return !isInLight;
        }
        return isInLight;
    }

    protected override bool ShouldRotate()
    {
        if (isInverted)
        {
            return !isInLight;
        }
        return true;
    }

    protected override bool IsStoppedByInversion()
    {
        return false;
    }

    protected override float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// ✅ 속도 증가 로직 - FixedUpdate 주기로 변경
    /// </summary>
    private void UpdateSpeed()
    {
        bool shouldIncreaseSpeed = isInverted ? !isInLight : isInLight;

        if (shouldIncreaseSpeed)
        {
            // ✅ FixedUpdate 주기 사용
            timeInLight += Time.fixedDeltaTime;

            int intervals = Mathf.FloorToInt(timeInLight / speedIncreaseInterval);
            float speedMultiplier = Mathf.Pow(speedIncreaseRate, intervals);

            currentSpeed = Mathf.Min(lightSeekerBaseSpeed * speedMultiplier, maxSpeed);
        }
        else
        {
            // ✅ 조건에 맞지 않으면 즉시 속도 초기화
            // 이렇게 하면 손전등 벗어났을 때 바로 리셋됨
            if (currentSpeed != lightSeekerBaseSpeed)
            {
                ResetSpeed();
            }
        }
    }

    protected override void OnEnterLight()
    {
        // 반전 상태에서 손전등 들어가면 속도 초기화
        if (isInverted)
        {
            ResetSpeed();
        }
    }

    protected override void OnExitLight()
    {
        // ✅ 평소/반전 관계없이 손전등 벗어나면 무조건 초기화
        ResetSpeed();
    }

    private void ForceVisible()
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                sr.color = Color.black;
            }
        }
    }

    private void ResetSpeed()
    {
        currentSpeed = lightSeekerBaseSpeed;
        timeInLight = 0f;
    }

    private void ResetColor()
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                Color color = sr.color;
                color.a = 0f;
                sr.color = color;
            }
        }
    }

    public override void Die()
    {
        ForceVisible();
        ResetSpeed();  // ✅ 죽을 때도 속도 리셋
        base.Die();
    }
}