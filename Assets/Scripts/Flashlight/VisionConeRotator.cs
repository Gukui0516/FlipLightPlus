using UnityEngine;

public class VisionConeRotator : MonoBehaviour
{
    public enum RotationMode
    {
        MouseDirection,      // 마우스 방향
        MovementDirection,   // 플레이어 이동 방향
        CustomVector         // 커스텀 벡터
    }

    [Header("Rotation Settings")]
    [SerializeField] private RotationMode rotationMode = RotationMode.MouseDirection;
    [SerializeField] private bool useSpeedLimit = true;
    [SerializeField] private float maxRotationSpeed = 360f; // 초당 최대 회전 각도
    
    [Header("Damping Settings")]
    [SerializeField] private bool useDamping = true;
    [SerializeField] private float dampingFactor = 5f; // 높을수록 빠르게 회전
    
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera mainCamera;
    
    private Vector2 lastMovementDirection;
    private Vector2 targetDirection;
    private float currentAngle;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        currentAngle = transform.eulerAngles.z;
    }

    void Update()
    {
        Vector2 direction = GetTargetDirection();
        
        if (direction != Vector2.zero)
        {
            RotateToDirection(direction);
        }
    }

    Vector2 GetTargetDirection()
    {
        switch (rotationMode)
        {
            case RotationMode.MouseDirection:
                return GetMouseDirection();
                
            case RotationMode.MovementDirection:
                return GetMovementDirection();
                
            case RotationMode.CustomVector:
                return targetDirection;
                
            default:
                return Vector2.zero;
        }
    }

    Vector2 GetMouseDirection()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        
        Vector2 direction = (mousePos - transform.position).normalized;
        return direction;
    }

    Vector2 GetMovementDirection()
    {
        if (playerTransform == null) return lastMovementDirection;
        
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector2 movementInput = new Vector2(horizontal, vertical).normalized;
        
        if (movementInput != Vector2.zero)
        {
            lastMovementDirection = movementInput;
        }
        
        return lastMovementDirection;
    }

    void RotateToDirection(Vector2 direction)
    {
        float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        
        if (useDamping)
        {
            // 부드러운 회전 (Damping)
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * dampingFactor);
        }
        else
        {
            currentAngle = targetAngle;
        }
        
        if (useSpeedLimit)
        {
            // 회전 속도 제한
            float angleDifference = Mathf.DeltaAngle(transform.eulerAngles.z, currentAngle);
            float maxDelta = maxRotationSpeed * Time.deltaTime;
            
            if (Mathf.Abs(angleDifference) > maxDelta)
            {
                currentAngle = transform.eulerAngles.z + Mathf.Sign(angleDifference) * maxDelta;
            }
        }
        
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    // Public 메서드들
    public void SetRotationMode(RotationMode mode)
    {
        rotationMode = mode;
    }

    public void SetCustomDirection(Vector2 direction)
    {
        targetDirection = direction.normalized;
        rotationMode = RotationMode.CustomVector;
    }

    public void RotateToPosition(Vector2 worldPosition)
    {
        Vector2 direction = (worldPosition - (Vector2)transform.position).normalized;
        SetCustomDirection(direction);
    }

    public void SetSpeedLimit(bool enabled, float speed = 360f)
    {
        useSpeedLimit = enabled;
        maxRotationSpeed = speed;
    }

    public void SetDamping(bool enabled, float factor = 5f)
    {
        useDamping = enabled;
        dampingFactor = factor;
    }

    public void SnapToDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        currentAngle = angle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // 디버그용
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector2 currentDir = transform.up;
        Gizmos.DrawRay(transform.position, currentDir * 2f);
    }
}