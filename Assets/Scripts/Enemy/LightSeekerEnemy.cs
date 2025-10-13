using System.Collections;
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

    [Header("Collision Settings")]
    [SerializeField] private float collisionStunDuration = 0.5f;

    private float currentSpeed;
    private float timeInLight = 0f;
    private Camera mainCamera;
    private bool isStunnedByCollision = false;
    private Coroutine stunCoroutine;

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
        isStunnedByCollision = false;
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
        if (isStunnedByCollision) return false;

        if (isInverted)
        {
            return IsInCameraView() && !isInLight;
        }
        return isInLight;
    }

    protected override bool ShouldRotate()
    {
        if (isInverted)
        {
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
    /// ✅ 외부에서 호출 가능한 스턴 메서드 (PlayerContact에서 호출)
    /// </summary>
    public void TriggerCollisionStun()
    {
        if (isDead) return;
        if (isStunnedByCollision) return; // 이미 스턴 중이면 무시

        // 이미 스턴 중이면 기존 코루틴 정지
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        // 즉시 속도 0으로 초기화
        ResetSpeed();
        movementModule?.StopImmediate();

        // 스턴 코루틴 시작
        stunCoroutine = StartCoroutine(StunFromCollision());
    }

    private IEnumerator StunFromCollision()
    {
        isStunnedByCollision = true;

        yield return new WaitForSeconds(collisionStunDuration);

        isStunnedByCollision = false;
        stunCoroutine = null;
    }

    private bool IsInCameraView()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return false;
        }

        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(transform.position);

        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               viewportPoint.z > 0;
    }

    private void UpdateSpeed()
    {
        if (isStunnedByCollision) return;

        bool shouldIncreaseSpeed;

        if (isInverted)
        {
            shouldIncreaseSpeed = IsInCameraView() && !isInLight;
        }
        else
        {
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
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }
        isStunnedByCollision = false;

        ForceVisible();
        ResetSpeed();
        base.Die();
    }
}