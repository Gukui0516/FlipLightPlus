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

        // 등록이 완료되지 않았으면 표시하지 않음
        if (!exitDoorController.IsRegistrationFinalized)
            return;

        // 남은 개수 계산
        int remaining = exitDoorController.RequiredGaugeCount - exitDoorController.SatisfiedGaugeCount;

        // 변경 사항이 없으면 업데이트 안 함 (최적화)
        if (remaining == lastRemaining)
            return;

        lastRemaining = remaining;

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
    /// ExitDoorController 수동 설정 (외부에서 호출 가능)
    /// </summary>
    public void SetExitDoorController(ExitDoorController controller)
    {
        exitDoorController = controller;
        lastRemaining = -1; // 강제 업데이트
        UpdateUI();
    }
}