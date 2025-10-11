using UnityEngine;

/// <summary>
/// 플레이어 이동 관련 설정을 관리하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "MovementSettings", menuName = "Settings/Movement Settings")]
public class MovementSettings : ScriptableObject
{
    [Header("Max Speeds (Per Axis)")]
    [Tooltip("X축 최대 속도")]
    public float maxSpeedX = 12f;
    
    [Tooltip("Y축 최대 속도")]
    public float maxSpeedY = 10f;

    [Header("Inverted Max Speeds")]
    [Tooltip("반전 상태일 때 X축 최대 속도")]
    public float invertedMaxSpeedX = 18f;
    
    [Tooltip("반전 상태일 때 Y축 최대 속도")]
    public float invertedMaxSpeedY = 15f;

    [Header("Acceleration & Deceleration")]
    [Tooltip("가속 곡선 (0~1)")]
    public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Tooltip("최대속도까지 도달하는 시간")]
    public float accelerationTime = 0.6f;
    
    [Tooltip("입력 없을 때 정지까지 걸리는 시간")]
    public float decelerationTime = 0.25f;

    [Header("Turning Angles (Degrees)")]
    [Range(0, 180)]
    [Tooltip("이 각도까지는 속도 손실 없음")]
    public float noLossTurnAngle = 91f;
    
    [Range(0, 180)]
    [Tooltip("이 각도부터 커브 감속 시작")]
    public float decelStartTurnAngle = 134f;
    
    [Range(0, 180)]
    [Tooltip("이 각도 이상은 강한 반전 처리")]
    public float hardFlipAngle = 170f;

    [Header("Turning Options")]
    [Tooltip("hardFlip 이상에서 가속 진행도 리셋 여부")]
    public bool resetOnHardFlip = true;
    
    [Tooltip("큰 각도일수록 감속을 더 빠르게 (1=같음, 0.5=두배 빠름)")]
    [Range(0.25f, 1f)]
    public float minTurnDecelTimeScale = 0.6f;

    [Header("Input")]
    [Range(0f, 0.25f)]
    [Tooltip("입력 데드존")]
    public float deadzone = 0.05f;

    /// <summary>
    /// 설정값 유효성 검사
    /// </summary>
    private void OnValidate()
    {
        // 각도 파라미터 안전장치
        noLossTurnAngle = Mathf.Clamp(noLossTurnAngle, 0, 180);
        decelStartTurnAngle = Mathf.Clamp(decelStartTurnAngle, noLossTurnAngle, 180);
        hardFlipAngle = Mathf.Clamp(hardFlipAngle, decelStartTurnAngle, 180);
        
        // 속도/시간 값 안전장치
        maxSpeedX = Mathf.Max(0.1f, maxSpeedX);
        maxSpeedY = Mathf.Max(0.1f, maxSpeedY);
        invertedMaxSpeedX = Mathf.Max(0.1f, invertedMaxSpeedX);
        invertedMaxSpeedY = Mathf.Max(0.1f, invertedMaxSpeedY);
        accelerationTime = Mathf.Max(0.01f, accelerationTime);
        decelerationTime = Mathf.Max(0.01f, decelerationTime);
    }
}