using System.Collections;
using UnityEngine;
using TMPro;

public class StageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private TextMeshProUGUI stageInfoText;

    [Header("Controller Reference")]
    [SerializeField] private ExitDoorController exitDoorController;
    [SerializeField] private bool autoFindController = true;

    [Header("Settings")]
    [SerializeField] private int currentStage = 1;
    [SerializeField] private float displayDuration = 2f;    // 1번째 문장 유지 시간
    [SerializeField] private float fadeOutDuration = 1f;    // 페이드 아웃 시간
    [SerializeField] private float secondDisplayDuration = 2f; // 2번째 문장 유지 시간
    [SerializeField] private float fadeInDuration = 0.4f;   // 2번째 문장 페이드 인 시간

    [Header("UI Move (Optional)")]
    [SerializeField] private RectTransform uiToMove;              // 이동시킬 UI (예: 배터리 아이콘)
    [SerializeField] private Vector2 nearMessageAnchoredPos;      // 두 번째 문장 옆 임시 위치
    [SerializeField] private Vector2 targetAnchoredPos;           // 원래 자리(오른쪽 위)
    [SerializeField] private float moveDuration = 0.6f;           // 이동 시간

    // 1) 스테이지별 첫 문장 (기존 배열)
    private readonly string[] stageInfoMessages = new string[]
    {
        "빛을 보면 멈추는 괴물이 등장합니다.",           // 1스테이지 - 첫 문장
        "빛을 보면 따라오는 괴물이 등장합니다.",        // 2스테이지
        "벽을 통과하며 돌진하는 큰 괴물이 등장합니다."      // 3스테이지
    };

    void Start()
    {
        // ExitDoorController 자동으로 찾기
        if (autoFindController && exitDoorController == null)
        {
            exitDoorController = FindFirstObjectByType<ExitDoorController>();
            
            if (exitDoorController == null)
            {
                Debug.LogWarning("StageUI: ExitDoorController를 찾을 수 없습니다. 기본 메시지를 사용합니다.");
            }
        }

        // 초기 알파값을 1로 설정
        SetTextAlpha(stageNumberText, 1f);
        SetTextAlpha(stageInfoText, 1f);

        if (GameManager.Instance != null)
            currentStage = GameManager.Instance.CurrentStage;

        // 순차 표시 시작
        StartCoroutine(DisplayStageInfo());
    }

    private IEnumerator DisplayStageInfo()
    {
        // 스테이지 번호 텍스트
        if (stageNumberText) stageNumberText.text = $"Stage : {currentStage}";

        // 인덱스 계산
        int idx = Mathf.Clamp(currentStage - 1, 0, stageInfoMessages.Length - 1);
        string part1 = stageInfoMessages[idx];
        
        // 두 번째 문장: ExitDoorController에서 필요 개수를 가져와서 동적으로 생성
        string part2 = GetSecondaryMessage();

        // 1) 첫 문장 표시 → 유지 → 첫 문장만 페이드아웃
        if (stageInfoText)
        {
            stageInfoText.text = part1;
            SetTextAlpha(stageInfoText, 1f);
        }
        yield return new WaitForSeconds(displayDuration);
        yield return Fade(stageInfoText, 1f, 0f, fadeOutDuration);

        // 2) 두 번째 문장이 있으면: 문장 옆으로 UI 배치 → 페이드인 → 유지 → 두 텍스트 동시 페이드아웃 → UI 원위치 이동
        if (!string.IsNullOrEmpty(part2))
        {
            if (stageInfoText) stageInfoText.text = part2;

            // 두 번째 문장 옆으로 UI 즉시 배치
            if (uiToMove) uiToMove.anchoredPosition = nearMessageAnchoredPos;

            SetTextAlpha(stageInfoText, 0f);
            yield return Fade(stageInfoText, 0f, 1f, fadeInDuration);

            yield return new WaitForSeconds(secondDisplayDuration);

            // 두 번째 문장 사라질 때 Stage도 같이 사라지게
            yield return FadePair(stageInfoText, stageNumberText, 1f, 0f, fadeOutDuration);

            // UI를 원래 자리(오른쪽 위)로 부드럽게 이동
            if (uiToMove) yield return MoveUI(uiToMove, targetAnchoredPos, moveDuration);
        }
        else
        {
            // 두 번째 문장이 없으면 Stage도 따로 페이드아웃
            yield return Fade(stageNumberText, 1f, 0f, fadeOutDuration);
        }

        // 완전히 투명 보장
        SetTextAlpha(stageInfoText, 0f);
        SetTextAlpha(stageNumberText, 0f);

        // 선택: UI 비활성화
        // gameObject.SetActive(false);
    }

    /// <summary>
    /// ExitDoorController의 설정을 기반으로 두 번째 메시지를 동적으로 생성
    /// </summary>
    private string GetSecondaryMessage()
    {
        int requiredCount = 1; // 기본값

        if (exitDoorController != null)
        {
            // ExitDoorController가 자동 모드인지 수동 모드인지 확인하고 필요 개수 가져오기
            if (exitDoorController.IsRegistrationFinalized)
            {
                // 이미 등록이 완료되어 RequiredGaugeCount가 설정된 경우
                requiredCount = exitDoorController.RequiredGaugeCount;
            }
            else
            {
                // 등록이 아직 완료되지 않은 경우 미리 계산
                requiredCount = GetPredictedRequiredCount();
            }
        }
        else
        {
            // ExitDoorController가 없으면 스테이지 번호를 기본값으로 사용
            requiredCount = currentStage;
        }

        // 한국어 숫자 변환 (선택사항)
        string countText = ConvertNumberToKorean(requiredCount);
        
        return $"{countText}의 발전기를 찾아 빛으로 충전시키세요.";
    }

    /// <summary>
    /// 등록 완료 전에 필요 개수를 미리 예측
    /// </summary>
    private int GetPredictedRequiredCount()
    {
        if (exitDoorController == null)
            return currentStage;

        // ExitDoorController의 SerializedField를 통해 설정을 확인할 수 없으므로
        // StageRequiredCounts 리스트를 직접 사용
        var stageRequiredCounts = exitDoorController.StageRequiredCounts;
        
        if (stageRequiredCounts == null || stageRequiredCounts.Count == 0)
            return currentStage;

        int index = currentStage - 1;
        
        if (index < 0)
            return 1;
        
        if (index < stageRequiredCounts.Count)
        {
            return stageRequiredCounts[index];
        }
        else
        {
            // 리스트 범위를 벗어나면 마지막 값의 1.5배 반올림
            int lastValue = stageRequiredCounts[stageRequiredCounts.Count - 1];
            return Mathf.RoundToInt(lastValue * 1.5f);
        }
    }

    /// <summary>
    /// 숫자를 한국어로 변환 (1~10까지만 지원, 그 이상은 숫자 그대로)
    /// </summary>
    private string ConvertNumberToKorean(int number)
    {
        switch (number)
        {
            case 1: return "한 개";
            case 2: return "두 개";
            case 3: return "세 개";
            case 4: return "네 개";
            case 5: return "다섯 개";
            case 6: return "여섯 개";
            case 7: return "일곱 개";
            case 8: return "여덟 개";
            case 9: return "아홉 개";
            case 10: return "열 개";
            default: return $"{number}개";
        }
    }

    private IEnumerator MoveUI(RectTransform rect, Vector2 targetPos, float duration)
    {
        if (!rect || duration <= 0f) { if (rect) rect.anchoredPosition = targetPos; yield break; }

        Vector2 startPos = rect.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t / duration);
            yield return null;
        }
        rect.anchoredPosition = targetPos;
    }

    private IEnumerator Fade(TextMeshProUGUI text, float from, float to, float duration)
    {
        if (!text || duration <= 0f) { SetTextAlpha(text, to); yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetTextAlpha(text, a);
            yield return null;
        }
        SetTextAlpha(text, to);
    }

    private IEnumerator FadePair(TextMeshProUGUI a, TextMeshProUGUI b, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetTextAlpha(a, to);
            SetTextAlpha(b, to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
            if (a) SetTextAlpha(a, alpha);
            if (b) SetTextAlpha(b, alpha);
            yield return null;
        }
        SetTextAlpha(a, to);
        SetTextAlpha(b, to);
    }

    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (!text) return;
        var c = text.color; c.a = alpha; text.color = c;
    }

    /// <summary>
    /// 외부에서 스테이지 번호를 설정할 수 있는 메서드
    /// </summary>
    public void SetStage(int stage) => currentStage = stage;

    /// <summary>
    /// 외부에서 ExitDoorController를 설정할 수 있는 메서드
    /// </summary>
    public void SetExitDoorController(ExitDoorController controller) => exitDoorController = controller;
}