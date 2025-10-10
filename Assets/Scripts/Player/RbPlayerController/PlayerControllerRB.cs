using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D XY 커브 가속/감속 컨트롤러 (이동 전용)
/// - MovementSettings SO를 통해 설정 관리
/// - AnimationCurve 기반 가속/감속
/// - X/Y 최대속도가 다른 타원 한계 처리
/// - InputAction 에셋 직접 사용
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControllerRB : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private MovementSettings movementSettings;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    
    private Rigidbody2D _rb;
    private Vector2 _input;

    // 가속/감속 상태
    private float _uAccel; // 0..1
    private bool _decelerating;
    private float _uDecel; // 0..1
    private float _decelStartSpeed;

    // 턴 감속 상태
    private bool _turning;
    private float _uTurn; // 0..1
    private float _turnStartSpeed;
    private float _turnLossLerp;

    private Vector2 _lastDirNorm = Vector2.right;
    
    // InputAction 참조
    private InputAction _moveAction;
    private InputActionMap _playerActionMap;
    
    private void Awake()
    {
        InitializeInputManager();
        InitializeRb();
    }

    private void OnEnable()
    {
        SetVelocityAndSync(_rb.linearVelocity);
        
        // ✅ ActionMap 전체 활성화
        if (_playerActionMap != null)
        {
            _playerActionMap.Enable();
        }
        
        // InputAction 이벤트 구독
        if (_moveAction != null)
        {
            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
        }
    }

    private void OnDisable()
    {
        // InputAction 이벤트 구독 해제
        if (_moveAction != null)
        {
            _moveAction.performed -= OnMove;
            _moveAction.canceled -= OnMove;
        }
        
        // ✅ ActionMap 전체 비활성화
        if (_playerActionMap != null)
        {
            _playerActionMap.Disable();
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    #region InputAction Handling
    
    /// <summary>
    /// 이동 입력 처리
    /// </summary>
    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        
        // ✅ 수정: moveInput을 먼저 체크
        if (moveInput.sqrMagnitude > 0.01f)
        {
            _lastDirNorm = moveInput.normalized;
        }
        
        _input = moveInput;
    }

    #endregion

    #region Initialization and Cleanup Methods

    /// <summary>
    /// 입력 시스템 초기화
    /// </summary>
    private void InitializeInputManager()
    {
        if (movementSettings == null)
        {
            Debug.LogWarning("MovementSettings가 할당되지 않았습니다!");
        }

        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset이 할당되지 않았습니다!");
            return;
        }

        // Action Map 찾기 및 저장
        _playerActionMap = inputActions.FindActionMap("Player");
        if (_playerActionMap == null)
        {
            Debug.LogError("'Player' ActionMap을 찾을 수 없습니다!");
            return;
        }

        // InputAction 찾기
        _moveAction = _playerActionMap.FindAction("Move");
        
        if (_moveAction == null)
        {
            Debug.LogError("'Move' InputAction을 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// Rigidbody2D 초기화
    /// </summary>
    private void InitializeRb()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
    
    #endregion

    #region Movement Methods

    /// <summary>
    /// 이동 처리 메인 로직
    /// </summary>
    private void ApplyMovement()
    {
        if (movementSettings == null)
        {
            Debug.LogWarning("MovementSettings가 할당되지 않았습니다!");
            return;
        }

        float dt = Time.fixedDeltaTime;
        bool hasInput = _input.magnitude > movementSettings.deadzone;

        // 월드 → 정규화 속도 공간 변환
        Vector2 vWorld = _rb.linearVelocity;
        Vector2 vNorm = ToNormalizedSpace(vWorld);
        float currentSpeed = vNorm.magnitude;
        Vector2 currentDir = currentSpeed > 1e-6f ? vNorm / Mathf.Max(currentSpeed, 1e-6f) : _lastDirNorm;

        if (hasInput)
        {
            HandleInputMovement(dt, currentSpeed, currentDir);
        }
        else
        {
            HandleDeceleration(dt, currentSpeed, currentDir);
        }
    }

    /// <summary>
    /// 입력이 있을 때의 이동 처리
    /// </summary>
    private void HandleInputMovement(float dt, float currentSpeed, Vector2 currentDir)
    {
        // 입력 방향을 정규화 공간으로 변환
        Vector2 inputDirNorm = new Vector2(
            _input.x / Mathf.Max(movementSettings.maxSpeedX, 1e-6f),
            _input.y / Mathf.Max(movementSettings.maxSpeedY, 1e-6f)
        );
        
        if (inputDirNorm.sqrMagnitude > 1e-8f)
            inputDirNorm.Normalize();
        else
            inputDirNorm = currentDir;

        // 가속 커브 진행
        _uAccel = Mathf.Clamp01(_uAccel + (movementSettings.accelerationTime <= 0f ? 1f : dt / movementSettings.accelerationTime));
        float inputMagnitude = Mathf.Clamp01(_input.magnitude);
        float goalSpeed = Mathf.Clamp01(movementSettings.accelerationCurve.Evaluate(_uAccel)) * inputMagnitude;

        // 회전 각도 계산
        float turnAngle = Vector2.Angle(currentDir, inputDirNorm);
        float newSpeed = CalculateSpeedWithTurning(turnAngle, currentSpeed, goalSpeed, dt);

        // 새 속도 적용
        Vector2 newVelocityNorm = inputDirNorm * newSpeed;
        _rb.linearVelocity = ToWorldSpace(newVelocityNorm);

        // 입력 중에는 일반 감속 상태 종료
        _decelerating = false;
        _uDecel = 0f;
        _lastDirNorm = inputDirNorm;
    }

    /// <summary>
    /// 회전 각도에 따른 속도 계산
    /// </summary>
    private float CalculateSpeedWithTurning(float angle, float currentSpeed, float goalSpeed, float dt)
    {
        if (angle <= movementSettings.noLossTurnAngle)
        {
            // ✅ 수정: 손실 없는 회전이지만 목표 속도는 준수
            _turning = false;
            _uTurn = 0f;
            
            // 가속 중이면 goalSpeed로, 감속 필요하면 부드럽게 감속
            if (currentSpeed < goalSpeed)
            {
                return goalSpeed;
            }
            else
            {
                // 급격한 감속 방지: 부드러운 감속 적용
                float decelRate = movementSettings.decelerationTime > 0 ? dt / movementSettings.decelerationTime : 1f;
                return Mathf.Lerp(currentSpeed, goalSpeed, decelRate);
            }
        }
        else if (angle >= movementSettings.decelStartTurnAngle)
        {
            // 턴 감속 처리
            return HandleTurnDeceleration(angle, currentSpeed, goalSpeed, dt);
        }
        else
        {
            // ✅ 수정: 중간 구간도 목표 속도 준수
            _turning = false;
            _uTurn = 0f;
            
            if (currentSpeed < goalSpeed)
            {
                return goalSpeed;
            }
            else
            {
                float decelRate = movementSettings.decelerationTime > 0 ? dt / movementSettings.decelerationTime : 1f;
                return Mathf.Lerp(currentSpeed, goalSpeed, decelRate);
            }
        }
    }

    /// <summary>
    /// 턴 감속 처리
    /// </summary>
    private float HandleTurnDeceleration(float angle, float currentSpeed, float goalSpeed, float dt)
    {
        float lossLerp = Mathf.InverseLerp(movementSettings.decelStartTurnAngle, movementSettings.hardFlipAngle, angle);
        float retainRatio = 1f - lossLerp;

        // 턴 상태 초기화/갱신
        if (!_turning || Mathf.Abs(lossLerp - _turnLossLerp) > 0.05f)
        {
            _turning = true;
            _uTurn = 0f;
            _turnStartSpeed = currentSpeed;
            _turnLossLerp = lossLerp;
        }

        // ✅ 수정: 목표 속도도 고려하여 최종 타겟 계산
        float turnTargetSpeed = _turnStartSpeed * retainRatio;
        float targetSpeed = Mathf.Min(goalSpeed, turnTargetSpeed);

        // 큰 각도일수록 빠르게 감속
        float timeScale = Mathf.Lerp(1f, movementSettings.minTurnDecelTimeScale, lossLerp);
        float turnDecelTime = Mathf.Max(1e-4f, movementSettings.decelerationTime * timeScale);

        _uTurn = Mathf.Clamp01(_uTurn + dt / turnDecelTime);
        float curveValue = movementSettings.accelerationCurve.Evaluate(_uTurn);
        float newSpeed = Mathf.Lerp(_turnStartSpeed, targetSpeed, curveValue);

        // 하드 플립: 가속 진행도 초기화
        if (angle >= movementSettings.hardFlipAngle && movementSettings.resetOnHardFlip)
        {
            _uAccel = 0f;
        }

        return newSpeed;
    }

    /// <summary>
    /// 입력이 없을 때의 감속 처리
    /// </summary>
    private void HandleDeceleration(float dt, float currentSpeed, Vector2 currentDir)
    {
        if (!_decelerating)
        {
            _decelerating = true;
            _uDecel = 0f;
            _decelStartSpeed = currentSpeed;
            _lastDirNorm = currentDir;
            _turning = false;
            _uTurn = 0f;
        }

        if (_decelStartSpeed <= 1e-5f || movementSettings.decelerationTime <= 0f)
        {
            StopMovement();
            return;
        }

        _uDecel = Mathf.Clamp01(_uDecel + dt / movementSettings.decelerationTime);
        float curveValue = 1f - Mathf.Clamp01(movementSettings.accelerationCurve.Evaluate(_uDecel));
        float newSpeed = _decelStartSpeed * curveValue;

        if (newSpeed <= 1e-4f)
        {
            StopMovement();
        }
        else
        {
            _rb.linearVelocity = ToWorldSpace(_lastDirNorm * newSpeed);
        }
    }

    /// <summary>
    /// 이동 완전 정지
    /// </summary>
    private void StopMovement()
    {
        _rb.linearVelocity = Vector2.zero;
        _decelerating = false;
        _uAccel = 0f;
    }

    /// <summary>
    /// 월드 좌표계 속도 → 정규화 속도 공간 변환
    /// </summary>
    private Vector2 ToNormalizedSpace(Vector2 worldVelocity)
    {
        return new Vector2(
            worldVelocity.x / Mathf.Max(movementSettings.maxSpeedX, 1e-6f),
            worldVelocity.y / Mathf.Max(movementSettings.maxSpeedY, 1e-6f)
        );
    }

    /// <summary>
    /// 정규화 속도 공간 → 월드 좌표계 속도 변환
    /// </summary>
    private Vector2 ToWorldSpace(Vector2 normalizedVelocity)
    {
        return new Vector2(
            normalizedVelocity.x * Mathf.Max(movementSettings.maxSpeedX, 1e-6f),
            normalizedVelocity.y * Mathf.Max(movementSettings.maxSpeedY, 1e-6f)
        );
    }

    /// <summary>
    /// 외부에서 속도 주입 후 내부 진행도 동기화
    /// </summary>
    public void SetVelocityAndSync(Vector2 worldVelocity)
    {
        _rb.linearVelocity = worldVelocity;
        float normalizedSpeed = ToNormalizedSpace(worldVelocity).magnitude;
        _uAccel = ApproximateInverseCurve(movementSettings.accelerationCurve, Mathf.Clamp01(normalizedSpeed));
        
        _decelerating = false;
        _uDecel = 0f;
        _turning = false;
        _uTurn = 0f;

        Vector2 normalizedDir = ToNormalizedSpace(worldVelocity);
        if (normalizedDir.sqrMagnitude > 1e-8f)
        {
            _lastDirNorm = normalizedDir.normalized;
        }
    }

    /// <summary>
    /// 애니메이션 커브의 근사 역함수 계산
    /// </summary>
    private static float ApproximateInverseCurve(AnimationCurve curve, float targetValue, int samples = 64)
    {
        targetValue = Mathf.Clamp01(targetValue);
        float bestTime = 0f;
        float bestError = float.MaxValue;

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float curveValue = Mathf.Clamp01(curve.Evaluate(t));
            float error = Mathf.Abs(curveValue - targetValue);
            
            if (error < bestError)
            {
                bestError = error;
                bestTime = t;
            }
        }

        return bestTime;
    }

    #endregion
}