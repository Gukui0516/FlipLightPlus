using System.Collections;
using UnityEngine;
using TMPro;

public class StageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private TextMeshProUGUI stageInfoText;

    [Header("Settings")]
    [SerializeField] private int currentStage = 1;
    [SerializeField] private float displayDuration = 2f;    // 1번째 문장 유지 시간
    [SerializeField] private float fadeOutDuration = 1f;    // 페이드 아웃 시간
    [SerializeField] private float secondDisplayDuration = 2f; // 2번째 문장 유지 시간
    [SerializeField] private float fadeInDuration = 0.4f;   // 2번째 문장 페이드 인 시간

    // 1) 스테이지별 첫 문장 (기존 배열)
    private readonly string[] stageInfoMessages = new string[]
    {
        "빛을 보면 멈추는 괴물이 등장합니다.",           // 1스테이지 - 첫 문장
        "빛을 보면 따라오는 괴물이 등장합니다.",        // 2스테이지
        "벽을 통과하며 돌진하는 괴물이 등장합니다."      // 3스테이지
    };

    // 2) 스테이지별 두 번째 문장 (새 배열: 필요 없으면 빈 문자열)
    private readonly string[] stageSecondaryMessages = new string[]
    {
        "한 개의 발전소를 찾아 빛을 모으세요.", // 1스테이지 - 두 번째 문장
        "두 개의 발전소를 찾아 빛을 모으세요.", // 2스테이지
        "세 개의 발전소를 찾아 빛을 모으세요." // 3스테이지
    };

    void Start()
    {
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
        string part2 = (idx < stageSecondaryMessages.Length) ? stageSecondaryMessages[idx] : "";

        // 1) 첫 문장 표시 → 유지 → 첫 문장만 페이드아웃
        if (stageInfoText)
        {
            stageInfoText.text = part1;
            SetTextAlpha(stageInfoText, 1f);
        }
        yield return new WaitForSeconds(displayDuration);
        yield return Fade(stageInfoText, 1f, 0f, fadeOutDuration);

        // 2) 두 번째 문장이 있으면: 페이드인 → 유지 → 두 텍스트 동시 페이드아웃
        if (!string.IsNullOrEmpty(part2))
        {
            if (stageInfoText) stageInfoText.text = part2;

            SetTextAlpha(stageInfoText, 0f);
            yield return Fade(stageInfoText, 0f, 1f, fadeInDuration);

            yield return new WaitForSeconds(secondDisplayDuration);

            // 두 번째 문장 사라질 때 Stage도 같이 사라지게
            yield return FadePair(stageInfoText, stageNumberText, 1f, 0f, fadeOutDuration);
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

    // 외부에서 스테이지 번호를 설정할 수 있는 메서드
    public void SetStage(int stage) => currentStage = stage;
}
