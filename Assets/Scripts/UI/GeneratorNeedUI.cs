using UnityEngine;
using TMPro;

public class GeneratorNeedUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TextMeshProUGUI gaugeCountText;
    [SerializeField] private ExitDoorController exitDoorController;

    [Header("표시 설정")]
    [SerializeField] private string remainingFormat = "X {0}";
    [SerializeField] private string openText = "OPEN";
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color openColor = Color.green;

    [Header("자동 찾기")]
    [SerializeField] private bool autoFindController = true;

    private int lastRemaining = -1;
    private int lastRequired = -1;

    void Start()
    {
        // ExitDoorController 자동으로 찾기
        if (autoFindController && exitDoorController == null)
        {
            exitDoorController = FindFirstObjectByType<ExitDoorController>();

            if (exitDoorController == null)
            {
                Debug.LogError("GeneratorNeedUI: ExitDoorController를 찾을 수 없습니다!");
            }
        }

        // 초기 UI 업데이트
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (gaugeCountText == null || exitDoorController == null)
            return;

        int requiredCount;
        int satisfiedCount;

        // 등록이 완료되었는지 확인
        if (exitDoorController.IsRegistrationFinalized)
        {
            // 등록 완료 후: 정확한 값 사용
            requiredCount = exitDoorController.RequiredGaugeCount;
            satisfiedCount = exitDoorController.SatisfiedGaugeCount;
        }
        else
        {
            // 등록 완료 전: 예측값 사용 (Start 시점에서도 표시하기 위해)
            requiredCount = GetPredictedRequiredCount();
            satisfiedCount = 0; // 등록 전이므로 만족한 개수는 0
        }

        // 남은 개수 계산
        int remaining = requiredCount - satisfiedCount;

        // 변경 사항이 없으면 업데이트 안 함 (최적화)
        if (remaining == lastRemaining && requiredCount == lastRequired)
            return;

        lastRemaining = remaining;
        lastRequired = requiredCount;

        // UI 업데이트
        if (remaining <= 0)
        {
            gaugeCountText.text = openText;
            gaugeCountText.color = openColor;
        }
        else
        {
            gaugeCountText.text = string.Format(remainingFormat, remaining);
            gaugeCountText.color = normalColor;
        }
    }

    /// <summary>
    /// 등록 완료 전에 필요 개수를 미리 예측
    /// ExitDoorController의 stageRequiredCounts 리스트를 기반으로 계산
    /// </summary>
    private int GetPredictedRequiredCount()
    {
        if (exitDoorController == null)
            return 1;

        // GameManager에서 현재 스테이지 가져오기
        int currentStage = 1;
        if (GameManager.Instance != null)
        {
            currentStage = GameManager.Instance.CurrentStage;
        }

        // ExitDoorController의 StageRequiredCounts 리스트 사용
        var stageRequiredCounts = exitDoorController.StageRequiredCounts;
        
        if (stageRequiredCounts == null || stageRequiredCounts.Count == 0)
        {
            // 리스트가 없으면 기본값 1 반환
            return 1;
        }

        int index = currentStage - 1;
        
        if (index < 0)
            return 1;
        
        if (index < stageRequiredCounts.Count)
        {
            // 리스트 범위 내
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
    /// ExitDoorController 수동 설정 (외부에서 호출 가능)
    /// </summary>
    public void SetExitDoorController(ExitDoorController controller)
    {
        exitDoorController = controller;
        lastRemaining = -1; // 강제 업데이트
        lastRequired = -1;
        UpdateUI();
    }

    /// <summary>
    /// 현재 필요한 개수를 반환 (외부에서 확인용)
    /// </summary>
    public int GetCurrentRequiredCount()
    {
        if (exitDoorController == null)
            return 1;

        if (exitDoorController.IsRegistrationFinalized)
            return exitDoorController.RequiredGaugeCount;
        else
            return GetPredictedRequiredCount();
    }
}