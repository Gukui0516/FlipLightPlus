using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 플래시라이트 업그레이드 UI 표시 및 애니메이션
/// - 업그레이드 정보 텍스트 표시
/// - 위로 이동하면서 페이드아웃 애니메이션
/// - 자동 초기화
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class FlashlightUpgradeUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI upgradeText;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float moveSpeed = 50f; // 위로 이동 속도 (픽셀/초)
    [SerializeField] private float fadeDuration = 2f; // 페이드아웃 시간 (초)
    [SerializeField] private float displayDuration = 2.5f; // 전체 표시 시간 (초)
    
    // 컴포넌트
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    
    // 원본 위치
    private Vector2 originalPosition;
    
    // 애니메이션 상태
    private Coroutine animationCoroutine;
    
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        // 원본 위치 저장
        originalPosition = rectTransform.anchoredPosition;
        
        // 시작 시 비활성화
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 업그레이드 UI 표시
    /// </summary>
    public void ShowUpgrade(int newLevel, float oldAngle, float newAngle, float oldRadius, float newRadius)
    {
        // 이전 애니메이션 중지
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // 초기화
        ResetUI();
        
        // 텍스트 설정
        float angleIncrease = newAngle - oldAngle;
        float radiusIncrease = newRadius - oldRadius;
        
        upgradeText.text = $"업그레이드 레벨 {newLevel}\n" +
                          $"각도: {oldAngle:F0}° → {newAngle:F0}° (+{angleIncrease:F0}°)\n" +
                          $"반지름: {oldRadius:F1}m → {newRadius:F1}m (+{radiusIncrease:F1}m)";
        
        // UI 활성화
        gameObject.SetActive(true);
        
        // 애니메이션 시작
        animationCoroutine = StartCoroutine(AnimateUpgrade());
    }
    
    /// <summary>
    /// 업그레이드 애니메이션 코루틴
    /// </summary>
    private IEnumerator AnimateUpgrade()
    {
        float elapsedTime = 0f;
        Vector2 startPosition = originalPosition;
        
        while (elapsedTime < displayDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 위로 이동
            float moveDistance = moveSpeed * elapsedTime;
            rectTransform.anchoredPosition = startPosition + new Vector2(0, moveDistance);
            
            // 알파값 감소 (fadeDuration 시간 동안)
            if (elapsedTime >= displayDuration - fadeDuration)
            {
                float fadeProgress = (elapsedTime - (displayDuration - fadeDuration)) / fadeDuration;
                canvasGroup.alpha = 1f - fadeProgress;
            }
            
            yield return null;
        }
        
        // UIManager를 통해 비활성화 (UIManager가 있는 경우)
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            // UIManager에 등록된 키가 있다면 해당 키로 숨김 처리
            // 없다면 직접 비활성화
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private void ResetUI()
    {
        rectTransform.anchoredPosition = originalPosition;
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// UI가 비활성화될 때 자동 초기화
    /// </summary>
    void OnDisable()
    {
        // 애니메이션 중지
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        // 초기화
        ResetUI();
    }
    
    /// <summary>
    /// 수동 초기화 메서드
    /// </summary>
    public void ManualReset()
    {
        ResetUI();
        gameObject.SetActive(false);
    }
}