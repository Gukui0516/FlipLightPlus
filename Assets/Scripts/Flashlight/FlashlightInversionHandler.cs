using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 손전등 반전 시 빛 영역 내부의 적을 처리하는 핸들러
/// ImprovedVisionCone과 분리하여 유지보수성 향상
/// Physics2D.OverlapCollider로 PolygonCollider2D와 실제 충돌 중인 적만 감지 (벽 고려)
/// </summary>
public class FlashlightInversionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ImprovedVisionCone visionCone;
    [SerializeField] private WorldStateManager worldStateManager;
    
    [Header("Settings")]
    [Tooltip("반전 시 적을 죽이는 기능 활성화")]
    [SerializeField] private bool enableKillOnInversion = true;
    
    [Tooltip("적 감지에 사용할 레이어 마스크")]
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Detection Method")]
    [Tooltip("즉시 감지: PolygonCollider2D와 실제 충돌 중인 적만 감지 (벽 고려, 권장)\n캐시 사용: VisionCone의 기존 목록 사용")]
    [SerializeField] private bool useImmediateDetection = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool wasInverted = false;

    void Awake()
    {
        // 자동으로 컴포넌트 찾기
        if (visionCone == null)
            visionCone = GetComponent<ImprovedVisionCone>();
        
        if (worldStateManager == null)
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        
        // 유효성 검사
        if (visionCone == null)
        {
            Debug.LogError("[FlashlightInversionHandler] ImprovedVisionCone을 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        if (worldStateManager == null)
        {
            Debug.LogError("[FlashlightInversionHandler] WorldStateManager를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        // enemyLayer가 설정되지 않았으면 경고
        if (useImmediateDetection && enemyLayer == 0)
        {
            Debug.LogWarning("[FlashlightInversionHandler] enemyLayer가 설정되지 않았습니다! 즉시 감지가 작동하지 않을 수 있습니다.");
        }
    }

    void OnEnable()
    {
        // WorldStateManager 이벤트 구독
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.AddListener(OnInversionChanged);
            wasInverted = worldStateManager.IsInverted;
            
            if (showDebugLogs)
                Debug.Log($"[FlashlightInversionHandler] 이벤트 구독 완료. 현재 반전 상태: {wasInverted}");
        }
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.RemoveListener(OnInversionChanged);
        }
    }

    /// <summary>
    /// 반전 상태 변경 시 호출되는 콜백
    /// </summary>
    private void OnInversionChanged(bool isInverted)
    {
        // 반전이 false -> true로 변경되는 시점만 감지 (구버전과 동일)
        bool changed = wasInverted != isInverted;
        
        if (changed && isInverted)
        {
            if (showDebugLogs)
                Debug.Log("[FlashlightInversionHandler] ⚡ 반전 시작 감지! 빛 영역 내 적 제거 시작...");
            
            int killedCount = KillAllEnemiesInLight();
            
            if (showDebugLogs)
                Debug.Log($"[FlashlightInversionHandler] ✅ Inversion burst: {killedCount}마리 제거 완료");
        }
        
        wasInverted = isInverted;
    }

    /// <summary>
    /// 빛 영역 내부의 모든 적을 죽임
    /// </summary>
    private int KillAllEnemiesInLight()
    {
        if (!enableKillOnInversion)
        {
            if (showDebugLogs)
                Debug.Log("[FlashlightInversionHandler] 반전 시 적 제거 기능이 비활성화되어 있습니다.");
            return 0;
        }

        if (visionCone == null)
        {
            Debug.LogWarning("[FlashlightInversionHandler] VisionCone이 null입니다.");
            return 0;
        }

        int killedCount = 0;

        if (useImmediateDetection)
        {
            // 방법 1: 즉시 감지 (구버전 Flashlight2D 방식) - 권장!
            killedCount = KillEnemiesImmediate();
        }
        else
        {
            // 방법 2: VisionCone의 캐시된 목록 사용 (타이밍 문제 가능성)
            killedCount = KillEnemiesFromCache();
        }

        return killedCount;
    }

    /// <summary>
    /// 즉시 감지 방식: PolygonCollider2D와 실제로 충돌 중인 적만 감지 (벽 고려)
    /// </summary>
    private int KillEnemiesImmediate()
    {
        int killedCount = 0;
        
        // VisionCone의 PolygonCollider2D 가져오기
        PolygonCollider2D visionCollider = visionCone.GetVisionCollider();
        
        if (visionCollider == null)
        {
            Debug.LogWarning("[FlashlightInversionHandler] VisionCollider가 null입니다. enableVisionCollider를 true로 설정하세요.");
            return 0;
        }

        // ContactFilter2D 설정 - 적 레이어만 필터링
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true; // Trigger 콜라이더도 감지

        // PolygonCollider2D와 실제로 겹치는(overlap) 콜라이더만 찾기
        // 벽에 가려진 영역은 콜라이더가 없으므로 자동으로 제외됨!
        List<Collider2D> results = new List<Collider2D>();
        int hitCount = Physics2D.OverlapCollider(visionCollider, filter, results);

        if (showDebugLogs)
            Debug.Log($"[FlashlightInversionHandler] 🔍 즉시 감지: {hitCount}개 콜라이더가 VisionCollider와 충돌 중");

        foreach (Collider2D hit in results)
        {
            if (hit == null) continue;

            BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[FlashlightInversionHandler] 💀 {enemy.name} 제거");
                
                enemy.Die();
                killedCount++;
            }
        }

        return killedCount;
    }

    /// <summary>
    /// 캐시 사용 방식: VisionCone의 기존 타겟 목록 사용 (타이밍 문제 가능)
    /// </summary>
    private int KillEnemiesFromCache()
    {
        int killedCount = 0;
        
        // VisionCone의 캐시된 타겟 목록 사용
        visionCone.ProcessAllVisibleTargets((targetTransform) =>
        {
            if (targetTransform == null) return;
            
            BaseEnemy enemy = targetTransform.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[FlashlightInversionHandler] 💀 {enemy.name} 제거 (캐시)");
                
                enemy.Die();
                killedCount++;
            }
        });

        if (killedCount == 0 && showDebugLogs)
        {
            Debug.LogWarning("[FlashlightInversionHandler] ⚠️ 캐시된 타겟이 없습니다. useImmediateDetection = true로 설정하세요.");
        }

        return killedCount;
    }

    /// <summary>
    /// 외부에서 수동으로 호출할 수 있는 메서드
    /// </summary>
    public void ManualKillAllEnemiesInLight()
    {
        if (showDebugLogs)
            Debug.Log("[FlashlightInversionHandler] 🔧 수동 제거 호출");
        
        int killed = KillAllEnemiesInLight();
        
        if (showDebugLogs)
            Debug.Log($"[FlashlightInversionHandler] 수동 제거 완료: {killed}마리");
    }
    
    /// <summary>
    /// 현재 빛 영역에 있는 적의 수 반환 (디버그용)
    /// </summary>
    public int GetEnemyCountInLight()
    {
        if (visionCone == null) return 0;
        
        if (useImmediateDetection)
        {
            PolygonCollider2D visionCollider = visionCone.GetVisionCollider();
            if (visionCollider == null) return 0;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useLayerMask = true;
            filter.useTriggers = true;

            List<Collider2D> results = new List<Collider2D>();
            Physics2D.OverlapCollider(visionCollider, filter, results);

            int count = 0;
            foreach (Collider2D hit in results)
            {
                if (hit != null && hit.GetComponent<BaseEnemy>() != null)
                    count++;
            }
            return count;
        }
        else
        {
            int count = 0;
            visionCone.ProcessAllVisibleTargets((targetTransform) =>
            {
                if (targetTransform != null && targetTransform.GetComponent<BaseEnemy>() != null)
                {
                    count++;
                }
            });
            return count;
        }
    }
}