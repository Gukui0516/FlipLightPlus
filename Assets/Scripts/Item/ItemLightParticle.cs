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
    [Tooltip("카메라 정렬(View) 대신 로컬 정렬(Local)로 둘지 선택")]
    [SerializeField] private bool useViewAlignment = true;

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

    [SerializeField] private int targetConcurrent = 240;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private bool isInitialized;
    private Color currentColor; // 현재 사용 중인 색상

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psr = GetComponent<ParticleSystemRenderer>();
        currentColor = color; // 초기값은 원본 색상
        SanitizeParams();
    }

    void Start()
    {
        InitializeParticleSystem(force: true);
    }

    void OnEnable()
    {
        if (!isInitialized) InitializeParticleSystem(force: true);

        // 과거 프레임의 잔여 파티클이 남아 Job을 잡아두지 않도록 안전 정리
        if (ps)
        {
            ps.Clear(withChildren: true);
            ps.Play(withChildren: true);
        }
    }

    void OnDisable()
    {
        // 한 프레임 안에서 Stop → Clear까지 마무리
        if (ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(withChildren: true);
        }
    }

    void OnDestroy()
    {
        if (ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(withChildren: true);
        }
    }

    public void OnCollected()
    {
        if (!ps) return;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
        ps.Clear(withChildren: true);
    }

    #region IInvertibleColor Implementation

    public void SetInvertedColor()
    {
        currentColor = invertedColor;
        UpdateParticleColor();
    }

    public void SetOriginalColor()
    {
        currentColor = color;
        UpdateParticleColor();
    }

    /// <summary>
    /// 파티클 시스템의 색상을 currentColor로 업데이트
    /// </summary>
    private void UpdateParticleColor()
    {
        if (!ps) return;

        var colorOverLifetime = ps.colorOverLifetime;
        if (!colorOverLifetime.enabled) return;

        float life = ps.main.startLifetime.constant;
        float fadeStart = Mathf.Clamp01((growDuration + holdDuration) / life);

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(currentColor, 0f), new GradientColorKey(currentColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    #endregion

    private void InitializeParticleSystem(bool force = false)
    {
        if (!ps || !psr) return;
        if (isInitialized && !force) return;

        SanitizeParams();

        float life = Mathf.Max(0.1f, growDuration + holdDuration + fadeDuration);
        if (travelDuration > 0f) life = Mathf.Max(life, travelDuration);

        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var sizeOverLifetime = ps.sizeOverLifetime;
        var colorOverLifetime = ps.colorOverLifetime;
        var rotationOverLifetime = ps.rotationOverLifetime;
        var velocityOverLifetime = ps.velocityOverLifetime;

        // Main
        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = 0f;
        main.maxParticles = Mathf.Max(targetConcurrent * 2, 100);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = true;
        main.prewarm = false;
        main.startSize3D = true;
        main.startSizeX = Mathf.Max(thickness, 0.001f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(
            Mathf.Max(lengthRange.x, 0.001f),
            Mathf.Max(lengthRange.y, 0.001f)
        );
        main.startSizeZ = 1f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);

        // Shape
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0f;
        shape.arc = 360f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        shape.radiusThickness = 1f;

        // Emission
        float emissionRate = (life > 0.01f) ? (float)targetConcurrent / life : 10f;
        emission.enabled = true;
        emission.rateOverTime = Mathf.Clamp(emissionRate, 0.1f, 1000f);

        // Size over lifetime
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.separateAxes = true;

        float g = Mathf.Clamp01(growDuration / life);
        float h = Mathf.Clamp01(holdDuration / life);
        float j = Mathf.Clamp01(endScaleJitter);

        var growCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(Mathf.Max(0.0001f, g), 1f),
            new Keyframe(Mathf.Max(0.0001f, g + h), 1f),
            new Keyframe(1f, 1f)
        );
        var minCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(Mathf.Max(0.0001f, g), 1f - j),
            new Keyframe(Mathf.Max(0.0001f, g + h), 1f - j),
            new Keyframe(1f, 1f - j)
        );
        var maxCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(Mathf.Max(0.0001f, g), 1f + j),
            new Keyframe(Mathf.Max(0.0001f, g + h), 1f + j),
            new Keyframe(1f, 1f + j)
        );

        sizeOverLifetime.x = new ParticleSystem.MinMaxCurve(1f);
        sizeOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, minCurve, maxCurve);
        sizeOverLifetime.z = new ParticleSystem.MinMaxCurve(1f);

        // Color over lifetime - currentColor 사용
        colorOverLifetime.enabled = true;
        float fadeStart = Mathf.Clamp01((growDuration + holdDuration) / life);
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(currentColor, 0f), new GradientColorKey(currentColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Velocity (선택)
        velocityOverLifetime.enabled = (travelDuration > 0f && travelSpeed > 0f);
        if (velocityOverLifetime.enabled)
        {
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = 0f;
            velocityOverLifetime.y = travelSpeed;
            velocityOverLifetime.z = 0f;
        }

        // Renderer
        psr.sortingLayerName = sortingLayerName;
        psr.sortingOrder = orderInLayer;
        psr.alignment = useViewAlignment ? ParticleSystemRenderSpace.View : ParticleSystemRenderSpace.Local;
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.normalDirection = 1f;
        psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psr.receiveShadows = false;

        // 머티리얼은 sharedMaterial로 (인스턴스 누수 방지)
        if (particleMaterial) psr.sharedMaterial = particleMaterial;

        isInitialized = true;
    }

    // 파라미터/트랜스폼 비정상 값 방지
    private void SanitizeParams()
    {
        if (!float.IsFinite(thickness)) thickness = 0.01f;
        if (!float.IsFinite(lengthRange.x)) lengthRange.x = 1f;
        if (!float.IsFinite(lengthRange.y)) lengthRange.y = Mathf.Max(lengthRange.x + 0.01f, 2f);
        if (lengthRange.y < lengthRange.x) (lengthRange.x, lengthRange.y) = (lengthRange.y, lengthRange.x);

        if (!float.IsFinite(travelSpeed)) travelSpeed = 0f;
        if (!float.IsFinite(travelDuration) || travelDuration < 0) travelDuration = 0f;

        // 상위 트랜스폼에 NaN/Inf가 들어오면 즉시 교정
        var t = transform;
        if (!IsFinite(t.position)) t.position = Vector3.zero;
        if (!IsFinite(t.localScale)) t.localScale = Vector3.one;
        if (!IsFinite(t.eulerAngles)) t.rotation = Quaternion.identity;
    }

    private static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}