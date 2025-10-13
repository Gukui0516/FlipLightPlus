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
    
    [Header("아웃라인 설정")]
    [SerializeField] private bool enableOutline = true;
    [Tooltip("아웃라인 색상")]
    [SerializeField] private Color outlineColor = Color.black;
    [Tooltip("아웃라인 굵기 (0.01 ~ 0.2 권장)")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float outlineThickness = 0.05f;
    [Tooltip("아웃라인 방향 개수 (4 또는 8)")]
    [SerializeField] private int outlineDirections = 8;
    
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
    private bool hasCompletedOnce = false;
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
        if (lightGaugeSystem == null)
            lightGaugeSystem = GetComponent<LightGaugeSystem>();
        
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
        
        if (pingText == null)
        {
            Debug.LogWarning($"GeneratorIndicator ({name}): Ping Text (TMP)가 할당되지 않았습니다. 인스펙터에서 수동으로 할당해주세요.");
        }
        
        CreateWhiteSprite();
        
        if (requirePlayerNearby)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
        
        if (lightGaugeSystem != null && startWhenCompleted)
        {
            lightGaugeSystem.onConditionMet.AddListener(OnGeneratorCompleted);
        }
    }
    
    void Update()
    {
        if (hasCompletedOnce)
            return;
        
        if (generatorManager != null && generatorManager.AreAllConditionsMet())
        {
            if (isIndicating)
            {
                StopIndicating();
                Debug.Log($"GeneratorIndicator ({name}): 모든 조건 만족! 인디케이터 중단");
            }
            return;
        }
        
        if (requirePlayerNearby && !IsPlayerNearby())
            return;
        
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
        if (hasCompletedOnce)
        {
            Debug.Log($"GeneratorIndicator ({name}): 이미 완료되어 다시 시작하지 않습니다.");
            return;
        }
        
        if (isIndicating || generatorManager == null)
            return;
        
        if (generatorManager.AreAllConditionsMet())
        {
            Debug.Log($"GeneratorIndicator ({name}): 모든 조건 만족으로 인디케이터 시작 안 함");
            return;
        }
        
        if (!generatorManager.IsInitialized)
        {
            Invoke(nameof(CheckAndStartIndicating), 0.5f);
            return;
        }
        
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
    
    private IEnumerator IndicateSequenceWithRepeat()
    {
        isIndicating = true;
        
        yield return StartCoroutine(ShowPingText());
        
        int sendCount = 0;
        int maxSendCount = repeatCount == 0 ? int.MaxValue : repeatCount;
        
        while (sendCount < maxSendCount)
        {
            if (generatorManager != null && generatorManager.AreAllConditionsMet())
            {
                Debug.Log($"GeneratorIndicator ({name}): 반복 중 조건 만족 감지, 중단");
                break;
            }
            
            if (targetGenerator != null && targetGenerator.IsConditionMet)
            {
                Debug.Log($"GeneratorIndicator ({name}): 타겟 발전기 완료됨, 인디케이터 종료");
                break;
            }
            
            yield return StartCoroutine(SendMorseSignals());
            sendCount++;
            currentRepeatCount = sendCount;
            
            Debug.Log($"GeneratorIndicator ({name}): 모스 부호 전송 완료 ({sendCount}/{(repeatCount == 0 ? "∞" : repeatCount.ToString())}회)");
            
            if (sendCount >= maxSendCount)
            {
                Debug.Log($"GeneratorIndicator ({name}): {sendCount}회 전송 완료, 종료");
                break;
            }
            
            yield return new WaitForSeconds(repeatInterval);
        }
        
        yield return StartCoroutine(FadeOutText());
        
        isIndicating = false;
        hasCompletedOnce = true;
        indicateCoroutine = null;
    }
    
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
    
    private IEnumerator SendMorseSignals()
    {
        if (targetGenerator == null)
            yield break;
        
        for (int i = 0; i < morseSequence.Length; i++)
        {
            string letter = morseSequence[i];
            
            for (int j = 0; j < letter.Length; j++)
            {
                if (generatorManager != null && generatorManager.AreAllConditionsMet())
                    yield break;
                
                char symbol = letter[j];
                
                if (symbol == '.')
                {
                    SpawnMorseSignal(true);
                    yield return new WaitForSeconds(dotDuration + symbolGap);
                }
                else if (symbol == '-')
                {
                    SpawnMorseSignal(false);
                    yield return new WaitForSeconds(dashDuration + symbolGap);
                }
            }
            
            yield return new WaitForSeconds(letterGap);
        }
    }
    
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
    /// 모스 부호 신호(점 또는 선) 생성 및 발사 (아웃라인 포함)
    /// </summary>
    private void SpawnMorseSignal(bool isDot)
    {
        if (targetGenerator == null)
            return;
        
        // 부모 오브젝트 생성
        GameObject signalParent = new GameObject(isDot ? "Dot" : "Dash");
        signalParent.transform.position = transform.position;
        
        // 목표 방향 계산
        Vector3 direction = (targetGenerator.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        signalParent.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 크기 설정
        float length = isDot ? 0.2f : 0.6f;
        
        // 아웃라인 생성 (활성화 시)
        List<SpriteRenderer> outlineRenderers = new List<SpriteRenderer>();
        if (enableOutline)
        {
            Vector2[] offsets = GetOutlineOffsets();
            
            foreach (Vector2 offset in offsets)
            {
                GameObject outlineObj = new GameObject("Outline");
                outlineObj.transform.SetParent(signalParent.transform);
                outlineObj.transform.localPosition = offset * outlineThickness;
                outlineObj.transform.localRotation = Quaternion.identity;
                
                SpriteRenderer outlineSR = outlineObj.AddComponent<SpriteRenderer>();
                outlineSR.sprite = whiteSprite;
                outlineSR.color = outlineColor;
                outlineSR.sortingLayerName = sortingLayerName;
                outlineSR.sortingOrder = orderInLayer - 1;
                
                Vector2 spriteSize = outlineSR.sprite.bounds.size;
                float scaleX = length / Mathf.Max(0.0001f, spriteSize.x);
                float scaleY = signalThickness / Mathf.Max(0.0001f, spriteSize.y);
                outlineObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                
                outlineRenderers.Add(outlineSR);
            }
        }
        
        // 메인 신호 생성
        GameObject mainSignalObj = new GameObject("Main");
        mainSignalObj.transform.SetParent(signalParent.transform);
        mainSignalObj.transform.localPosition = Vector3.zero;
        mainSignalObj.transform.localRotation = Quaternion.identity;
        
        SpriteRenderer mainSR = mainSignalObj.AddComponent<SpriteRenderer>();
        mainSR.sprite = whiteSprite;
        mainSR.color = signalColor;
        mainSR.sortingLayerName = sortingLayerName;
        mainSR.sortingOrder = orderInLayer;
        
        Vector2 mainSpriteSize = mainSR.sprite.bounds.size;
        float mainScaleX = length / Mathf.Max(0.0001f, mainSpriteSize.x);
        float mainScaleY = signalThickness / Mathf.Max(0.0001f, mainSpriteSize.y);
        mainSignalObj.transform.localScale = new Vector3(mainScaleX, mainScaleY, 1f);
        
        // 이동 코루틴 시작
        StartCoroutine(MoveSignalToTarget(signalParent, mainSR, outlineRenderers, direction, length));
    }
    
    /// <summary>
    /// 아웃라인 오프셋 방향 계산
    /// </summary>
    private Vector2[] GetOutlineOffsets()
    {
        if (outlineDirections == 4)
        {
            // 4방향 (상하좌우)
            return new Vector2[]
            {
                new Vector2(0, 1),   // 위
                new Vector2(0, -1),  // 아래
                new Vector2(-1, 0),  // 왼쪽
                new Vector2(1, 0)    // 오른쪽
            };
        }
        else // 8방향
        {
            return new Vector2[]
            {
                new Vector2(0, 1),      // 위
                new Vector2(1, 1),      // 오른쪽 위
                new Vector2(1, 0),      // 오른쪽
                new Vector2(1, -1),     // 오른쪽 아래
                new Vector2(0, -1),     // 아래
                new Vector2(-1, -1),    // 왼쪽 아래
                new Vector2(-1, 0),     // 왼쪽
                new Vector2(-1, 1)      // 왼쪽 위
            };
        }
    }
    
    /// <summary>
    /// 신호를 목표 지점으로 이동시키고 도달 시 페이드 아웃
    /// </summary>
    private IEnumerator MoveSignalToTarget(GameObject signalParent, SpriteRenderer mainSR, 
        List<SpriteRenderer> outlineRenderers, Vector3 direction, float signalLength)
    {
        if (targetGenerator == null)
        {
            Destroy(signalParent);
            yield break;
        }
        
        Vector3 targetPos = targetGenerator.transform.position;
        float journeyLength = Vector3.Distance(signalParent.transform.position, targetPos);
        float distanceTraveled = 0f;
        
        while (distanceTraveled < journeyLength)
        {
            if (signalParent == null)
                yield break;
            
            float step = signalSpeed * Time.deltaTime;
            signalParent.transform.position += direction * step;
            distanceTraveled += step;
            
            float distanceToTarget = Vector3.Distance(signalParent.transform.position, targetPos);
            float fadeStartDistance = 0.6f;
            
            if (distanceToTarget < fadeStartDistance)
            {
                float alpha = distanceToTarget / fadeStartDistance;
                
                // 메인 신호 페이드
                Color mainColor = mainSR.color;
                mainColor.a = alpha;
                mainSR.color = mainColor;
                
                // 아웃라인 페이드
                foreach (var outlineSR in outlineRenderers)
                {
                    if (outlineSR != null)
                    {
                        Color outlineCol = outlineSR.color;
                        outlineCol.a = alpha;
                        outlineSR.color = outlineCol;
                    }
                }
            }
            
            if (distanceToTarget < 0.05f)
                break;
            
            yield return null;
        }
        
        if (signalParent != null)
            Destroy(signalParent);
    }
    
    public void StopIndicating()
    {
        if (indicateCoroutine != null)
        {
            StopCoroutine(indicateCoroutine);
            indicateCoroutine = null;
        }
        
        isIndicating = false;
        hasCompletedOnce = true;
        currentRepeatCount = 0;
        
        if (pingText != null)
        {
            pingText.text = "";
        }
        
        ClearAllSignals();
    }
    
    private void ClearAllSignals()
    {
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (var obj in allObjects)
        {
            if (obj.name == "Dot" || obj.name == "Dash")
            {
                Destroy(obj);
            }
        }
    }
    
    public void StartIndicating()
    {
        hasCompletedOnce = false;
        
        if (!isIndicating && generatorManager != null)
        {
            CheckAndStartIndicating();
        }
    }
    
    public void StartIndicating(LightGaugeSystem target)
    {
        hasCompletedOnce = false;
        
        if (!isIndicating && target != null)
        {
            targetGenerator = target;
            currentRepeatCount = 0;
            indicateCoroutine = StartCoroutine(IndicateSequenceWithRepeat());
        }
    }
    
    public void ResetCompletion()
    {
        hasCompletedOnce = false;
        Debug.Log($"GeneratorIndicator ({name}): 완료 상태 리셋됨");
    }
    
    private void OnDestroy()
    {
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
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"Repeat: {currentRepeatCount + 1}/{(repeatCount == 0 ? "∞" : repeatCount.ToString())}"
            );
#endif
        }
    }
}