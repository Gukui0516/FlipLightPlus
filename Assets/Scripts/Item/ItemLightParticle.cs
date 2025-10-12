using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem), typeof(ParticleSystemRenderer))]
public class ItemLightParticles : MonoBehaviour, IInvertibleColor
{
    [Header("Visual")]
    [SerializeField] private Color color = Color.white;
    [SerializeField] private Color invertedColor = Color.black;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = 0;
    [Tooltip("파티클에 사용할 머티리얼(흰 사각형 텍스처 권장). 비우면 기본 파티클 머티리얼.")]
    [SerializeField] private Material particleMaterial;

    [Header("Timing")]
    //[SerializeField, Min(0.001f)] private float intervalSeconds = 0.6f;
    [SerializeField, Min(0f)] private float growDuration = 0.15f;
    [SerializeField, Min(0f)] private float holdDuration = 0.05f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    [Header("Travel")]
    [SerializeField, Min(0f)] private float travelDuration = 0f;
    [SerializeField] private float travelSpeed = 0f;

    [Header("Shape")]
    [SerializeField] private Vector2 lengthRange = new Vector2(1f, 2f);
    [SerializeField] private float thickness = 0.01f;

    [SerializeField, Range(0f, 0.5f)]
    private float endScaleJitter = 0.25f;
    
    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    
    [SerializeField] private int targetConcurrent = 240;

    // ✅ 핵심 수정: Curve를 미리 생성하고 재사용하되, 직접 수정 대신 새로 생성
    private AnimationCurve cachedGrowHoldCurve;
    private AnimationCurve cachedYMinCurve;
    private AnimationCurve cachedYMaxCurve;
    private Gradient cachedColorGradient;
    
    private float lastGrowDuration = -1f;
    private float lastHoldDuration = -1f;
    private float lastFadeDuration = -1f;
    private float lastJitter = -1f;
    private Color lastColor = Color.clear;
    private Color currentColor; // 현재 사용 중인 색상
    
    private bool isSettingUp;
    private float lastSetupTime;
    private const float MIN_SETUP_INTERVAL = 0.5f; // Setup 호출 최소 간격

#if UNITY_EDITOR
    private float lastValidateTime;
#endif

    void Reset()
    {
        CreateInitialCurves();
    }

    void OnValidate()
    {
        if (lengthRange.x < 0) lengthRange.x = 0;
        if (lengthRange.y < lengthRange.x) lengthRange.y = lengthRange.x;
        if (thickness < 0.001f) thickness = 0.001f;
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastValidateTime > 1f)
            {
                lastValidateTime = currentTime;
            }
        }
#endif
    }

    void Awake()
    {
        currentColor = color; // 초기값은 원본 색상
        CreateInitialCurves();
        ps = GetComponent<ParticleSystem>();
        psr = GetComponent<ParticleSystemRenderer>();
    }

    void Start()
    {
        SafeSetup();
    }

    void OnEnable()
    {
        // 활성화 시에만 설정, 너무 자주 호출 방지
        if (Application.isPlaying && Time.time - lastSetupTime > MIN_SETUP_INTERVAL)
        {
            SafeSetup();
        }
    }

    public void OnCollected()
    {
        if (!ps) return;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
    }

    #region IInvertibleColor Implementation

    public void SetInvertedColor()
    {
        currentColor = invertedColor;
        SafeSetup(); // 파티클 시스템 재설정으로 색상 적용
    }

    public void SetOriginalColor()
    {
        currentColor = color;
        SafeSetup(); // 파티클 시스템 재설정으로 색상 적용
    }

    #endregion

    // ✅ Curve 생성 - 직접 keys 배열 수정 대신 생성자 사용
    private void CreateInitialCurves()
    {
        if (cachedGrowHoldCurve == null)
        {
            cachedGrowHoldCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        if (cachedYMinCurve == null)
        {
            cachedYMinCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        if (cachedYMaxCurve == null)
        {
            cachedYMaxCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        if (cachedColorGradient == null)
        {
            cachedColorGradient = new Gradient();
        }
    }

    private void SafeSetup()
    {
        // 이미 설정 중이거나, 너무 최근에 설정했으면 스킵
        if (isSettingUp || Time.time - lastSetupTime < MIN_SETUP_INTERVAL)
        {
            return;
        }

        if (!ps) ps = GetComponent<ParticleSystem>();
        if (!psr) psr = GetComponent<ParticleSystemRenderer>();
        if (!ps || !psr) return;

        bool needsUpdate = 
            lastGrowDuration != growDuration ||
            lastHoldDuration != holdDuration ||
            lastFadeDuration != fadeDuration ||
            lastJitter != endScaleJitter ||
            lastColor != currentColor;

        if (!needsUpdate && Application.isPlaying)
        {
            return;
        }

        lastGrowDuration = growDuration;
        lastHoldDuration = holdDuration;
        lastFadeDuration = fadeDuration;
        lastJitter = endScaleJitter;
        lastColor = currentColor;
        lastSetupTime = Time.time;

        bool wasPlaying = Application.isPlaying && ps.isPlaying;
        
        if (wasPlaying)
        {
            isSettingUp = true;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            StartCoroutine(SetupAfterDelay());
        }
        else
        {
            ApplyParticleSystemSettings();
        }
    }

    // ✅ 충분한 대기 시간 확보 (최소 2프레임)
    private System.Collections.IEnumerator SetupAfterDelay()
    {
        yield return null; // 1프레임 대기
        yield return null; // 2프레임 대기 (Job 완료 보장)
        
        ApplyParticleSystemSettings();
        
        if (gameObject.activeInHierarchy && ps)
        {
            ps.Clear();
            ps.Play();
        }
        
        isSettingUp = false;
    }

    private void ApplyParticleSystemSettings()
    {
        if (!ps || !psr) return;

        float life = Mathf.Max(0.1f, growDuration + holdDuration + fadeDuration);

        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var sizeOverLifetime = ps.sizeOverLifetime;
        var colorOverLifetime = ps.colorOverLifetime;
        var rotationOverLifetime = ps.rotationOverLifetime;
        var velocityOverLifetime = ps.velocityOverLifetime;

        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = 0f;
        main.maxParticles = Mathf.Max(targetConcurrent * 2, 100);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = true;
        main.prewarm = false;

        //main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0f;
        shape.arc = 360f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        shape.radiusThickness = 1f;

        rotationOverLifetime.enabled = false;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);

        float emissionRate = (life > 0.01f) ? (float)targetConcurrent / life : 10f;
        emission.enabled = true;
        emission.rateOverTime = Mathf.Clamp(emissionRate, 0.1f, 1000f);

        main.startSize3D = true;
        main.startSizeX = Mathf.Max(thickness, 0.001f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(
            Mathf.Max(lengthRange.x, 0.001f), 
            Mathf.Max(lengthRange.y, 0.001f)
        );
        main.startSizeZ = 1f;

        // ✅ 핵심 수정: Curve를 새로 생성하여 할당 (keys 배열 직접 수정 X)
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.separateAxes = true;

        float g = Mathf.Clamp01(growDuration / life);
        float h = Mathf.Clamp01(holdDuration / life);
        
        // 새 Curve 생성 (기존 것을 수정하지 않음)
        cachedGrowHoldCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(Mathf.Max(0.0001f, g), 1f),
            new Keyframe(Mathf.Max(0.0001f, g + h), 1f),
            new Keyframe(1f, 1f)
        );

        float j = Mathf.Clamp01(endScaleJitter);
        cachedYMinCurve = CreateScaledCurve(cachedGrowHoldCurve, 1f - j);
        cachedYMaxCurve = CreateScaledCurve(cachedGrowHoldCurve, 1f + j);

        sizeOverLifetime.x = new ParticleSystem.MinMaxCurve(1f);
        sizeOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, cachedYMinCurve, cachedYMaxCurve);
        sizeOverLifetime.z = new ParticleSystem.MinMaxCurve(1f);

        // Color Over Lifetime
        colorOverLifetime.enabled = true;
        float fadeStart = Mathf.Clamp01((growDuration + holdDuration) / life);
        
        // Gradient도 새로 생성
        cachedColorGradient = new Gradient();
        cachedColorGradient.SetKeys(
            new[] { new GradientColorKey(currentColor, 0f), new GradientColorKey(currentColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(cachedColorGradient);

        // 이동
        velocityOverLifetime.enabled = (travelDuration > 0f && travelSpeed > 0f);
        if (velocityOverLifetime.enabled)
        {
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = 0f;
            velocityOverLifetime.y = travelSpeed;
            velocityOverLifetime.z = 0f;
        }

        // 렌더러
        psr.sortingLayerName = sortingLayerName;
        psr.sortingOrder = orderInLayer;
        psr.alignment = ParticleSystemRenderSpace.View;
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.normalDirection = 1f;
        psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psr.receiveShadows = false;
        if (particleMaterial) psr.material = particleMaterial;
    }

    // ✅ 새 Curve 생성 (기존 것을 수정하지 않음)
    private AnimationCurve CreateScaledCurve(AnimationCurve source, float factor)
    {
        var sourceKeys = source.keys;
        Keyframe[] newKeys = new Keyframe[sourceKeys.Length];
        
        for (int i = 0; i < sourceKeys.Length; i++)
        {
            var key = sourceKeys[i];
            newKeys[i] = new Keyframe(
                key.time,
                key.value * factor,
                key.inTangent * factor,
                key.outTangent * factor
            );
        }
        
        return new AnimationCurve(newKeys);
    }
}