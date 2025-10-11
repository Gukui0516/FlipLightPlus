using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.VisualScripting;

[System.Serializable]
public class GaugeThreshold
{
    [Tooltip("방지턱 퍼센트 (0~100)")]
    [Range(0, 100)]
    public float thresholdPercent = 50f;
    
    [Tooltip("방지턱에서 대기하는 시간 (초)")]
    public float waitTime = 2f;
    
    [Tooltip("100%일 때만: 더 이상 떨어지지 않음")]
    public bool neverDecrease = false;
}

public class LightGaugeSystem : MonoBehaviour
{
    [Header("게이지 설정")]
    [SerializeField] private float maxGauge = 100f;
    [SerializeField] private float currentGauge = 0f;
    
    [Header("충전/방전 속도 (초당)")]
    [SerializeField] private float chargePerSecond = 25f;
    [SerializeField] private float drainPerSecond = 10f;
    
    [Header("방지턱 설정")]
    [SerializeField] private List<GaugeThreshold> thresholds = new List<GaugeThreshold>();
    
    [Header("충돌 감지 레이어")]
    [SerializeField] private LayerMask targetLayerMask;
    
    [Header("UI 참조")]
    [SerializeField] private Image gaugeFillImage;
    [SerializeField] private bool autoFindFillImage = true;
    
    [Header("조건 만족 설정")]
    [Tooltip("이 퍼센트 이상 도달하면 조건 만족")]
    [SerializeField] private float conditionThreshold = 100f;
    
    // 이벤트
    public UnityEvent onConditionMet = new UnityEvent();
    
    private bool isInLight = false;
    private bool isConditionMet = false; // 조건 만족 여부
    private bool isWaitingAtThreshold = false; // 방지턱에서 대기 중
    private float currentThresholdWaitTime = 0f;
    private GaugeThreshold currentThreshold = null;
    
    // 외부에서 참조 가능한 프로퍼티
    public float CurrentGauge => currentGauge;
    public float MaxGauge => maxGauge;
    public float GaugePercentage => (currentGauge / maxGauge) * 100f;
    public bool IsInLight => isInLight;
    public bool IsConditionMet => isConditionMet;
    
    private void Start()
    {
        // ExitDoorController에 자신을 등록
        RegisterToExitDoor();
        // 자동으로 Fill 이미지 찾기
        if (gaugeFillImage == null && autoFindFillImage) 
        {
            gaugeFillImage = GetComponentInChildren<Image>();
            
            if (gaugeFillImage != null)
            {
                Debug.Log($"자동으로 Fill 이미지를 찾았습니다: {gaugeFillImage.name}");
            }
            else
            {
                Debug.LogWarning("Fill 이미지를 찾을 수 없습니다. 인스펙터에서 수동으로 할당해주세요.");
            }
        }
        
        // 방지턱 퍼센트 순으로 정렬
        thresholds.Sort((a, b) => a.thresholdPercent.CompareTo(b.thresholdPercent));
        
        // Fill 이미지 초기 설정
        UpdateGaugeUI();
    }
    
    void Update()
    {
        float previousGauge = currentGauge;
        
        // 게이지 증감
        if (isInLight)
        {
            // 빛을 받으면 게이지 충전
            currentGauge += chargePerSecond * Time.deltaTime;
            
            // 충전 중이면 대기 상태 취소
            if (isWaitingAtThreshold)
            {
                isWaitingAtThreshold = false;
                currentThreshold = null;
            }
        }
        else
        {
            // 빛을 받지 않으면 게이지 감소 처리
            HandleGaugeDrain(previousGauge);
        }
        
        // 0 ~ maxGauge 범위로 제한
        currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
        
        // 조건 만족 체크
        CheckCondition();
        
        // UI 업데이트
        UpdateGaugeUI();
    }
    
    private void HandleGaugeDrain(float previousGauge)
    {
        // 방지턱에서 대기 중인 경우
        if (isWaitingAtThreshold && currentThreshold != null)
        {
            currentThresholdWaitTime += Time.deltaTime;
            
            // 대기 시간이 끝나면
            if (currentThresholdWaitTime >= currentThreshold.waitTime)
            {
                // neverDecrease가 true이고 100%인 경우 더 이상 감소하지 않음
                if (currentThreshold.neverDecrease && Mathf.Approximately(currentThreshold.thresholdPercent, 100f))
                {
                    currentGauge = maxGauge;
                    return;
                }
                else
                {
                    // 일반 방지턱은 대기 후 계속 감소
                    isWaitingAtThreshold = false;
                    currentThreshold = null;
                }
            }
            else
            {
                // 대기 중에는 게이지 유지
                return;
            }
        }
        
        // 게이지 감소
        currentGauge -= drainPerSecond * Time.deltaTime;
        
        // 방지턱 체크 (게이지가 감소하면서 방지턱에 도달했는지)
        CheckThresholdReached(previousGauge, currentGauge);
    }
    
    private void CheckThresholdReached(float previousGauge, float newGauge)
    {
        if (previousGauge <= newGauge) return; // 증가 중이면 체크 안함
        
        float previousPercent = (previousGauge / maxGauge) * 100f;
        float newPercent = (newGauge / maxGauge) * 100f;
        
        // 방지턱을 통과했는지 확인 (높은 퍼센트부터 체크)
        for (int i = thresholds.Count - 1; i >= 0; i--)
        {
            GaugeThreshold threshold = thresholds[i];
            
            // 이전엔 방지턱 이상이었고, 지금은 방지턱 아래로 떨어진 경우
            if (previousPercent >= threshold.thresholdPercent && newPercent < threshold.thresholdPercent)
            {
                // 방지턱에 정확히 고정
                currentGauge = (threshold.thresholdPercent / 100f) * maxGauge;
                isWaitingAtThreshold = true;
                currentThreshold = threshold;
                currentThresholdWaitTime = 0f;
                break;
            }
        }
    }
    
    private void CheckCondition()
    {
        bool wasMet = isConditionMet;
        float currentPercent = GaugePercentage;
        
        // 조건: 설정한 퍼센트 이상 도달
        isConditionMet = currentPercent >= conditionThreshold;
        
        // 처음으로 조건를 만족했을 때 이벤트 발생
        if (!wasMet && isConditionMet)
        {
            onConditionMet.Invoke();
            Debug.Log($"{gameObject.name}: 조건 만족! ({currentPercent:F1}%)");
        }
    }
    
    private void RegisterToExitDoor()
    {
        Debug.Log($"{gameObject.name}이(가) ExitDoor에 등록을 시도합니다.");
        ExitDoorController exitDoor = FindFirstObjectByType<ExitDoorController>();
        
        if (exitDoor != null)
        {
            exitDoor.RegisterGauge(this);
            Debug.Log($"{gameObject.name}이(가) ExitDoor에 등록되었습니다.");
        }
        else
        {
            Debug.LogError("ExitDoor 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }
    
    private void UpdateGaugeUI()
    {
        if (gaugeFillImage != null)
        {
            gaugeFillImage.fillAmount = currentGauge / maxGauge;
        }
    }
    
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, targetLayerMask))
        {
            isInLight = true;
        }
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, targetLayerMask))
        {
            isInLight = true;
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, targetLayerMask))
        {
            isInLight = false;
        }
    }
    
    public void SetGauge(float value)
    {
        currentGauge = Mathf.Clamp(value, 0f, maxGauge);
        UpdateGaugeUI();
    }
    
    public void ResetGauge()
    {
        currentGauge = 0f;
        isConditionMet = false;
        isWaitingAtThreshold = false;
        currentThreshold = null;
        UpdateGaugeUI();
    }
}