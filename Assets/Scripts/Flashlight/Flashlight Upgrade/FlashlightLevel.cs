using UnityEngine;

/// <summary>
/// 손전등 레벨 데이터 구조
/// </summary>
[System.Serializable]
public class FlashlightLevel
{
    [Tooltip("레벨 번호 (자동 할당)")]
    public int level;
    
    [Tooltip("부채꼴 각도")]
    [Range(0f, 360f)]
    public float viewAngle = 90f;
    
    [Tooltip("부채꼴 반지름")]
    [Range(1f, 50f)]
    public float viewRadius = 10f;
    
    public FlashlightLevel(int level, float angle, float radius)
    {
        this.level = level;
        this.viewAngle = angle;
        this.viewRadius = radius;
    }
}