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
        // 색상 초기화 (죽을 때 흰색으로 변경되므로)
        ResetColor();

        // Visibility 모듈 초기화
        if (visibilityModule != null)
        {
            visibilityModule.Initialize(EnemyType.LightSeeker);
        }

        // 속도 초기화
        currentSpeed = lightSeekerBaseSpeed;
        timeInLight = 0f;
        isInLight = false;
    }

    protected override void Update()
    {
        base.Update();

        // 속도 증가 로직
        UpdateSpeed();
    }

    protected override bool ShouldMove()
    {
        // 반전 상태: 손전등 밖에서 움직임 (Normal처럼)
        if (isInverted)
        {
            return !isInLight;
        }

        // 평소: 손전등 비춰질 때만 움직임
        return isInLight;
    }

    protected override bool ShouldRotate()
    {
        // 반전 상태: Normal처럼 손전등 밖에서만 회전
        if (isInverted)
        {
            return !isInLight;
        }

        // 평소: 항상 회전
        return true;
    }

    protected override bool IsStoppedByInversion()
    {
        // LightSeeker는 반전 상태에서도 계속 움직임 (패턴만 변경)
        return false;
    }

    protected override float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// 속도 증가 로직 - 조건에 맞을 때만 증가
    /// </summary>
    private void UpdateSpeed()
    {
        // 반전 상태: 손전등 밖에서 움직이므로 손전등 밖에서 속도 증가
        // 평소: 손전등 안에서 움직이므로 손전등 안에서 속도 증가
        bool shouldIncreaseSpeed = isInverted ? !isInLight : isInLight;

        if (shouldIncreaseSpeed)
        {
            timeInLight += Time.deltaTime;

            // 지수적 증가
            int intervals = Mathf.FloorToInt(timeInLight / speedIncreaseInterval);
            float speedMultiplier = Mathf.Pow(speedIncreaseRate, intervals);

            currentSpeed = Mathf.Min(lightSeekerBaseSpeed * speedMultiplier, maxSpeed);
        }
    }



    protected override void OnEnterLight()
    {
        // 반전 상태에서 손전등 들어가면 속도 초기화 (멈추므로)
        if (isInverted)
        {
            ResetSpeed();
        }
    }

    protected override void OnExitLight()
    {
        // 평소: 손전등 나가면 속도 초기화 (손전등 안에서 움직이다가 나감)
        if (!isInverted)
        {
            ResetSpeed();
        }
    }

    /// <summary>
    /// 죽을 때 강제로 보이게 함
    /// </summary>
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
                // 투명하게 초기화 (알파 0)
                Color color = sr.color;
                color.a = 0f;
                sr.color = color;
            }
        }
    }



    public override void Die()
    {
        ForceVisible(); // 죽을 때는 무조건 보이게
        base.Die();
        ResetSpeed();
    }
}