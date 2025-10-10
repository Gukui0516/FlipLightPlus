using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 각 발전기에 붙여서 100% 달성 시 가장 가까운 미완료 발전기를 가리키는 화살표 표시
/// 월드 스페이스 캔버스의 UI Image를 사용합니다
/// </summary>
public class GeneratorNavigateUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image arrowImage;
    [SerializeField] private Image arrowImageChild; // 자식 이미지 (선택)
    [SerializeField] private bool autoFindImage = true;
    
    [Header("회전 설정")]
    [SerializeField] private float rotationOffset = 0f; // 화살표가 위를 가리킬 때 기준 (0도 = 위쪽)
    
    [Header("거리 표시 (선택)")]
    [SerializeField] private bool showDistance = false;
    [SerializeField] private Text distanceText;
    
    private GeneratorManager generatorManager;
    private LightGaugeSystem myGauge;
    private LightGaugeSystem targetGauge;
    private List<LightGaugeSystem> allGauges;
    private RectTransform arrowRectTransform;
    
    private bool isMyGaugeComplete = false;
    
    void Start()
    {
        // 자동으로 Image 찾기
        if (arrowImage == null && autoFindImage)
        {
            arrowImage = GetComponent<Image>();
            if (arrowImage == null)
            {
                Debug.LogError($"GeneratorNavigate ({gameObject.name}): Image 컴포넌트를 찾을 수 없습니다!");
            }
            else
            {
                arrowRectTransform = arrowImage.GetComponent<RectTransform>();
            }
        }
        else if (arrowImage != null)
        {
            arrowRectTransform = arrowImage.GetComponent<RectTransform>();
        }
        
        // 자식 이미지 자동 찾기
        if (arrowImageChild == null && transform.childCount > 0)
        {
            arrowImageChild = transform.GetChild(0).GetComponent<Image>();
        }
        
        // 자신의 LightGaugeSystem 가져오기 (부모 오브젝트에서)
        myGauge = GetComponentInParent<LightGaugeSystem>();
        if (myGauge == null)
        {
            Debug.LogError($"GeneratorNavigate ({gameObject.name}): LightGaugeSystem을 찾을 수 없습니다! 발전기 오브젝트의 자식으로 배치하세요.");
            enabled = false;
            return;
        }
        
        // GeneratorManager 찾기 (Exit 태그에서)
        GameObject exitObject = GameObject.FindGameObjectWithTag("Exit");
        if (exitObject != null)
        {
            generatorManager = exitObject.GetComponent<GeneratorManager>();
            if (generatorManager == null)
            {
                Debug.LogError($"GeneratorNavigate ({gameObject.name}): Exit 오브젝트에 GeneratorManager가 없습니다!");
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError($"GeneratorNavigate ({gameObject.name}): Exit 태그를 가진 오브젝트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        // 자신의 게이지 완료 이벤트 구독
        myGauge.onConditionMet.AddListener(OnMyGaugeComplete);
        
        // 초기에는 화살표 비활성화
        SetArrowActive(false);
        
        // 약간의 딜레이 후 초기화 (GeneratorManager의 초기화가 완료될 때까지 대기)
        Invoke(nameof(InitializeAfterManager), 0.7f);
    }
    
    private void InitializeAfterManager()
    {
        if (!generatorManager.IsInitialized)
        {
            Debug.LogWarning($"GeneratorNavigate ({gameObject.name}): GeneratorManager가 아직 초기화되지 않았습니다. 재시도...");
            Invoke(nameof(InitializeAfterManager), 0.2f);
            return;
        }
        
        // 모든 게이지 가져오기
        allGauges = generatorManager.GetAllGauges();
        
        if (allGauges == null || allGauges.Count == 0)
        {
            Debug.LogWarning($"GeneratorNavigate ({gameObject.name}): 등록된 게이지가 없습니다.");
            return;
        }
        
        // 다른 모든 게이지의 완료 이벤트 구독 (타겟 재계산용)
        foreach (var gauge in allGauges)
        {
            if (gauge != myGauge)
            {
                gauge.onConditionMet.AddListener(OnOtherGaugeComplete);
            }
        }
        
        Debug.Log($"GeneratorNavigate ({gameObject.name}): 초기화 완료 - 총 {allGauges.Count}개 발전기 감지");
    }
    
    void Update()
    {
        // 자신의 게이지가 100%이고 타겟이 있을 때만 화살표 표시
        if (isMyGaugeComplete && targetGauge != null)
        {
            UpdateArrowDirection();
            
            if (showDistance && distanceText != null)
            {
                float distance = Vector3.Distance(myGauge.transform.position, targetGauge.transform.position);
                distanceText.text = $"{distance:F1}m";
            }
        }
    }
    
    private void UpdateArrowDirection()
    {
        if (targetGauge == null || arrowRectTransform == null) return;
        
        // 발전기 위치에서 타겟 발전기까지의 방향 계산
        Vector3 direction = targetGauge.transform.position - myGauge.transform.position;
        
        // 2D 각도 계산 (Z축 회전)
        // Atan2는 오른쪽(+X)을 0도로 하므로, 위쪽(+Y) 기준으로 맞추기 위해 -90도
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        
        // 회전 적용 (오프셋 포함)
        arrowRectTransform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }
    
    private void UpdateTargetGenerator()
    {
        if (generatorManager == null)
        {
            targetGauge = null;
            SetArrowActive(false);
            return;
        }
        
        // GeneratorManager를 통해 가장 가까운 미완료 발전기 찾기
        targetGauge = generatorManager.FindNearestIncompleteGauge(myGauge.transform, myGauge);
        
        if (targetGauge == null)
        {
            // 모든 발전기가 완료됨
            SetArrowActive(false);
            Debug.Log($"GeneratorNavigate ({gameObject.name}): 모든 발전기 완료!");
        }
        else
        {
            float distance = Vector3.Distance(myGauge.transform.position, targetGauge.transform.position);
            Debug.Log($"GeneratorNavigate ({gameObject.name}): 새 타겟 설정 - {targetGauge.gameObject.name} (거리: {distance:F1})");
            SetArrowActive(true);
        }
    }
    
    private void OnMyGaugeComplete()
    {
        isMyGaugeComplete = true;
        Debug.Log($"GeneratorNavigate ({gameObject.name}): 내 게이지 100% 달성! 화살표 활성화");
        UpdateTargetGenerator();
    }
    
    private void OnOtherGaugeComplete()
    {
        // 다른 발전기가 완료되면 타겟 재계산
        if (isMyGaugeComplete)
        {
            Debug.Log($"GeneratorNavigate ({gameObject.name}): 다른 발전기 완료 감지, 타겟 재계산");
            UpdateTargetGenerator();
        }
    }
    
    private void SetArrowActive(bool active)
    {
        if (arrowImage != null)
        {
            arrowImage.enabled = active;
        }
        
        if (arrowImageChild != null)
        {
            arrowImageChild.enabled = active;
        }
        
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(active);
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (myGauge != null)
        {
            myGauge.onConditionMet.RemoveListener(OnMyGaugeComplete);
        }
        
        if (allGauges != null)
        {
            foreach (var gauge in allGauges)
            {
                if (gauge != null && gauge != myGauge)
                {
                    gauge.onConditionMet.RemoveListener(OnOtherGaugeComplete);
                }
            }
        }
    }
    
    // 디버그용: Scene 뷰에서 타겟까지의 선 표시
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isMyGaugeComplete || targetGauge == null || myGauge == null)
            return;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(myGauge.transform.position, targetGauge.transform.position);
        Gizmos.DrawWireSphere(targetGauge.transform.position, 0.5f);
    }
}