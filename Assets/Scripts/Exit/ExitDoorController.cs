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
    [Tooltip("문이 열리기 위해 필요한 최소 게이지 만족 개수")]
    [SerializeField] private int requiredGaugeCount = 1;
    
    private List<LightGaugeSystem> registeredGauges = new List<LightGaugeSystem>();
    private int satisfiedGaugeCount = 0;
    private bool _isOpening = false;
    private bool _isOpen = false;

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
        
        // 게이지의 조건 만족 이벤트 구독
        gauge.onConditionMet.AddListener(() => OnGaugeConditionMet(gauge));
        
        Debug.Log($"ExitDoorController: {gauge.gameObject.name} 등록 완료 (총 {registeredGauges.Count}개)");
    }
    
    /// <summary>
    /// 게이지가 조건을 만족했을 때 호출되는 콜백
    /// </summary>
    private void OnGaugeConditionMet(LightGaugeSystem gauge)
    {
        satisfiedGaugeCount++;
        
        Debug.Log($"ExitDoorController: 게이지 조건 만족! ({satisfiedGaugeCount}/{requiredGaugeCount})");
        
        // 필요한 개수만큼 만족했는지 확인
        CheckAndOpenDoor();
    }
    
    /// <summary>
    /// 조건을 확인하고 문을 엽니다
    /// </summary>
    private void CheckAndOpenDoor()
    {
        if (satisfiedGaugeCount >= requiredGaugeCount && !_isOpen && !_isOpening)
        {
            Debug.Log($"ExitDoorController: 모든 조건 만족! 문을 엽니다.");
            _isOpen = true;
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

    /// <summary>
    /// 문을 등속도로 여는 코루틴
    /// </summary>
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
        
        Debug.Log("ExitDoorController: 문이 완전히 열렸습니다!");
    }

    /// <summary>
    /// 문을 등속도로 닫는 코루틴
    /// </summary>
    private IEnumerator CloseDoorCoroutine()
    {
        _isOpening = true;
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
    
    // 디버그용: 현재 등록된 게이지 정보
    public void PrintRegisteredGauges()
    {
        Debug.Log($"=== ExitDoorController 상태 ===");
        Debug.Log($"등록된 게이지 수: {registeredGauges.Count}");
        Debug.Log($"조건 만족 게이지 수: {satisfiedGaugeCount}/{requiredGaugeCount}");
        Debug.Log($"문 상태: {(_isOpen ? "열림" : "닫힘")}");
    }
}