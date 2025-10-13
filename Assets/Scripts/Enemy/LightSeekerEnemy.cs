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
    private Camera mainCamera;

    protected override void Awake()
    {
        base.Awake();
        currentSpeed = lightSeekerBaseSpeed;
        mainCamera = Camera.main;
    }

    protected override void InitializeEnemy()
    {
        ResetColor();

        if (visibilityModule != null)
        {
            visibilityModule.Initialize(EnemyType.LightSeeker);
        }

        ResetSpeed();
        isInLight = false;
    }

    protected override void Update()
    {
        base.Update();
    }

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
            // ✅ 반전 상태: 카메라 범위 안 + 손전등 밖에 있을 때만 움직임
            return IsInCameraView() && !isInLight;
        }
        // 평소 상태: 손전등 안에 있을 때만 움직임
        return isInLight;
    }

    protected override bool ShouldRotate()
    {
        if (isInverted)
        {
            // ✅ 반전 상태: 카메라 범위 안 + 손전등 밖에 있을 때 회전
            return IsInCameraView() && !isInLight;
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
    /// ✅ 카메라 뷰포트 범위 내에 있는지 체크
    /// </summary>
    private bool IsInCameraView()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return false;
        }

        // 오브젝트의 월드 좌표를 뷰포트 좌표로 변환 (0~1 범위)
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(transform.position);

        // 뷰포트 범위 내에 있고, 카메라 앞쪽에 있는지 체크
        // x, y가 0~1 사이, z가 양수(카메라 앞)
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               viewportPoint.z > 0;
    }

    private void UpdateSpeed()
    {
        bool shouldIncreaseSpeed;

        if (isInverted)
        {
            // ✅ 반전 상태: 카메라 범위 안 + 손전등 밖
            shouldIncreaseSpeed = IsInCameraView() && !isInLight;
        }
        else
        {
            // 평소 상태: 손전등 안
            shouldIncreaseSpeed = isInLight;
        }

        if (shouldIncreaseSpeed)
        {
            timeInLight += Time.fixedDeltaTime;

            int intervals = Mathf.FloorToInt(timeInLight / speedIncreaseInterval);
            float speedMultiplier = Mathf.Pow(speedIncreaseRate, intervals);

            currentSpeed = Mathf.Min(lightSeekerBaseSpeed * speedMultiplier, maxSpeed);
        }
        else
        {
            if (currentSpeed != lightSeekerBaseSpeed)
            {
                ResetSpeed();
            }
        }
    }

    protected override void OnEnterLight()
    {
        if (isInverted)
        {
            ResetSpeed();
        }
    }

    protected override void OnExitLight()
    {
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
        ResetSpeed();
        base.Die();
    }
}