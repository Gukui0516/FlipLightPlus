using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 발전기에 붙여서 다음 발전기 위치를 모스 부호로 표시하는 인디케이터
/// </summary>
public class GeneratorIndicator : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private LightGaugeSystem lightGaugeSystem;
    [Tooltip("수동으로 할당해주세요 (미리 배치된 TMP 사용)")]
    [SerializeField] private TextMeshProUGUI pingText;
    
    [Header("모스 부호 설정")]
    [Tooltip("점(·) 지속 시간")]
    [SerializeField] private float dotDuration = 0.05f;
    [Tooltip("선(—) 지속 시간 (보통 점의 3배)")]
    [SerializeField] private float dashDuration = 0.15f;
    [Tooltip("점/선 사이 간격")]
    [SerializeField] private float symbolGap = 0.05f;
    [Tooltip("글자 사이 간격")]
    [SerializeField] private float letterGap = 0.15f;
    [Tooltip("단어 사이 간격 (사용 안 함)")]
    [SerializeField] private float wordGap = 0.35f;
    
    [Header("신호 외형")]
    [SerializeField] private Color signalColor = Color.white;
    [Tooltip("신호 선의 두께")]
    [SerializeField] private float signalThickness = 0.2f;
    [Tooltip("신호 이동 속도 (단위/초)")]
    [SerializeField] private float signalSpeed = 15f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = 100;
    
    [Header("텍스트 설정")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float textDisplayDuration = 2f;
    
    [Header("반복 설정")]
    [Tooltip("모스 부호를 반복할 횟수 (0 = 무한 반복)")]
    [SerializeField] private int repeatCount = 3;
    [Tooltip("각 반복 사이의 대기 시간 (초)")]
    [SerializeField] private float repeatInterval = 2f;
    [Tooltip("반복 중에도 텍스트 유지")]
    [SerializeField] private bool keepTextDuringRepeat = true;
    
    [Header("활성화 설정")]
    [Tooltip("이 발전기가 완료되면 인디케이터 시작")]
    [SerializeField] private bool startWhenCompleted = true;
    [Tooltip("플레이어가 근처에 있을 때만 활성화")]
    [SerializeField] private bool requirePlayerNearby = false;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask playerLayer;
    
    private GeneratorManager generatorManager;
    private LightGaugeSystem targetGenerator;
    private bool isIndicating = false;
    private bool hasCompletedOnce = false; // 한 번 완료된 후 다시 시작 방지
    private Sprite whiteSprite;
    private Transform playerTransform;
    private Coroutine indicateCoroutine;
    private int currentRepeatCount = 0;
    
    // 모스 부호: GENERATOR
    private readonly string[] morseSequence = new string[]
    {
        "--.",  // G
        ".",    // E
        "-.",   // N
        ".",    // E
        ".-.",  // R
        ".-",   // A
        "-",    // T
        "---",  // O
        ".-."   // R
    };
    
    void Start()
    {
        // 컴포넌트 자동 찾기
        if (lightGaugeSystem == null)
            lightGaugeSystem = GetComponent<LightGaugeSystem>();
        
        // Exit 태그로 GeneratorManager 찾기
        GameObject exitObject = GameObject.FindGameObjectWithTag("Exit");
        if (exitObject != null)
        {
            generatorManager = exitObject.GetComponent<GeneratorManager>();
            if (generatorManager == null)
            {
                Debug.LogWarning($"GeneratorIndicator ({name}): Exit 오브젝트에 GeneratorManager가 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"GeneratorIndicator ({name}): Exit 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
        
        // TMP 텍스트 체크
        if (pingText == null)
        {
            Debug.LogWarning($"GeneratorIndicator ({name}): Ping Text (TMP)가 할당되지 않았습니다. 인스펙터에서 수동으로 할당해주세요.");
        }
        
        // 화이트 스프라이트 생성
        CreateWhiteSprite();
        
        // 플레이어 찾기
        if (requirePlayerNearby)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
        
        // 이벤트 등록
        if (lightGaugeSystem != null && startWhenCompleted)
        {
            lightGaugeSystem.onConditionMet.AddListener(OnGeneratorCompleted);
        }
    }
    
    void Update()
    {
        // 이미 한 번 완료되었으면 다시 시작하지 않음
        if (hasCompletedOnce)
            return;
        
        // exitDoor 조건이 모두 만족되면 인디케이터 중단
        if (generatorManager != null && generatorManager.AreAllConditionsMet())
        {
            if (isIndicating)
            {
                StopIndicating();
                Debug.Log($"GeneratorIndicator ({name}): 모든 조건 만족! 인디케이터 중단");
            }
            return;
        }
        
        // 플레이어 근접 체크가 활성화된 경우
        if (requirePlayerNearby && !IsPlayerNearby())
            return;
        
        // 자동 시작 로직
        if (!isIndicating && lightGaugeSystem != null && lightGaugeSystem.IsConditionMet)
        {
            CheckAndStartIndicating();
        }
    }
    
    private void CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
    
    private bool IsPlayerNearby()
    {
        if (playerTransform == null)
            return false;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= detectionRadius;
    }
    
    private void OnGeneratorCompleted()
    {
        CheckAndStartIndicating();
    }
    
    private void CheckAndStartIndicating()
    {
        // 이미 한 번 완료되었으면 시작하지 않음
        if (hasCompletedOnce)
        {
            Debug.Log($"GeneratorIndicator ({name}): 이미 완료되어 다시 시작하지 않습니다.");
            return;
        }
        
        if (isIndicating || generatorManager == null)
            return;
        
        // exitDoor 조건이 모두 만족되면 시작하지 않음
        if (generatorManager.AreAllConditionsMet())
        {
            Debug.Log($"GeneratorIndicator ({name}): 모든 조건 만족으로 인디케이터 시작 안 함");
            return;
        }
        
        // GeneratorManager 초기화 대기
        if (!generatorManager.IsInitialized)
        {
            Invoke(nameof(CheckAndStartIndicating), 0.5f);
            return;
        }
        
        // 다음 미완료 발전기 찾기
        targetGenerator = generatorManager.FindNearestIncompleteGauge(transform, lightGaugeSystem);
        
        if (targetGenerator != null)
        {
            currentRepeatCount = 0;
            indicateCoroutine = StartCoroutine(IndicateSequenceWithRepeat());
        }
        else
        {
            Debug.Log($"GeneratorIndicator ({name}): 다음 미완료 발전기를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 반복이 포함된 전체 인디케이션 시퀀스
    /// </summary>
    private IEnumerator IndicateSequenceWithRepeat()
    {
        isIndicating = true;
        
        // 1. 터미널 스타일 텍스트 표시 (최초 1회만)
        yield return StartCoroutine(ShowPingText());
        
        // 2. 모스 부호 반복 전송
        int sendCount = 0; // 실제 전송 횟수
        int maxSendCount = repeatCount == 0 ? int.MaxValue : repeatCount; // 0이면 무한
        
        while (sendCount < maxSendCount)
        {
            // exitDoor 조건 체크
            if (generatorManager != null && generatorManager.AreAllConditionsMet())
            {
                Debug.Log($"GeneratorIndicator ({name}): 반복 중 조건 만족 감지, 중단");
                break;
            }
            
            // 타겟이 완료되었는지 체크 - 완료되면 그냥 종료
            if (targetGenerator != null && targetGenerator.IsConditionMet)
            {
                Debug.Log($"GeneratorIndicator ({name}): 타겟 발전기 완료됨, 인디케이터 종료");
                break;
            }
            
            // 모스 부호 1회 전송
            yield return StartCoroutine(SendMorseSignals());
            sendCount++;
            currentRepeatCount = sendCount;
            
            Debug.Log($"GeneratorIndicator ({name}): 모스 부호 전송 완료 ({sendCount}/{(repeatCount == 0 ? "∞" : repeatCount.ToString())}회)");
            
            // 설정된 횟수만큼 전송 완료했으면 종료
            if (sendCount >= maxSendCount)
            {
                Debug.Log($"GeneratorIndicator ({name}): {sendCount}회 전송 완료, 종료");
                break;
            }
            
            // 다음 반복 전 대기 (마지막 전송 후에는 대기 안 함)
            yield return new WaitForSeconds(repeatInterval);
        }
        
        // 3. 텍스트 페이드 아웃
        yield return StartCoroutine(FadeOutText());
        
        isIndicating = false;
        hasCompletedOnce = true; // 한 번 완료되면 다시 시작하지 않음
        indicateCoroutine = null;
    }
    
    /// <summary>
    /// 터미널 스타일 텍스트 타이핑 효과
    /// </summary>
    private IEnumerator ShowPingText()
    {
        if (pingText == null || targetGenerator == null)
            yield break;
        
        
        string fullText = $"ping NextGenerator...";
        
        pingText.text = "";
        pingText.color = new Color(pingText.color.r, pingText.color.g, pingText.color.b, 1f);
        
        foreach (char c in fullText)
        {
            pingText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        if (!keepTextDuringRepeat)
        {
            yield return new WaitForSeconds(textDisplayDuration);
        }
    }
    
    /// <summary>
    /// 모스 부호 시퀀스 전송
    /// </summary>
    private IEnumerator SendMorseSignals()
    {
        if (targetGenerator == null)
            yield break;
        
        for (int i = 0; i < morseSequence.Length; i++)
        {
            string letter = morseSequence[i];
            
            // 각 글자의 모스 부호 전송
            for (int j = 0; j < letter.Length; j++)
            {
                // 중간에 조건 만족 체크
                if (generatorManager != null && generatorManager.AreAllConditionsMet())
                    yield break;
                
                char symbol = letter[j];
                
                if (symbol == '.')
                {
                    SpawnMorseSignal(true); // 점
                    yield return new WaitForSeconds(dotDuration + symbolGap);
                }
                else if (symbol == '-')
                {
                    SpawnMorseSignal(false); // 선
                    yield return new WaitForSeconds(dashDuration + symbolGap);
                }
            }
            
            // 글자 간 간격
            yield return new WaitForSeconds(letterGap);
        }
    }
    
    /// <summary>
    /// 텍스트 페이드 아웃
    /// </summary>
    private IEnumerator FadeOutText()
    {
        if (pingText == null)
            yield break;
        
        float fadeTime = 0.5f;
        float elapsed = 0f;
        Color startColor = pingText.color;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            Color color = startColor;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            pingText.color = color;
            yield return null;
        }
        
        pingText.text = "";
        pingText.color = startColor;
    }
    
    /// <summary>
    /// 모스 부호 신호(점 또는 선) 생성 및 발사
    /// </summary>
    private void SpawnMorseSignal(bool isDot)
    {
        if (targetGenerator == null)
            return;
        
        GameObject signalObj = new GameObject(isDot ? "Dot" : "Dash");
        SpriteRenderer sr = signalObj.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = signalColor;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = orderInLayer;
        
        // 시작 위치
        signalObj.transform.position = transform.position;
        
        // 목표 방향 계산
        Vector3 direction = (targetGenerator.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        signalObj.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 크기 설정 - 더 촘촘하게
        float length = isDot ? 0.2f : 0.6f; // 점은 짧게, 선은 점의 3배
        Vector2 spriteSize = sr.sprite.bounds.size;
        float scaleX = length / Mathf.Max(0.0001f, spriteSize.x);
        float scaleY = signalThickness / Mathf.Max(0.0001f, spriteSize.y);
        signalObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        
        // 이동 코루틴 시작
        StartCoroutine(MoveSignalToTarget(signalObj, sr, direction, length));
    }
    
    /// <summary>
    /// 신호를 목표 지점으로 이동시키고 도달 시 페이드 아웃
    /// </summary>
    private IEnumerator MoveSignalToTarget(GameObject signalObj, SpriteRenderer sr, Vector3 direction, float signalLength)
    {
        if (targetGenerator == null)
        {
            Destroy(signalObj);
            yield break;
        }
        
        Vector3 targetPos = targetGenerator.transform.position;
        float journeyLength = Vector3.Distance(signalObj.transform.position, targetPos);
        float distanceTraveled = 0f;
        
        // 이동 단계
        while (distanceTraveled < journeyLength)
        {
            if (signalObj == null)
                yield break;
            
            float step = signalSpeed * Time.deltaTime;
            signalObj.transform.position += direction * step;
            distanceTraveled += step;
            
            // 목표에 가까워지면 페이드 시작 (거리를 줄여서 빠르게 사라지도록)
            float distanceToTarget = Vector3.Distance(signalObj.transform.position, targetPos);
            float fadeStartDistance = 0.6f;
            
            if (distanceToTarget < fadeStartDistance)
            {
                float alpha = distanceToTarget / fadeStartDistance;
                Color color = sr.color;
                color.a = alpha;
                sr.color = color;
            }
            
            // 목표 도달 체크
            if (distanceToTarget < 0.05f)
                break;
            
            yield return null;
        }
        
        // 신호 제거
        if (signalObj != null)
            Destroy(signalObj);
    }
    
    /// <summary>
    /// 인디케이터 강제 중단
    /// </summary>
    public void StopIndicating()
    {
        if (indicateCoroutine != null)
        {
            StopCoroutine(indicateCoroutine);
            indicateCoroutine = null;
        }
        
        isIndicating = false;
        hasCompletedOnce = true; // 중단해도 다시 시작하지 않음
        currentRepeatCount = 0;
        
        // 텍스트 정리
        if (pingText != null)
        {
            pingText.text = "";
        }
        
        // 남아있는 신호 제거
        ClearAllSignals();
    }
    
    /// <summary>
    /// 모든 신호 오브젝트 제거
    /// </summary>
    private void ClearAllSignals()
    {
        GameObject[] dots = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (var obj in dots)
        {
            if (obj.name == "Dot" || obj.name == "Dash")
            {
                Destroy(obj);
            }
        }
    }
    
    /// <summary>
    /// 수동으로 인디케이터 시작 (외부 호출용)
    /// </summary>
    public void StartIndicating()
    {
        hasCompletedOnce = false; // 수동 시작 시 플래그 리셋
        
        if (!isIndicating && generatorManager != null)
        {
            CheckAndStartIndicating();
        }
    }
    
    /// <summary>
    /// 특정 발전기를 타겟으로 설정하고 인디케이터 시작
    /// </summary>
    public void StartIndicating(LightGaugeSystem target)
    {
        hasCompletedOnce = false; // 수동 시작 시 플래그 리셋
        
        if (!isIndicating && target != null)
        {
            targetGenerator = target;
            currentRepeatCount = 0;
            indicateCoroutine = StartCoroutine(IndicateSequenceWithRepeat());
        }
    }
    
    /// <summary>
    /// 완료 상태를 리셋하여 다시 자동 시작 가능하게 만듦
    /// </summary>
    public void ResetCompletion()
    {
        hasCompletedOnce = false;
        Debug.Log($"GeneratorIndicator ({name}): 완료 상태 리셋됨");
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (lightGaugeSystem != null)
        {
            lightGaugeSystem.onConditionMet.RemoveListener(OnGeneratorCompleted);
        }
        
        StopIndicating();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (requirePlayerNearby)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
        
        if (targetGenerator != null && isIndicating)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetGenerator.transform.position);
            
#if UNITY_EDITOR
            // 현재 반복 횟수 표시
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"Repeat: {currentRepeatCount + 1}/{(repeatCount == 0 ? "∞" : repeatCount.ToString())}"
            );
#endif
        }
    }
}