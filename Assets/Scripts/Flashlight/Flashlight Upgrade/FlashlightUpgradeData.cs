using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 손전등 업그레이드 데이터 ScriptableObject
/// Assets 폴더에서 우클릭 > Create > Flashlight/Upgrade Data로 생성
/// </summary>
[CreateAssetMenu(fileName = "FlashlightUpgradeData", menuName = "Flashlight/Upgrade Data")]
public class FlashlightUpgradeData : ScriptableObject
{
    [Header("기본 설정")]
    [Tooltip("기본 부채꼴 각도")]
    [Range(0f, 360f)]
    public float defaultViewAngle = 90f;
    
    [Tooltip("기본 부채꼏 반지름")]
    [Range(1f, 50f)]
    public float defaultViewRadius = 10f;
    
    [Header("레벨 데이터")]
    [Tooltip("손전등 레벨별 설정 (리스트에 추가하면 자동으로 레벨 할당)")]
    public List<FlashlightLevel> levels = new List<FlashlightLevel>();
    
    /// <summary>
    /// 에디터에서 리스트 변경 시 레벨 번호 자동 할당
    /// </summary>
    private void OnValidate()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null)
            {
                levels[i].level = i + 1;
            }
        }
    }
    
    /// <summary>
    /// 특정 레벨의 데이터 가져오기
    /// </summary>
    public FlashlightLevel GetLevel(int level)
    {
        if (level < 1 || level > levels.Count)
            return null;
        
        return levels[level - 1];
    }
    
    /// <summary>
    /// 최대 레벨 확인
    /// </summary>
    public int GetMaxLevel()
    {
        return levels.Count;
    }
    
    /// <summary>
    /// 기본값으로 FlashlightLevel 생성
    /// </summary>
    public FlashlightLevel GetDefaultLevel()
    {
        return new FlashlightLevel(1, defaultViewAngle, defaultViewRadius);
    }
}