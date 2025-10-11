using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 탈출구 오브젝트에 붙여서 모든 발전기(LightGaugeSystem)를 관리하는 클래스
/// ExitDoorController와 함께 사용됩니다
/// </summary>
public class GeneratorManager : MonoBehaviour
{
    private List<LightGaugeSystem> allGauges = new List<LightGaugeSystem>();
    private bool isInitialized = false;
    private ExitDoorController exitDoorController;
    
    [Header("초기화 설정")]
    [SerializeField] private float initializeDelay = 0.6f;
    
    void Start()
    {
        // ExitDoorController 참조 가져오기
        exitDoorController = GetComponent<ExitDoorController>();
        if (exitDoorController == null)
        {
            Debug.LogWarning("GeneratorManager: 같은 오브젝트에 ExitDoorController가 없습니다. 일부 기능이 제한될 수 있습니다.");
        }
        
        Invoke(nameof(Initialize), initializeDelay);
    }
    
    private void Initialize()
    {
        // 씬의 모든 LightGaugeSystem 찾기
        LightGaugeSystem[] foundGauges = FindObjectsByType<LightGaugeSystem>(FindObjectsSortMode.None);
        allGauges = new List<LightGaugeSystem>(foundGauges);
        
        isInitialized = true;
        
        Debug.Log($"GeneratorManager: {allGauges.Count}개의 발전기 발견");
    }
    
    /// <summary>
    /// 모든 발전기 리스트 반환
    /// </summary>
    public List<LightGaugeSystem> GetAllGauges()
    {
        return new List<LightGaugeSystem>(allGauges);
    }
    
    /// <summary>
    /// 완료되지 않은 발전기들만 반환
    /// </summary>
    public List<LightGaugeSystem> GetIncompleteGauges()
    {
        return allGauges.Where(g => !g.IsConditionMet).ToList();
    }
    
    /// <summary>
    /// 특정 발전기를 제외하고, 가장 가까운 미완료 발전기 찾기
    /// </summary>
    public LightGaugeSystem FindNearestIncompleteGauge(Transform origin, LightGaugeSystem excludeGauge = null)
    {
        var availableGauges = allGauges
            .Where(g => g != excludeGauge && !g.IsConditionMet)
            .ToList();
        
        if (availableGauges.Count == 0)
            return null;
        
        LightGaugeSystem nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var gauge in availableGauges)
        {
            float distance = Vector3.Distance(origin.position, gauge.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = gauge;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// ExitDoorController의 조건을 기준으로 모든 조건이 만족되었는지 확인
    /// </summary>
    public bool AreAllConditionsMet()
    {
        if (exitDoorController == null)
            return false;
        
        return exitDoorController.AreAllConditionsMet;
    }
    
    public bool IsInitialized => isInitialized;
    public int TotalGaugeCount => allGauges.Count;
    public int CompletedGaugeCount => allGauges.Count(g => g.IsConditionMet);
}