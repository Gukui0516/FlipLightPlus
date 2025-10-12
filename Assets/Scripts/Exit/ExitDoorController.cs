using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 발전기 완료 정보를 담는 클래스
/// </summary>
[System.Serializable]
public class GeneratorCompleteInfo
{
    public LightGaugeSystem completedGauge;    // 완료된 발전기
    public int satisfiedCount;                  // 현재까지 만족한 개수
    public int requiredCount;                   // 필요한 전체 개수
    public bool allConditionsMet;               // 모든 조건 만족 여부
    
    public GeneratorCompleteInfo(LightGaugeSystem gauge, int satisfied, int required, bool allMet)
    {
        completedGauge = gauge;
        satisfiedCount = satisfied;
        requiredCount = required;
        allConditionsMet = allMet;
    }
}

/// <summary>
/// 발전기 완료 이벤트
/// </summary>
[System.Serializable]
public class GeneratorCompleteEvent : UnityEvent<GeneratorCompleteInfo> { }

public class ExitDoorController : MonoBehaviour
{
    [Header("문 회전 설정")]
    [SerializeField] private Transform _leftDoorRotatePoint;
    [SerializeField] private Transform _rightDoorRotatePoint;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _openTime = 1.5f;
    private float _leftOriginRotateY;
    private float _rightOriginRotateY;

    [Header("카메라 뷰포트 체크")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("문이 카메라 뷰포트에 들어왔는지 체크할 Transform (문의 중심점 권장)")]
    [SerializeField] private Transform doorCheckPoint;
    [Tooltip("카메라 뷰포트 체크 간격 (초)")]
    [SerializeField] private float viewportCheckInterval = 0.2f;

    [Header("게이지 조건 설정")]
    [Tooltip("자동으로 필요 게이지 개수를 설정")]
    [SerializeField] private bool autoSetRequiredCount = true;
    
    [Tooltip("자동 설정 모드 선택")]
    [SerializeField] private AutoSetMode autoMode = AutoSetMode.MatchRegisteredCount;

    [Tooltip("수동 설정 시: 문이 열리기 위해 필요한 최소 게이지 만족 개수")]
    [SerializeField] private int manualRequiredGaugeCount = 1;
    
    [Header("이펙트 설정")]
    [Tooltip("문이 열릴 조건이 만족되었을 때 활성화할 이펙트 게임오브젝트")]
    [SerializeField] private GameObject openEffectGameObject;
    
    [Header("이벤트")]
    [Tooltip("발전기가 완료될 때마다 발행되는 이벤트")]
    public GeneratorCompleteEvent onGeneratorComplete = new GeneratorCompleteEvent();
    [Tooltip("플레이어가 탈출할 때 발행되는 이벤트 (비어있으면 기본 동작 실행)")]
    public UnityEvent onPlayerEscape = new UnityEvent();
    private int requiredGaugeCount = 1;
    
    public enum AutoSetMode
    {
        MatchRegisteredCount,
        MatchStageNumber
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
    public bool IsOpen => _isOpen;
    public bool _hasPlayerEscaped = false;
    private bool _isRegistrationFinalized = false;
    private bool _isWaitingForCameraView = false;

    public int RequiredGaugeCount => requiredGaugeCount;
    public int RegisteredGaugeCount => registeredGauges.Count;
    public int SatisfiedGaugeCount => satisfiedGaugeCount;
    public bool IsRegistrationFinalized => _isRegistrationFinalized;
    public bool AreAllConditionsMet => _isRegistrationFinalized && satisfiedGaugeCount >= requiredGaugeCount;

    void Start()
    {
        // 왼쪽 문 초기화
        if (_leftDoorRotatePoint != null)
        {
            _leftOriginRotateY = _leftDoorRotatePoint.localEulerAngles.y;
        }
        else
        {
            Debug.LogError("ExitDoorController: _leftDoorRotatePoint가 할당되지 않았습니다!");
        }
        
        // 오른쪽 문 초기화
        if (_rightDoorRotatePoint != null)
        {
            _rightOriginRotateY = _rightDoorRotatePoint.localEulerAngles.y;
        }
        else
        {
            Debug.LogError("ExitDoorController: _rightDoorRotatePoint가 할당되지 않았습니다!");
        }
        
        // 메인 카메라 자동 할당
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("ExitDoorController: 메인 카메라를 찾을 수 없습니다!");
            }
        }
        
        // doorCheckPoint 자동 할당 (할당되지 않은 경우 자신의 Transform 사용)
        if (doorCheckPoint == null)
        {
            doorCheckPoint = transform;
            Debug.Log("ExitDoorController: doorCheckPoint가 할당되지 않아 자신의 Transform을 사용합니다.");
        }
        
        // 이펙트 초기화 (비활성화 상태로)
        if (openEffectGameObject != null)
        {
            openEffectGameObject.SetActive(false);
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
        
    }
    
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
    
    public void FinalizeRegistration()
    {
        if (_isRegistrationFinalized)
        {
            Debug.LogWarning("ExitDoorController: 이미 등록이 완료되었습니다.");
            return;
        }
        
        _isRegistrationFinalized = true;
        
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
        
        CheckAndOpenDoor();
    }
    
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
    
    public void SetRequiredGaugeCountByRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        int count = Mathf.Max(1, Mathf.CeilToInt(registeredGauges.Count * ratio));
        SetRequiredGaugeCount(count);
        
        Debug.Log($"ExitDoorController: 비율 기반 설정 ({ratio * 100}%) - {count}/{registeredGauges.Count}개");
    }
    
    public void SetRequiredGaugeCountByStage(int stageNumber)
    {
        if (registeredGauges.Count == 0)
        {
            Debug.LogWarning("ExitDoorController: 등록된 게이지가 없어 스테이지 기반 설정을 할 수 없습니다.");
            return;
        }
        
        int count;
        
        if (stageNumber <= 3)
        {
            count = Mathf.Max(1, Mathf.CeilToInt(registeredGauges.Count * 0.3f));
        }
        else if (stageNumber <= 6)
        {
            count = Mathf.Max(1, Mathf.CeilToInt(registeredGauges.Count * 0.5f));
        }
        else if (stageNumber <= 9)
        {
            count = Mathf.Max(1, Mathf.CeilToInt(registeredGauges.Count * 0.7f));
        }
        else
        {
            count = registeredGauges.Count;
        }
        
        SetRequiredGaugeCount(count);
        Debug.Log($"ExitDoorController: 스테이지 {stageNumber} - 필요 게이지: {count}/{registeredGauges.Count}개");
    }
    
    private void OnGaugeConditionMet(LightGaugeSystem gauge)
    {
        satisfiedGaugeCount++;
        Debug.Log($"ExitDoorController: 게이지 조건 만족! ({satisfiedGaugeCount}/{requiredGaugeCount})");
        
        // 발전기 완료 이벤트 발행
        bool allMet = satisfiedGaugeCount >= requiredGaugeCount;
        GeneratorCompleteInfo info = new GeneratorCompleteInfo(
            gauge, 
            satisfiedGaugeCount, 
            requiredGaugeCount, 
            allMet
        );
        onGeneratorComplete.Invoke(info);
        
        Debug.Log($"ExitDoorController: 발전기 완료 이벤트 발행 - {gauge.gameObject.name} ({satisfiedGaugeCount}/{requiredGaugeCount}), 모든 조건 만족: {allMet}");
        
        CheckAndOpenDoor();
    }
    
    /// <summary>
    /// 조건을 확인하고, 조건 만족 시 이펙트를 활성화하고 카메라 뷰포트 대기 상태로 전환
    /// </summary>
    private void CheckAndOpenDoor()
    {
        if (!_isRegistrationFinalized)
            return;
            
        if (satisfiedGaugeCount >= requiredGaugeCount && !_isOpen && !_isOpening && !_isWaitingForCameraView)
        {
            Debug.Log($"ExitDoorController: 모든 조건 만족! 이펙트 활성화 및 카메라 뷰포트 대기 중...");
            
            // 조건 만족 시 이펙트 활성화
            ActivateOpenEffect();
            
            // 카메라 뷰포트 대기 시작
            StartCoroutine(WaitForCameraView());
        }
    }
    
    /// <summary>
    /// 문 열림 이펙트 활성화
    /// </summary>
    private void ActivateOpenEffect()
    {
        if (openEffectGameObject != null)
        {
            openEffectGameObject.SetActive(true);
            Debug.Log("ExitDoorController: 문 열림 이펙트 활성화!");
        }
    }
    
    /// <summary>
    /// 문이 카메라 뷰포트에 보일 때까지 대기
    /// </summary>
    private IEnumerator WaitForCameraView()
    {
        _isWaitingForCameraView = true;
        
        while (!IsDoorInCameraView())
        {
            yield return new WaitForSeconds(viewportCheckInterval);
        }
        
        Debug.Log("ExitDoorController: 문이 카메라에 보입니다! 문을 엽니다.");
        _isWaitingForCameraView = false;
        OpenDoorImmediately();
    }
    
    /// <summary>
    /// 문이 카메라 뷰포트 내에 있는지 확인
    /// </summary>
    private bool IsDoorInCameraView()
    {
        if (mainCamera == null || doorCheckPoint == null)
        {
            Debug.LogWarning("ExitDoorController: 카메라 또는 체크포인트가 없어 즉시 열립니다.");
            return true;
        }
        
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(doorCheckPoint.position);
        
        // 뷰포트 좌표는 (0,0) ~ (1,1) 범위
        // z > 0 이면 카메라 앞쪽에 있음
        bool isInView = viewportPoint.z > 0 && 
                        viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
                        viewportPoint.y >= 0 && viewportPoint.y <= 1;
        
        Debug.Log($"ExitDoorController: 뷰포트 체크 - Position: {viewportPoint}, InView: {isInView}");
        
        return isInView;
    }
    
    /// <summary>
    /// 즉시 문을 여는 메서드 (테스트용)
    /// </summary>
    public void OpenDoorImmediately()
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
        
        // 왼쪽 문: 반시계 방향 회전 (음수)
        float leftStartRotation = _leftOriginRotateY;
        float leftTargetRotation = _leftOriginRotateY - _openAngle;
        
        // 오른쪽 문: 시계 방향 회전 (양수)
        float rightStartRotation = _rightOriginRotateY;
        float rightTargetRotation = _rightOriginRotateY + _openAngle;

        while (elapsedTime < _openTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _openTime;
            
            // 왼쪽 문 회전
            if (_leftDoorRotatePoint != null)
            {
                float leftCurrentRotation = Mathf.Lerp(leftStartRotation, leftTargetRotation, t);
                _leftDoorRotatePoint.localEulerAngles = new Vector3(
                    _leftDoorRotatePoint.localEulerAngles.x,
                    leftCurrentRotation,
                    _leftDoorRotatePoint.localEulerAngles.z
                );
            }
            
            // 오른쪽 문 회전
            if (_rightDoorRotatePoint != null)
            {
                float rightCurrentRotation = Mathf.Lerp(rightStartRotation, rightTargetRotation, t);
                _rightDoorRotatePoint.localEulerAngles = new Vector3(
                    _rightDoorRotatePoint.localEulerAngles.x,
                    rightCurrentRotation,
                    _rightDoorRotatePoint.localEulerAngles.z
                );
            }

            yield return null;
        }

        // 최종 위치 설정
        if (_leftDoorRotatePoint != null)
        {
            _leftDoorRotatePoint.localEulerAngles = new Vector3(
                _leftDoorRotatePoint.localEulerAngles.x,
                leftTargetRotation,
                _leftDoorRotatePoint.localEulerAngles.z
            );
        }
        
        if (_rightDoorRotatePoint != null)
        {
            _rightDoorRotatePoint.localEulerAngles = new Vector3(
                _rightDoorRotatePoint.localEulerAngles.x,
                rightTargetRotation,
                _rightDoorRotatePoint.localEulerAngles.z
            );
        }

        _isOpening = false;
        _isOpen = true;
        
        ActivateExitTrigger();
        
        Debug.Log("ExitDoorController: 양쪽 문이 완전히 열렸습니다!");
    }

    private IEnumerator CloseDoorCoroutine()
    {
        _isOpening = true;
        
        if (exitTriggerCollider != null)
        {
            exitTriggerCollider.enabled = false;
        }
        
        float elapsedTime = 0f;
        
        // 왼쪽 문: 원위치로
        float leftStartRotation = _leftOriginRotateY - _openAngle;
        float leftTargetRotation = _leftOriginRotateY;
        
        // 오른쪽 문: 원위치로
        float rightStartRotation = _rightOriginRotateY + _openAngle;
        float rightTargetRotation = _rightOriginRotateY;

        while (elapsedTime < _openTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _openTime;
            
            // 왼쪽 문 회전
            if (_leftDoorRotatePoint != null)
            {
                float leftCurrentRotation = Mathf.Lerp(leftStartRotation, leftTargetRotation, t);
                _leftDoorRotatePoint.localEulerAngles = new Vector3(
                    _leftDoorRotatePoint.localEulerAngles.x,
                    leftCurrentRotation,
                    _leftDoorRotatePoint.localEulerAngles.z
                );
            }
            
            // 오른쪽 문 회전
            if (_rightDoorRotatePoint != null)
            {
                float rightCurrentRotation = Mathf.Lerp(rightStartRotation, rightTargetRotation, t);
                _rightDoorRotatePoint.localEulerAngles = new Vector3(
                    _rightDoorRotatePoint.localEulerAngles.x,
                    rightCurrentRotation,
                    _rightDoorRotatePoint.localEulerAngles.z
                );
            }

            yield return null;
        }

        // 최종 위치 설정
        if (_leftDoorRotatePoint != null)
        {
            _leftDoorRotatePoint.localEulerAngles = new Vector3(
                _leftDoorRotatePoint.localEulerAngles.x,
                leftTargetRotation,
                _leftDoorRotatePoint.localEulerAngles.z
            );
        }
        
        if (_rightDoorRotatePoint != null)
        {
            _rightDoorRotatePoint.localEulerAngles = new Vector3(
                _rightDoorRotatePoint.localEulerAngles.x,
                rightTargetRotation,
                _rightDoorRotatePoint.localEulerAngles.z
            );
        }

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
    
    protected virtual void OnPlayerEscape()  // private -> protected virtual 로 변경
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
    
    public void PrintStatus()
    {
        Debug.Log($"=== ExitDoorController 상태 ===");
        Debug.Log($"자동 설정 모드: {autoSetRequiredCount} ({autoMode})");
        Debug.Log($"등록 완료 여부: {_isRegistrationFinalized}");
        Debug.Log($"등록된 게이지 수: {registeredGauges.Count}");
        Debug.Log($"조건 만족 게이지 수: {satisfiedGaugeCount}/{requiredGaugeCount}");
        Debug.Log($"문 상태: {(_isOpen ? "열림" : "닫힘")}");
        Debug.Log($"카메라 대기 중: {_isWaitingForCameraView}");
        Debug.Log($"탈출 완료 여부: {_hasPlayerEscaped}");
        Debug.Log($"이펙트 활성화 여부: {(openEffectGameObject != null ? openEffectGameObject.activeSelf.ToString() : "할당되지 않음")}");
        
        Debug.Log("등록된 게이지 목록:");
        foreach (var gauge in registeredGauges)
        {
            Debug.Log($"  - {gauge.gameObject.name} (조건 만족: {gauge.IsConditionMet})");
        }
    }
}