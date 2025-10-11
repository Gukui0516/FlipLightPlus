using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ExitDoorController와 연동되어 조건 만족 시 문에서 빛을 발산하는 이펙트
/// 360도 방사형 빛줄기 + 무작위 추가 플래시
/// </summary>
public class DoorLightIndicator : MonoBehaviour
{
    [Header("Door Reference")]
    public Transform doorTransform; // 문의 위치 (자동 할당)
    
    [Header("Basic Rotating Rays (기본 회전 빛줄기)")]
    [Tooltip("360도로 회전하는 빛줄기 개수")]
    public int basicRayCount = 5;
    [Tooltip("빛줄기 길이")]
    public float basicRayLength = 75f;
    [Tooltip("빛줄기 시작 두께 (문 쪽, 얇게 - 고정)")]
    public float basicStartWidth = 0.05f;
    
    [Header("Dynamic Width Settings (거리 기반 굵기 조절)")]
    [Tooltip("이 거리보다 가까우면 최소 굵기")]
    public float minDistanceForWidth = 20f;
    [Tooltip("이 거리보다 멀면 최대 굵기")]
    public float maxDistanceForWidth = 150f;
    [Tooltip("가까울 때 빛줄기 끝 두께 (최소)")]
    public float minEndWidth = 0.8f;
    [Tooltip("멀 때 빛줄기 끝 두께 (최대)")]
    public float maxEndWidth = 3.2f;
    
    [Header("Extra Flash Rays (추가 무작위 빛줄기)")]
    [Tooltip("추가 무작위 플래시용 빛줄기 개수")]
    public int extraFlashRayCount = 3;
    [Tooltip("추가 빛줄기 길이 (기본보다 더 길게)")]
    public float extraRayLength = 100f;
    
    [Header("Animation Settings")]
    public float rotationSpeed = 20f;
    public float pulseSpeed = 2f;
    public float flickerSpeed = 5f;
    public float minIntensity = 0.3f;
    public float maxIntensity = 0.9f;
    
    [Header("Random Flash Settings")]
    [Tooltip("빛줄기가 나타나는 최소 간격 (초)")]
    public float minFlashInterval = 2f;
    [Tooltip("빛줄기가 나타나는 최대 간격 (초)")]
    public float maxFlashInterval = 5f;
    [Tooltip("한번에 나타나는 빛줄기 최소 개수")]
    public int minFlashCount = 1;
    [Tooltip("한번에 나타나는 빛줄기 최대 개수")]
    public int maxFlashCount = 2;
    [Tooltip("빛줄기 페이드 인 시간")]
    public float fadeInDuration = 0.3f;
    [Tooltip("빛줄기 유지 시간")]
    public float flashDuration = 0.8f;
    [Tooltip("빛줄기 페이드 아웃 시간")]
    public float fadeOutDuration = 0.5f;
    
    [Header("Light Color")]
    public Color lightColor = new Color(1f, 1f, 0.8f, 0.7f);
    
    private class RayFlashState
    {
        public bool isFlashing = false;
        public float currentAlpha = 0f;
        public float flashTimer = 0f;
        public float targetAngle = 0f;
        
        public enum FlashPhase
        {
            FadeIn,
            Hold,
            FadeOut,
            Idle
        }
        public FlashPhase phase = FlashPhase.Idle;
    }
    
    private List<LineRenderer> lightRays = new List<LineRenderer>();
    private List<RayFlashState> rayStates = new List<RayFlashState>();
    private Camera mainCamera;
    private Transform playerTransform;
    private ExitDoorController exitDoorController;
    private float currentRotation = 0f;
    private bool isActive = false;
    private float nextFlashTime = 0f;
    private float currentDynamicEndWidth = 0.8f;

    void Start()
    {
        mainCamera = Camera.main;
        
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("DoorLightIndicator: Player를 찾을 수 없어 카메라 위치를 사용합니다.");
        }
        
        // 문 Transform 자동 할당
        if (doorTransform == null)
        {
            doorTransform = transform;
        }
        
        // ExitDoorController 찾기
        exitDoorController = GetComponent<ExitDoorController>();
        if (exitDoorController == null)
        {
            Debug.LogError("DoorLightIndicator: ExitDoorController를 찾을 수 없습니다!");
            return;
        }
        
        // ExitDoorController의 조건 만족 이벤트에 구독
        exitDoorController.onGeneratorComplete.AddListener(OnGeneratorComplete);
        
        CreateLightRays();
        SetAllVisible(false);
    }

    void CreateLightRays()
    {
        // 기본 빛줄기 + 추가 플래시용 빛줄기
        int totalRayCount = basicRayCount + extraFlashRayCount;
        
        for (int i = 0; i < totalRayCount; i++)
        {
            GameObject rayObj = new GameObject($"LightRay_{i}");
            rayObj.transform.SetParent(transform);
            
            LineRenderer lineRenderer = rayObj.AddComponent<LineRenderer>();
            
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = lightColor;
            lineRenderer.material = mat;
            
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.3f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 99;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            
            // 2D 게임이므로 Z축을 기준으로 정렬
            lineRenderer.alignment = LineAlignment.TransformZ;
            
            // 끝부분을 둥글게 처리
            lineRenderer.numCapVertices = 10;
            lineRenderer.numCornerVertices = 10;
            
            // 텍스처 모드를 Stretch로 설정
            lineRenderer.textureMode = LineTextureMode.Stretch;
            
            lightRays.Add(lineRenderer);
            rayStates.Add(new RayFlashState());
        }
    }

    void Update()
    {
        if (!isActive || doorTransform == null || mainCamera == null)
        {
            return;
        }

        // 플레이어 위치 가져오기 (플레이어가 없으면 카메라 사용)
        Vector3 playerPosition = playerTransform != null ? playerTransform.position : mainCamera.transform.position;
        
        // 플레이어와 문 사이의 거리 계산
        float distanceToPlayer = Vector3.Distance(doorTransform.position, playerPosition);
        
        // 거리에 따라 동적 굵기 계산
        UpdateDynamicWidth(distanceToPlayer);

        // 문이 화면에 있는지 확인
        Vector3 doorViewportPos = mainCamera.WorldToViewportPoint(doorTransform.position);
        bool isDoorVisible = doorViewportPos.z > 0 && 
                            doorViewportPos.x >= 0 && doorViewportPos.x <= 1 && 
                            doorViewportPos.y >= 0 && doorViewportPos.y <= 1;

        // 문이 이미 열렸는지 확인
        bool isDoorOpen = exitDoorController != null && exitDoorController.IsOpen;

        // 문이 화면에 보이고 아직 열리지 않았다면 빛 끄기
        if (isDoorVisible && !isDoorOpen)
        {
            SetAllVisible(false);
            return;
        }

        // 빛 효과 활성화
        UpdateBasicRotatingRays();
        UpdateExtraFlashRays();
        AnimateBasicRays();
    }

    void UpdateDynamicWidth(float distance)
    {
        // 거리에 따라 굵기 계산
        float t = Mathf.InverseLerp(minDistanceForWidth, maxDistanceForWidth, distance);
        currentDynamicEndWidth = Mathf.Lerp(minEndWidth, maxEndWidth, t);
    }

    void UpdateBasicRotatingRays()
    {
        Vector3 doorPosition = doorTransform.position;
        currentRotation += rotationSpeed * Time.deltaTime;
        
        // 기본 빛줄기들 회전
        for (int i = 0; i < basicRayCount; i++)
        {
            if (i >= lightRays.Count) break;
            
            LineRenderer ray = lightRays[i];
            ray.enabled = true;
            
            // 360도를 균등하게 분배
            float angle = (360f / basicRayCount) * i + currentRotation;
            float rad = angle * Mathf.Deg2Rad;
            
            Vector3 direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            
            Vector3 startPos = doorPosition;
            Vector3 endPos = startPos + direction * basicRayLength;
            
            ray.SetPosition(0, startPos);
            ray.SetPosition(1, endPos);
            
            // 동적 굵기 적용
            ray.startWidth = basicStartWidth;
            ray.endWidth = currentDynamicEndWidth;
        }
    }

    void UpdateExtraFlashRays()
    {
        Vector3 doorPosition = doorTransform.position;
        
        // 새로운 플래시를 시작할 시간인지 확인
        if (Time.time >= nextFlashTime)
        {
            int flashCount = Random.Range(minFlashCount, maxFlashCount + 1);
            
            // 추가 빛줄기 인덱스 범위
            int extraStartIndex = basicRayCount;
            int extraEndIndex = basicRayCount + extraFlashRayCount;
            
            // 사용 가능한 추가 빛줄기 중에서 무작위로 선택
            List<int> availableIndices = new List<int>();
            for (int i = extraStartIndex; i < extraEndIndex && i < lightRays.Count; i++)
            {
                if (!rayStates[i].isFlashing)
                {
                    availableIndices.Add(i);
                }
            }
            
            // 선택된 개수만큼 플래시 시작
            for (int i = 0; i < Mathf.Min(flashCount, availableIndices.Count); i++)
            {
                int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
                availableIndices.Remove(randomIndex);
                
                StartFlash(randomIndex);
            }
            
            // 다음 플래시 시간 설정
            nextFlashTime = Time.time + Random.Range(minFlashInterval, maxFlashInterval);
        }
        
        // 추가 빛줄기들의 플래시 상태 업데이트
        int extraStart = basicRayCount;
        int extraEnd = basicRayCount + extraFlashRayCount;
        
        for (int i = extraStart; i < extraEnd && i < lightRays.Count; i++)
        {
            RayFlashState state = rayStates[i];
            LineRenderer ray = lightRays[i];
            
            if (!state.isFlashing)
            {
                ray.enabled = false;
                continue;
            }
            
            ray.enabled = true;
            
            // 플래시 타이머 업데이트
            state.flashTimer += Time.deltaTime;
            
            // 페이즈에 따라 알파값 계산
            switch (state.phase)
            {
                case RayFlashState.FlashPhase.FadeIn:
                    state.currentAlpha = Mathf.Clamp01(state.flashTimer / fadeInDuration);
                    if (state.flashTimer >= fadeInDuration)
                    {
                        state.phase = RayFlashState.FlashPhase.Hold;
                        state.flashTimer = 0f;
                    }
                    break;
                    
                case RayFlashState.FlashPhase.Hold:
                    state.currentAlpha = 1f;
                    if (state.flashTimer >= flashDuration)
                    {
                        state.phase = RayFlashState.FlashPhase.FadeOut;
                        state.flashTimer = 0f;
                    }
                    break;
                    
                case RayFlashState.FlashPhase.FadeOut:
                    state.currentAlpha = 1f - Mathf.Clamp01(state.flashTimer / fadeOutDuration);
                    if (state.flashTimer >= fadeOutDuration)
                    {
                        state.phase = RayFlashState.FlashPhase.Idle;
                        state.isFlashing = false;
                        state.currentAlpha = 0f;
                    }
                    break;
            }
            
            // 빛줄기 위치 설정 (더 긴 길이 사용)
            float rad = state.targetAngle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            
            Vector3 startPos = doorPosition;
            Vector3 endPos = startPos + direction * extraRayLength;
            
            ray.SetPosition(0, startPos);
            ray.SetPosition(1, endPos);
            
            // 동적 굵기 적용 (추가 빛줄기는 조금 더 굵게)
            ray.startWidth = basicStartWidth;
            ray.endWidth = currentDynamicEndWidth * 1.2f;
            
            // 알파값 적용
            Color currentColor = lightColor;
            currentColor.a = lightColor.a * state.currentAlpha;
            ray.material.color = currentColor;
        }
    }

    void AnimateBasicRays()
    {
        float pulse = Mathf.Lerp(minIntensity, maxIntensity, 
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        
        // 기본 빛줄기들만 애니메이션
        for (int i = 0; i < basicRayCount && i < lightRays.Count; i++)
        {
            if (!lightRays[i].enabled) continue;
            
            LineRenderer ray = lightRays[i];
            
            // Perlin Noise로 자연스러운 깜빡임
            float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed + i * 10f, i);
            float individualPulse = Mathf.Lerp(minIntensity, maxIntensity, flicker);
            
            Color currentColor = lightColor;
            currentColor.a = lightColor.a * individualPulse * pulse;
            ray.material.color = currentColor;
        }
    }

    void StartFlash(int rayIndex)
    {
        RayFlashState state = rayStates[rayIndex];
        state.isFlashing = true;
        state.currentAlpha = 0f;
        state.flashTimer = 0f;
        state.phase = RayFlashState.FlashPhase.FadeIn;
        
        // 무작위 각도 설정
        state.targetAngle = Random.Range(0f, 360f);
    }

    void SetAllVisible(bool visible)
    {
        foreach (var ray in lightRays)
        {
            ray.enabled = visible;
        }
    }

    /// <summary>
    /// ExitDoorController의 발전기 완료 이벤트 핸들러
    /// </summary>
    void OnGeneratorComplete(GeneratorCompleteInfo info)
    {
        if (info.allConditionsMet)
        {
            // 모든 조건이 만족되면 빛 활성화
            ActivateLight();
            Debug.Log("DoorLightIndicator: 모든 조건 만족! 문에서 빛이 발산됩니다.");
        }
    }

    /// <summary>
    /// 빛 이펙트 활성화
    /// </summary>
    public void ActivateLight()
    {
        isActive = true;
        nextFlashTime = Time.time + Random.Range(minFlashInterval, maxFlashInterval);
    }

    /// <summary>
    /// 빛 이펙트 비활성화
    /// </summary>
    public void DeactivateLight()
    {
        isActive = false;
        SetAllVisible(false);
    }

    void OnDestroy()
    {
        foreach (var ray in lightRays)
        {
            if (ray != null && ray.material != null)
            {
                Destroy(ray.material);
            }
        }
        
        // 이벤트 구독 해제
        if (exitDoorController != null)
        {
            exitDoorController.onGeneratorComplete.RemoveListener(OnGeneratorComplete);
        }
    }

    void OnDrawGizmos()
    {
        if (doorTransform != null && isActive)
        {
            // 문 위치 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(doorTransform.position, 0.5f);
            
            // 거리 범위 시각화
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(doorTransform.position, minDistanceForWidth);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(doorTransform.position, maxDistanceForWidth);
            
            #if UNITY_EDITOR
            Transform targetTransform = playerTransform != null ? playerTransform : (mainCamera != null ? mainCamera.transform : null);
            if (targetTransform != null)
            {
                float distance = Vector3.Distance(doorTransform.position, targetTransform.position);
                UnityEditor.Handles.Label(doorTransform.position + Vector3.up * 2, 
                    $"Door Light Active\nDistance: {distance:F1}\nCurrent Width: {currentDynamicEndWidth:F2}");
            }
            #endif
        }
    }
}