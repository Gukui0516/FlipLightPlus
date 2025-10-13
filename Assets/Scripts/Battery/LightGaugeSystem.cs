using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.VisualScripting;
using TMPro;

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
    
    [Header("Thunder Fill 효과 설정")]
    [SerializeField] private Image thunderFillImage;
    [Tooltip("번개 효과가 반복되는 주기 (초)")]
    [SerializeField] private float thunderInterval = 0.5f;
    [Tooltip("0%에서 100%까지 차오르는 시간 (초)")]
    [SerializeField] private float thunderFillDuration = 0.2f;
    
    [Header("애니메이션 설정")]
    [SerializeField] private Animator gaugeAnimator;
    [Tooltip("100% 도달 시 재생할 트리거 이름")]
    [SerializeField] private string fullChargeTrigger = "FullCharge";
    
    [Header("텍스트 UI (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI gaugeText;
    [Tooltip("텍스트 포맷 (예: {0}%, {0:F1}%)")]
    [SerializeField] private string textFormat = "{0:F0}%";
    [Tooltip("100%일 때만 텍스트 표시")]
    [SerializeField] private bool showTextOnlyAt100 = false;
    
    [Header("텍스트 아웃라인 설정")]
    [Tooltip("아웃라인 활성화")]
    [SerializeField] private bool enableOutline = true;
    [Tooltip("아웃라인 두께 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float outlineWidth = 0.2f;
    [Tooltip("아웃라인 색상")]
    [SerializeField] private Color outlineColor = Color.black;
    
    [Header("조건 만족 설정")]
    [Tooltip("이 퍼센트 이상 도달하면 조건 만족")]
    [SerializeField] private float conditionThreshold = 100f;
    
    // 이벤트
    public UnityEvent onConditionMet = new UnityEvent();
    
    private bool isInLight = false;
    private bool isConditionMet = false;
    private bool isWaitingAtThreshold = false;
    private float currentThresholdWaitTime = 0f;
    private GaugeThreshold currentThreshold = null;
    
    // Thunder Fill 관련 변수
    private float thunderTimer = 0f;
    private float thunderFillTimer = 0f;
    private bool isThunderFilling = false;
    
    // 애니메이션 재생 관련 변수
    private bool hasPlayedFullChargeAnimation = false;
    
    // 외부에서 참조 가능한 프로퍼티
    public float CurrentGauge => currentGauge;
    public float MaxGauge => maxGauge;
    public float GaugePercentage => (currentGauge / maxGauge) * 100f;
    public bool IsInLight => isInLight;
    public bool IsConditionMet => isConditionMet;
    
    private void Start()
    {
        RegisterToExitDoor();
        
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
        
        thresholds.Sort((a, b) => a.thresholdPercent.CompareTo(b.thresholdPercent));
        
        // 텍스트 아웃라인 초기화
        InitializeTextOutline();
        
        UpdateGaugeUI();
        UpdateGaugeText();
        InitializeThunderFill();
    }
    
    private void InitializeTextOutline()
    {
        if (gaugeText == null || !enableOutline) return;
        
        // Material을 복사하여 이 텍스트에만 적용 (다른 텍스트에 영향 없음)
        gaugeText.fontMaterial = new Material(gaugeText.fontMaterial);
        
        // 아웃라인 설정
        gaugeText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        gaugeText.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
        
        Debug.Log($"{gameObject.name}: 텍스트 아웃라인 적용 (두께: {outlineWidth}, 색상: {outlineColor})");
    }
    
    private void InitializeThunderFill()
    {
        if (thunderFillImage != null)
        {
            thunderFillImage.fillAmount = 0f;
        }
    }
    
    void Update()
    {
        float previousGauge = currentGauge;
        
        if (isInLight)
        {
            currentGauge += chargePerSecond * Time.deltaTime;
            
            if (isWaitingAtThreshold)
            {
                isWaitingAtThreshold = false;
                currentThreshold = null;
            }
            
            UpdateThunderFillEffect();
        }
        else
        {
            HandleGaugeDrain(previousGauge);
            ResetThunderFillEffect();
        }
        
        currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
        
        // 100% 도달 시 애니메이션 재생 (한 번만)
        CheckFullChargeAnimation();
        
        CheckCondition();
        UpdateGaugeUI();
        UpdateGaugeText();
    }
    
    private void CheckFullChargeAnimation()
    {
        // 게이지가 100%에 도달했고, 아직 애니메이션을 재생하지 않았다면
        if (currentGauge >= maxGauge && !hasPlayedFullChargeAnimation)
        {
            if (gaugeAnimator != null)
            {
                gaugeAnimator.SetTrigger(fullChargeTrigger);
                Debug.Log($"{gameObject.name}: FullCharge 애니메이션 재생!");
            }
            hasPlayedFullChargeAnimation = true;
        }
    }
    
    private void UpdateThunderFillEffect()
    {
        if (thunderFillImage == null) return;
        
        // 게이지가 100%에 도달하면 Thunder Fill 효과 중단
        if (currentGauge >= maxGauge)
        {
            thunderFillImage.fillAmount = 0f;
            isThunderFilling = false;
            thunderTimer = 0f;
            thunderFillTimer = 0f;
            return;
        }
        
        thunderTimer += Time.deltaTime;
        
        // 주기마다 새로운 번개 효과 시작
        if (thunderTimer >= thunderInterval)
        {
            thunderTimer = 0f;
            isThunderFilling = true;
            thunderFillTimer = 0f;
        }
        
        // 번개 효과 진행 중
        if (isThunderFilling)
        {
            thunderFillTimer += Time.deltaTime;
            float fillProgress = thunderFillTimer / thunderFillDuration;
            
            if (fillProgress >= 1f)
            {
                // 효과 완료 - 다시 0으로
                thunderFillImage.fillAmount = 0f;
                isThunderFilling = false;
            }
            else
            {
                // 0에서 1까지 채우기
                thunderFillImage.fillAmount = fillProgress;
            }
        }
    }
    
    private void ResetThunderFillEffect()
    {
        if (thunderFillImage != null)
        {
            thunderFillImage.fillAmount = 0f;
        }
        
        thunderTimer = 0f;
        thunderFillTimer = 0f;
        isThunderFilling = false;
    }
    
    private void HandleGaugeDrain(float previousGauge)
    {
        if (isWaitingAtThreshold && currentThreshold != null)
        {
            currentThresholdWaitTime += Time.deltaTime;
            
            if (currentThresholdWaitTime >= currentThreshold.waitTime)
            {
                if (currentThreshold.neverDecrease && Mathf.Approximately(currentThreshold.thresholdPercent, 100f))
                {
                    currentGauge = maxGauge;
                    return;
                }
                else
                {
                    isWaitingAtThreshold = false;
                    currentThreshold = null;
                }
            }
            else
            {
                return;
            }
        }
        
        currentGauge -= drainPerSecond * Time.deltaTime;
        CheckThresholdReached(previousGauge, currentGauge);
    }
    
    private void CheckThresholdReached(float previousGauge, float newGauge)
    {
        if (previousGauge <= newGauge) return;
        
        float previousPercent = (previousGauge / maxGauge) * 100f;
        float newPercent = (newGauge / maxGauge) * 100f;
        
        for (int i = thresholds.Count - 1; i >= 0; i--)
        {
            GaugeThreshold threshold = thresholds[i];
            
            if (previousPercent >= threshold.thresholdPercent && newPercent < threshold.thresholdPercent)
            {
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
        
        isConditionMet = currentPercent >= conditionThreshold;
        
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
    
    private void UpdateGaugeText()
    {
        if (gaugeText == null) return;
        
        float percentage = GaugePercentage;
        
        // 0%일 때는 텍스트 표시 안 함
        if (percentage <= 0f)
        {
            gaugeText.text = "";
            return;
        }
        
        // 100%일 때만 표시 옵션이 켜져 있다면
        if (showTextOnlyAt100)
        {
            if (percentage >= 100f)
            {
                gaugeText.text = string.Format(textFormat, percentage);
            }
            else
            {
                gaugeText.text = "";
            }
        }
        else
        {
            // 일반 모드: 0% 초과일 때 퍼센트 표시
            gaugeText.text = string.Format(textFormat, percentage);
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
        UpdateGaugeText();
    }
    
    public void ResetGauge()
    {
        currentGauge = 0f;
        isConditionMet = false;
        isWaitingAtThreshold = false;
        currentThreshold = null;
        hasPlayedFullChargeAnimation = false;
        ResetThunderFillEffect();
        UpdateGaugeUI();
        UpdateGaugeText();
    }
}