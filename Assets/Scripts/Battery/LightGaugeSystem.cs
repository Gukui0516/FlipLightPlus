using UnityEngine;
using UnityEngine.UI;

public class LightGaugeSystem : MonoBehaviour
{
    [Header("게이지 설정")]
    [SerializeField] private float maxGauge = 100f;
    [SerializeField] private float currentGauge = 0f;
    
    [Header("충전/방전 속도 (초당)")]
    [SerializeField] private float chargePerSecond = 25f;  // 초당 충전량
    [SerializeField] private float drainPerSecond = 10f;   // 초당 감소량
    
    [Header("충돌 감지 레이어")]
    [SerializeField] private LayerMask targetLayerMask;  // 인스펙터에서 레이어 선택 가능
    
    [Header("UI 참조")]
    [SerializeField] private Image gaugeFillImage;  // 인스펙터에서 직접 할당 가능
    [SerializeField] private bool autoFindFillImage = true;  // 자동으로 자식에서 찾기
    
    private bool isInLight = false;
    
    // 외부에서 참조 가능한 프로퍼티
    public float CurrentGauge => currentGauge;
    public float MaxGauge => maxGauge;
    public float GaugePercentage => (currentGauge / maxGauge) * 100f;
    public bool IsInLight => isInLight;
    
    void Start()
    {
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
        
        // Fill 이미지 초기 설정
        UpdateGaugeUI();
    }
    
    void Update()
    {
        // 게이지 증감
        if (isInLight)
        {
            // 빛을 받으면 게이지 충전
            currentGauge += chargePerSecond * Time.deltaTime;
        }
        else
        {
            // 빛을 받지 않으면 게이지 감소
            currentGauge -= drainPerSecond * Time.deltaTime;
        }
        
        // 0 ~ maxGauge 범위로 제한
        currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
        
        // UI 업데이트
        UpdateGaugeUI();
    }
    
    private void UpdateGaugeUI()
    {
        if (gaugeFillImage != null)
        {
            // fillAmount는 0~1 범위이므로 퍼센트를 100으로 나눔
            gaugeFillImage.fillAmount = currentGauge / maxGauge;
        }
    }
    
    // LayerMask에 특정 레이어가 포함되어 있는지 확인
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
    
    // 충돌 시작
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, targetLayerMask))
        {
            isInLight = true;
            //Debug.Log($"타겟 레이어 오브젝트와 충돌 시작! 게이지 충전 중...");
        }
    }
    
    // 충돌 유지 (혹시 Enter를 놓쳤을 경우 대비)
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, targetLayerMask))
        {
            isInLight = true;
        }
    }
    
    // 충돌 종료
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, targetLayerMask))
        {
            isInLight = false;
            //Debug.Log($"타겟 레이어 오브젝트와 충돌 종료! 게이지 감소 중...");
        }
    }
    
    // 외부에서 게이지 직접 설정 (필요시)
    public void SetGauge(float value)
    {
        currentGauge = Mathf.Clamp(value, 0f, maxGauge);
        UpdateGaugeUI();
    }
    
    // 게이지 초기화
    public void ResetGauge()
    {
        currentGauge = 0f;
        UpdateGaugeUI();
    }
}