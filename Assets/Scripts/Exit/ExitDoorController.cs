using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDoorController : MonoBehaviour
{
    [SerializeField] private Transform _doorRotatePoint;
    [SerializeField] private float _openAngle = 180f;
    [SerializeField] private float _openTime = 1.5f;
    [SerializeField] private float _originRotateY;

    [Header("게이지 조건 설정")]
    [Tooltip("자동으로 필요 게이지 개수를 설정")]
    [SerializeField] private bool autoSetRequiredCount = true;
    
    [Tooltip("자동 설정 모드 선택")]
    [SerializeField] private AutoSetMode autoMode = AutoSetMode.MatchRegisteredCount;
    
    [Tooltip("수동 설정 시: 문이 열리기 위해 필요한 최소 게이지 만족 개수")]
    [SerializeField] private int manualRequiredGaugeCount = 1;
    
    private int requiredGaugeCount = 1; // 실제 사용되는 값
    
    public enum AutoSetMode
    {
        MatchRegisteredCount,  // 등록된 게이지 개수와 같게
        MatchStageNumber      // 현재 스테이지 숫자와 같게
    }
    
    [Header("탈출 트리거 설정")]
    [Tooltip("문이 열렸을 때 활성화할 BoxCollider2D (IsTrigger = true 권장)")]
    [SerializeField] private BoxCollider2D exitTriggerCollider;
    [Tooltip("플레이어를 식별할 태그")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("등록 완료 대기 시간")]
    [Tooltip("씬 시작 후 이 시간이 지나면 자동으로 등록을 완료합니다 (0 = 즉시)")]
    [SerializeField] private float autoFinalizeDelay = 0.5f;
    
    private List<LightGaugeSystem> registeredGauges = new List<LightGaugeSystem>();
    private int satisfiedGaugeCount = 0;
    private bool _isOpening = false;
    private bool _isOpen = false;
    private bool _hasPlayerEscaped = false;
    private bool _isRegistrationFinalized = false;

    // 외부 접근용 프로퍼티
    public int RequiredGaugeCount => requiredGaugeCount;
    public int RegisteredGaugeCount => registeredGauges.Count;
    public int SatisfiedGaugeCount => satisfiedGaugeCount;
    public bool IsRegistrationFinalized => _isRegistrationFinalized;

    void Start()
    {
        if (_doorRotatePoint != null)
        {
            _originRotateY = _doorRotatePoint.localEulerAngles.y;
        }
        else
        {
            Debug.LogError("ExitDoorController: _doorRotatePoint가 할당되지 않았습니다!");
        }
        
        satisfiedGaugeCount = 0;
        
        // 수동 모드면 바로 설정
        if (!autoSetRequiredCount)
        {
            requiredGaugeCount = manualRequiredGaugeCount;
            Debug.Log($"ExitDoorController: 수동 모드 - 필요 게이지 개수: {requiredGaugeCount}");
        }
        
        // 탈출 트리거는 초기에 비활성화
        if (exitTriggerCollider != null)
        {
            exitTriggerCollider.enabled = false;
            
            if (!exitTriggerCollider.isTrigger)
            {
                Debug.LogWarning("ExitDoorController: exitTriggerCollider의 IsTrigger가 false입니다. true로 설정하는 것을 권장합니다.");
            }
        }
        else
        {
            Debug.LogWarning("ExitDoorController: exitTriggerCollider가 할당되지 않았습니다!");
        }
        
        // 자동 완료 모드가 활성화되어 있으면 일정 시간 후 등록 완료
        if (autoFinalizeDelay > 0)
        {
            StartCoroutine(AutoFinalizeRegistration());
        }
        else if (autoFinalizeDelay == 0)
        {
            StartCoroutine(FinalizeNextFrame());
        }
    }
    
    private IEnumerator FinalizeNextFrame()
    {
        yield return null;
        FinalizeRegistration();
    }
    
    private IEnumerator AutoFinalizeRegistration()
    {
        yield return new WaitForSeconds(autoFinalizeDelay);
        FinalizeRegistration();
    }

    void Update()
    {
        // 테스트용: 스페이스바로 문 열기
        if (Input.GetKeyDown(KeyCode.Space) && !_isOpen && !_isOpening)
        {
            OpenDoor();
        }
    }
    
    /// <summary>
    /// LightGaugeSystem을 등록하고 이벤트를 구독합니다
    /// </summary>
    public void RegisterGauge(LightGaugeSystem gauge)
    {
        if (gauge == null)
        {
            Debug.LogError("ExitDoorController: null 게이지를 등록하려고 시도했습니다.");
            return;
        }
        
        if (registeredGauges.Contains(gauge))
        {
            Debug.LogWarning($"ExitDoorController: {gauge.gameObject.name}은(는) 이미 등록되어 있습니다.");
            return;
        }
        
        registeredGauges.Add(gauge);
        gauge.onConditionMet.AddListener(() => OnGaugeConditionMet(gauge));
        
        Debug.Log($"ExitDoorController: {gauge.gameObject.name} 등록 완료 (총 {registeredGauges.Count}개)");
    }
    
    /// <summary>
    /// 등록을 완료하고 필요한 게이지 개수를 확정합니다
    /// </summary>
    public void FinalizeRegistration()
    {
        if (_isRegistrationFinalized)
        {
            Debug.LogWarning("ExitDoorController: 이미 등록이 완료되었습니다.");
            return;
        }
        
        _isRegistrationFinalized = true;
        
        // 자동 설정 모드 처리
        if (autoSetRequiredCount)
        {
            switch (autoMode)
            {
                case AutoSetMode.MatchRegisteredCount:
                    requiredGaugeCount = registeredGauges.Count;
                    Debug.Log($"ExitDoorController: 자동 설정 완료 (등록 개수) - 등록된 게이지: {registeredGauges.Count}개, 필요 개수: {requiredGaugeCount}개");
                    break;
                    
                case AutoSetMode.MatchStageNumber:
                    if (GameManager.Instance != null)
                    {
                        requiredGaugeCount = GameManager.Instance.CurrentStage;
                        Debug.Log($"ExitDoorController: 자동 설정 완료 (스테이지 숫자) - 현재 스테이지: {GameManager.Instance.CurrentStage}, 필요 개수: {requiredGaugeCount}개");
                    }
                    else
                    {
                        Debug.LogWarning("ExitDoorController: GameManager를 찾을 수 없어 등록 개수로 설정합니다.");
                        requiredGaugeCount = registeredGauges.Count;
                    }
                    break;
            }
        }
        
        // 이미 조건을 만족했는지 확인
        CheckAndOpenDoor();
    }
    
    /// <summary>
    /// 필요한 게이지 개수를 수동으로 설정합니다
    /// </summary>
    public void SetRequiredGaugeCount(int count)
    {
        if (count < 0)
        {
            Debug.LogError($"ExitDoorController: 잘못된 게이지 개수입니다: {count}");
            return;
        }
        
        requiredGaugeCount = count;
        autoSetRequiredCount = false;
        _isRegistrationFinalized = true;
        
        Debug.Log($"ExitDoorController: 필요 게이지 개수 수동 설정: {requiredGaugeCount}개");
        CheckAndOpenDoor();
    }
    
    /// <summary>
    /// 필요한 게이지 개수를 비율로 설정합니다
    /// </summary>
    public void SetRequiredGaugeCountByRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        int count = Mathf.Max(1, Mathf.CeilToInt(registeredGauges.Count * ratio));
        SetRequiredGaugeCount(count);
        
        Debug.Log($"ExitDoorController: 비율 기반 설정 ({ratio * 100}%) - {count}/{registeredGauges.Count}개");
    }
    
    /// <summary>
    /// 스테이지 번호에 따라 필요 게이지 개수를 설정하는 메서드
    /// </summary>
    public void SetRequiredGaugeCountByStage(int stageNumber)
    {
        if (registeredGauges.Count == 0)
        {
            Debug.LogWarning("ExitDoorController: 등록된 게이지가 없어 스테이지 기반 설정을 할 수 없습니다.");
            return;
        }
        
        int count;
        
        // 스테이지 1-3: 30%
        if (stageNumber <= 3)
        {
            count = Mathf.Max(1, Mathf.CeilToInt(registeredGauges.Count * 0.3f));
        }
        // 스테이지 4-6: 50%
        else if (stageNumber <= 6)
        {
            count = Mathf.Max(1, Mathf.CeilToInt(registeredGauges.Count * 0.5f));
        }
        // 스테이지 7-9: 70%
        else if (stageNumber <= 9)
        {
            count = Mathf.Max(1, Mathf.CeilToInt(registeredGauges.Count * 0.7f));
        }
        // 스테이지 10+: 100%
        else
        {
            count = registeredGauges.Count;
        }
        
        SetRequiredGaugeCount(count);
        Debug.Log($"ExitDoorController: 스테이지 {stageNumber} - 필요 게이지: {count}/{registeredGauges.Count}개");
    }
    
    /// <summary>
    /// 게이지가 조건을 만족했을 때 호출되는 콜백
    /// </summary>
    private void OnGaugeConditionMet(LightGaugeSystem gauge)
    {
        satisfiedGaugeCount++;
        Debug.Log($"ExitDoorController: 게이지 조건 만족! ({satisfiedGaugeCount}/{requiredGaugeCount})");
        CheckAndOpenDoor();
    }
    
    /// <summary>
    /// 조건을 확인하고 문을 엽니다
    /// </summary>
    private void CheckAndOpenDoor()
    {
        if (!_isRegistrationFinalized)
            return;
            
        if (satisfiedGaugeCount >= requiredGaugeCount && !_isOpen && !_isOpening)
        {
            Debug.Log($"ExitDoorController: 모든 조건 만족! 문을 엽니다.");
            OpenDoor();
        }
    }
    
    /// <summary>
    /// 문을 여는 공개 메서드
    /// </summary>
    public void OpenDoor()
    {
        if (!_isOpening && !_isOpen)
        {
            StartCoroutine(OpenDoorCoroutine());
        }
        else if (_isOpen)
        {
            Debug.Log("문이 이미 열려있습니다.");
        }
    }

    /// <summary>
    /// 문을 닫는 공개 메서드
    /// </summary>
    public void CloseDoor()
    {
        if (!_isOpening && _isOpen)
        {
            StartCoroutine(CloseDoorCoroutine());
        }
    }

    private IEnumerator OpenDoorCoroutine()
    {
        _isOpening = true;
        float elapsedTime = 0f;
        float startRotation = _originRotateY;
        float targetRotation = _originRotateY + _openAngle;

        while (elapsedTime < _openTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _openTime;
            
            float currentRotation = Mathf.Lerp(startRotation, targetRotation, t);
            _doorRotatePoint.localEulerAngles = new Vector3(
                _doorRotatePoint.localEulerAngles.x,
                currentRotation,
                _doorRotatePoint.localEulerAngles.z
            );

            yield return null;
        }

        _doorRotatePoint.localEulerAngles = new Vector3(
            _doorRotatePoint.localEulerAngles.x,
            targetRotation,
            _doorRotatePoint.localEulerAngles.z
        );

        _isOpening = false;
        _isOpen = true;
        
        ActivateExitTrigger();
        
        Debug.Log("ExitDoorController: 문이 완전히 열렸습니다!");
    }

    private IEnumerator CloseDoorCoroutine()
    {
        _isOpening = true;
        
        if (exitTriggerCollider != null)
        {
            exitTriggerCollider.enabled = false;
        }
        
        float elapsedTime = 0f;
        float startRotation = _originRotateY + _openAngle;
        float targetRotation = _originRotateY;

        while (elapsedTime < _openTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _openTime;
            
            float currentRotation = Mathf.Lerp(startRotation, targetRotation, t);
            _doorRotatePoint.localEulerAngles = new Vector3(
                _doorRotatePoint.localEulerAngles.x,
                currentRotation,
                _doorRotatePoint.localEulerAngles.z
            );

            yield return null;
        }

        _doorRotatePoint.localEulerAngles = new Vector3(
            _doorRotatePoint.localEulerAngles.x,
            targetRotation,
            _doorRotatePoint.localEulerAngles.z
        );

        _isOpening = false;
        _isOpen = false;
    }
    
    private void ActivateExitTrigger()
    {
        if (exitTriggerCollider != null)
        {
            exitTriggerCollider.enabled = true;
            Debug.Log("ExitDoorController: 탈출 트리거 활성화!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasPlayerEscaped || !_isOpen)
            return;
        
        if (other.CompareTag(playerTag))
        {
            OnPlayerEscape();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hasPlayerEscaped || !_isOpen)
            return;
        
        if (collision.gameObject.CompareTag(playerTag))
        {
            OnPlayerEscape();
        }
    }
    
    private void OnPlayerEscape()
    {
        if (_hasPlayerEscaped)
            return;
        
        _hasPlayerEscaped = true;
        
        Debug.Log("ExitDoorController: 플레이어 탈출 성공!");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceStageAndReload();
        }
        else
        {
            Debug.LogError("ExitDoorController: GameManager.Instance를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 디버그용: 현재 상태 출력
    /// </summary>
    public void PrintStatus()
    {
        Debug.Log($"=== ExitDoorController 상태 ===");
        Debug.Log($"자동 설정 모드: {autoSetRequiredCount} ({autoMode})");
        Debug.Log($"등록 완료 여부: {_isRegistrationFinalized}");
        Debug.Log($"등록된 게이지 수: {registeredGauges.Count}");
        Debug.Log($"조건 만족 게이지 수: {satisfiedGaugeCount}/{requiredGaugeCount}");
        Debug.Log($"문 상태: {(_isOpen ? "열림" : "닫힘")}");
        Debug.Log($"탈출 완료 여부: {_hasPlayerEscaped}");
        
        Debug.Log("등록된 게이지 목록:");
        foreach (var gauge in registeredGauges)
        {
            Debug.Log($"  - {gauge.gameObject.name} (조건 만족: {gauge.IsConditionMet})");
        }
    }
}