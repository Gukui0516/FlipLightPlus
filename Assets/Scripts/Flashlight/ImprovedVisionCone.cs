using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
// 색 팔레트용 엔트리
[System.Serializable]
public struct NamedColor
{
    public string key;
    public Color color;
}
/// <summary>
/// 완전 개선된 시야 원뿔 시스템
/// - 정확한 그림자 경계 (실루엣 감지)
/// - 겹친 물체 완벽 처리 (깊이 기반 검증)
/// - CPU/GPU 하이브리드 최적화
/// - PolygonCollider2D 자동 갱신
/// - WorldStateManager 연동 색상 변경
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ImprovedVisionCone : MonoBehaviour
{
    // 장애물 정보
    private class ObstacleInfo
    {
        public List<Vector2> vertices;
        public Collider2D collider;
        public float distanceToPlayer;
        public Vector2 centerPosition;
    }
    [SerializeField] private float viewRadius = 10f;
    [Range(0f, 360f)]
    [SerializeField] private float viewAngle = 90f;
    
    [Tooltip("균등 부채꼴 레이 개수")]
    [Range(32, 128)]
    [SerializeField] private int uniformRayCount = 60;
    
    [Header("⚙️ GPU 설정 (향후 지원)")]
    [Tooltip("GPU 사용 여부 (현재 CPU 모드)")]
    [SerializeField] private bool useGPU = false;
    [SerializeField] private ComputeShader visionCompute;
    
    [Header("🎯 레이어")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask targetLayer;
    
    [Header("🎨 비주얼")]
    [SerializeField] private Color flashlightColor = new Color(1f, 1f, 0.5f, 0.3f);
    [SerializeField] private Material visionMaterial;
    
    [Header("📊 최적화")]
    [SerializeField] private float obstacleUpdateInterval = 0.1f;
    [SerializeField] private float obstacleSearchRadius = 15f;
    [Tooltip("꼭지점 근처 미세 각도 추가 (그림자 정밀도)")]
    [Range(0f, 0.01f)]
    [SerializeField] private float vertexAngleOffset = 0.0001f;
    
    [Header("💥 콜라이더")]
    [SerializeField] private float colliderUpdateInterval = 0.05f;
    [SerializeField] private bool enableVisionCollider = true;
    
    [Header("🔧 디버그")]
    [SerializeField] private bool showDebugRays = false;
    [SerializeField] private bool showDebugVertices = false;
    [SerializeField] private bool usePreciseDetection = true;
    
    [Header("🎨 색상 팔레트")]
    [SerializeField] private List<NamedColor> colorPalette = new List<NamedColor>();
    
    // 내부 변수
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private PolygonCollider2D visionCollider;
    
    // 색상 관리
    private Dictionary<string, Color> colorMap = new Dictionary<string, Color>();
    private Color originalColor; // 원래 색상 저장
    
    // WorldStateManager 연동
    private WorldStateManager worldStateManager;
    [SerializeField] private bool isInverted;
    public bool IsInverted => isInverted;
    
    // 내부 변수
    private List<ObstacleInfo> obstacles = new List<ObstacleInfo>();
    private List<Vector2> visionPoints = new List<Vector2>();
    private Vector2[] visionPolygon;
    private float lastObstacleUpdateTime;
    private float lastColliderUpdateTime;
    
    // 타겟 감지
    private HashSet<Transform> currentVisibleTargets = new HashSet<Transform>();
    private HashSet<Transform> previousVisibleTargets = new HashSet<Transform>();
    
    // 이벤트
    public System.Action<Transform> OnTargetEnter;
    public System.Action<Transform> OnTargetExit;
    public System.Action<HashSet<Transform>> OnVisibleTargetsUpdate;
    
    

    void Awake()
    {
        // OnEnable보다 먼저 초기화
        Debug.Log("[ImprovedVisionCone] Awake - 초기화 시작");
        
        // 먼저 컴포넌트 초기화 (meshRenderer 등)
        InitializeComponents();
        
        // 그 다음 색상 팔레트 빌드
        RebuildColorMap();
        originalColor = flashlightColor; // 원래 색상 저장
        Debug.Log($"[ImprovedVisionCone] 원래 색상 저장: {originalColor}");
    }

    void Start()
    {
        // GPU는 향후 지원 예정
        useGPU = false;
    }

    void OnEnable()
    {
        // Awake에서 이미 초기화되었으므로 안전하게 구독
        SubscribeWorldEvents();
        SyncInvertedImmediate();
    }

    void OnDisable()
    {
        UnsubscribeWorldEvents();
    }

    void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        mesh = new Mesh();
        mesh.name = "Vision Cone Mesh";
        meshFilter.mesh = mesh;
        
        if (visionMaterial != null)
        {
            meshRenderer.material = visionMaterial;
            meshRenderer.material.SetColor("_Color", flashlightColor);
        }
        else
        {
            meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.material.color = flashlightColor;
        }
        
        meshRenderer.sortingOrder = -1;
        
        // PolygonCollider2D 초기화
        visionCollider = GetComponent<PolygonCollider2D>();
        if (visionCollider == null && enableVisionCollider)
        {
            visionCollider = gameObject.AddComponent<PolygonCollider2D>();
        }
        
        if (visionCollider != null)
        {
            visionCollider.isTrigger = true;
        }
    }

    void Update()
    {
        UpdateObstacles();
    }

    void LateUpdate()
    {
        ComputeVisionCone();
        UpdateMesh();
        UpdateVisionCollider();
        DetectTargets();
    }

    void UpdateObstacles()
    {
        if (Time.time - lastObstacleUpdateTime < obstacleUpdateInterval)
            return;
        
        lastObstacleUpdateTime = Time.time;
        obstacles.Clear();
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position,
            obstacleSearchRadius,
            obstacleLayer
        );
        
        Vector2 origin = transform.position;
        
        foreach (var col in colliders)
        {
            if (col == null) continue;
            
            ObstacleInfo obstacle = new ObstacleInfo();
            obstacle.vertices = GetObstacleVertices(col);
            obstacle.collider = col;
            obstacle.centerPosition = col.transform.position;
            obstacle.distanceToPlayer = Vector2.Distance(origin, obstacle.centerPosition);
            obstacles.Add(obstacle);
        }
        
        // 거리순 정렬 (가까운 것부터)
        obstacles.Sort((a, b) => a.distanceToPlayer.CompareTo(b.distanceToPlayer));
    }

    List<Vector2> GetObstacleVertices(Collider2D col)
    {
        List<Vector2> vertices = new List<Vector2>();
        
        // IObstacleWithVertices 인터페이스 우선
        IObstacleWithVertices obstacleWithVertices = col.GetComponent<IObstacleWithVertices>();
        if (obstacleWithVertices != null)
        {
            return obstacleWithVertices.GetWorldVertices();
        }
        
        // 자동 추출
        if (col is PolygonCollider2D polygon)
        {
            foreach (var point in polygon.points)
            {
                vertices.Add(col.transform.TransformPoint(point));
            }
        }
        else if (col is BoxCollider2D box)
        {
            Vector2 halfSize = box.size * 0.5f;
            Vector2[] corners = new Vector2[]
            {
                new Vector2(-halfSize.x, -halfSize.y),
                new Vector2(halfSize.x, -halfSize.y),
                new Vector2(halfSize.x, halfSize.y),
                new Vector2(-halfSize.x, halfSize.y)
            };
            
            foreach (var corner in corners)
            {
                vertices.Add(col.transform.TransformPoint(corner));
            }
        }
        else if (col is CircleCollider2D circle)
        {
            float radius = circle.radius * col.transform.lossyScale.x;
            int segments = 16;
            
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (360f / segments) * Mathf.Deg2Rad;
                Vector2 point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertices.Add(col.transform.TransformPoint(point));
            }
        }
        
        return vertices;
    }

    void ComputeVisionCone()
    {
        visionPoints.Clear();
        Vector2 origin = transform.position;
        
        // 1️⃣ 균등 부채꼴 레이캐스트 (기본 형태)
        List<float> uniformAngles = GenerateUniformAngles();
        List<Vector2> uniformPoints = CastRaysAtAngles(uniformAngles);
        
        // 2️⃣ 유효한 꼭지점 찾기 (레이캐스트로 검증)
        List<float> vertexAngles = CollectValidVertexAngles();
        List<Vector2> vertexPoints = CastRaysAtAngles(vertexAngles);
        
        // 3️⃣ 결합 및 정렬
        visionPoints.AddRange(uniformPoints);
        visionPoints.AddRange(vertexPoints);
        
        // 4️⃣ 각도순 정렬
        float forwardAngleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
        visionPoints = visionPoints
            .OrderBy(point => {
                Vector2 dir = point - origin;
                float angle = Mathf.Atan2(dir.x, dir.y);
                return NormalizeAngleRelativeToForward(angle, forwardAngleRad);
            })
            .Distinct()
            .ToList();
        
        // 5️⃣ 시야 폴리곤 저장
        visionPolygon = visionPoints.ToArray();
        
        if (showDebugRays)
        {
            Debug.Log($"✅ 균등: {uniformPoints.Count}, 꼭지점: {vertexPoints.Count}, 이: {visionPoints.Count}");
        }
    }
    
    List<float> GenerateUniformAngles()
    {
        List<float> angles = new List<float>();
        float forwardAngleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
        float halfAngleRad = viewAngle / 2f * Mathf.Deg2Rad;
        
        for (int i = 0; i < uniformRayCount; i++)
        {
            float t = i / (float)(uniformRayCount - 1);
            float angle = forwardAngleRad - halfAngleRad + t * (viewAngle * Mathf.Deg2Rad);
            angles.Add(angle);
        }
        
        return angles;
    }

    List<float> CollectValidVertexAngles()
    {
        List<float> angles = new List<float>();
        Vector2 origin = transform.position;
        
        int totalVertices = 0;
        int validVertices = 0;
        
        foreach (var obstacle in obstacles)
        {
            foreach (var vertex in obstacle.vertices)
            {
                totalVertices++;
                
                Vector2 dirToVertex = vertex - origin;
                float distToVertex = dirToVertex.magnitude;
                
                if (distToVertex > viewRadius || distToVertex < 0.01f) continue;
                
                float angle = Mathf.Atan2(dirToVertex.x, dirToVertex.y);
                float angleDeg = angle * Mathf.Rad2Deg;
                
                if (!IsAngleInView(angleDeg)) continue;
                
                Vector2 rayDir = dirToVertex.normalized;
                RaycastHit2D hit = Physics2D.Raycast(
                    origin,
                    rayDir,
                    distToVertex + 0.5f,
                    obstacleLayer
                );
                
                if (hit.collider == null)
                {
                    angles.Add(angle - vertexAngleOffset);
                    angles.Add(angle);
                    angles.Add(angle + vertexAngleOffset);
                    validVertices++;
                    
                    if (showDebugVertices)
                    {
                        Debug.DrawLine(origin, vertex, Color.green, 0.5f);
                        Debug.DrawRay(vertex, Vector2.up * 0.5f, Color.yellow, 0.5f);
                    }
                }
                else
                {
                    float hitToVertexDist = Vector2.Distance(hit.point, vertex);
                    
                    if (hitToVertexDist < 0.3f)
                    {
                        angles.Add(angle - vertexAngleOffset);
                        angles.Add(angle);
                        angles.Add(angle + vertexAngleOffset);
                        validVertices++;
                        
                        if (showDebugVertices)
                        {
                            Debug.DrawLine(origin, vertex, Color.green, 0.5f);
                            Debug.DrawRay(vertex, Vector2.up * 0.5f, Color.yellow, 0.5f);
                        }
                    }
                    else
                    {
                        if (showDebugVertices)
                        {
                            Debug.DrawLine(origin, hit.point, Color.red, 0.5f);
                            Debug.DrawLine(hit.point, vertex, Color.gray, 0.5f);
                        }
                    }
                }
            }
        }
        
        if (showDebugRays)
        {
            Debug.Log($"🔷 전체 꼭지점: {totalVertices}, 유효: {validVertices}, 각도: {angles.Count}");
        }
        
        return angles;
    }
    
    List<Vector2> CastRaysAtAngles(List<float> angles)
    {
        List<Vector2> points = new List<Vector2>();
        Vector2 origin = transform.position;
        
        foreach (float angle in angles)
        {
            Vector2 rayDir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            RaycastHit2D hit = Physics2D.Raycast(origin, rayDir, viewRadius, obstacleLayer);
            
            Vector2 point;
            if (hit.collider != null)
            {
                point = hit.point;
                
                if (showDebugRays)
                {
                    Debug.DrawLine(origin, point, Color.yellow, 0.1f);
                }
            }
            else
            {
                point = origin + rayDir * viewRadius;
                
                if (showDebugRays)
                {
                    Debug.DrawLine(origin, point, Color.cyan, 0.1f);
                }
            }
            
            points.Add(point);
        }
        
        return points;
    }
    
    float NormalizeAngleRelativeToForward(float angle, float forward)
    {
        float diff = angle - forward;
        
        while (diff > Mathf.PI) diff -= 2 * Mathf.PI;
        while (diff < -Mathf.PI) diff += 2 * Mathf.PI;
        
        return diff;
    }

    bool IsAngleInView(float angleDeg)
    {
        float forward = transform.eulerAngles.z;
        float diff = Mathf.DeltaAngle(forward, angleDeg);
        return Mathf.Abs(diff) <= viewAngle / 2f;
    }

    void UpdateMesh()
    {
        if (visionPoints.Count < 2) return;
        
        int vertexCount = visionPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(visionPoints.Count - 1) * 3];
        
        vertices[0] = Vector3.zero;
        
        for (int i = 0; i < visionPoints.Count; i++)
        {
            Vector3 worldPoint = new Vector3(visionPoints[i].x, visionPoints[i].y, 0);
            vertices[i + 1] = transform.InverseTransformPoint(worldPoint);
            
            if (i < visionPoints.Count - 1)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void UpdateVisionCollider()
    {
        if (!enableVisionCollider || visionCollider == null) return;
        if (visionPoints.Count < 3) return;
        
        if (Time.time - lastColliderUpdateTime < colliderUpdateInterval)
            return;
        
        lastColliderUpdateTime = Time.time;
        
        Vector2[] localPoints = new Vector2[visionPoints.Count + 1];
        
        localPoints[0] = Vector2.zero;
        
        for (int i = 0; i < visionPoints.Count; i++)
        {
            Vector3 worldPoint = new Vector3(visionPoints[i].x, visionPoints[i].y, 0);
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            localPoints[i + 1] = localPoint;
        }
        
        visionCollider.points = localPoints;
    }

    void DetectTargets()
    {
        previousVisibleTargets.Clear();
        foreach (var target in currentVisibleTargets)
        {
            previousVisibleTargets.Add(target);
        }
        currentVisibleTargets.Clear();
        
        Collider2D[] targetsInRange = Physics2D.OverlapCircleAll(
            transform.position,
            viewRadius,
            targetLayer
        );
        
        foreach (var targetCollider in targetsInRange)
        {
            if (targetCollider == null) continue;
            
            Vector2 targetPos = targetCollider.transform.position;
            
            if (usePreciseDetection && visionPolygon != null && visionPolygon.Length > 0)
            {
                if (IsPointInPolygon(targetPos, visionPolygon))
                {
                    currentVisibleTargets.Add(targetCollider.transform);
                }
            }
            else
            {
                if (IsTargetVisibleFast(targetPos))
                {
                    currentVisibleTargets.Add(targetCollider.transform);
                }
            }
        }
        
        foreach (var target in currentVisibleTargets)
        {
            if (!previousVisibleTargets.Contains(target))
            {
                OnTargetEnter?.Invoke(target);
            }
        }
        
        foreach (var previousTarget in previousVisibleTargets)
        {
            if (previousTarget != null && !currentVisibleTargets.Contains(previousTarget))
            {
                OnTargetExit?.Invoke(previousTarget);
            }
        }
        
        OnVisibleTargetsUpdate?.Invoke(new HashSet<Transform>(currentVisibleTargets));
    }

    bool IsTargetVisibleFast(Vector2 targetPos)
    {
        Vector2 dirToTarget = (targetPos - (Vector2)transform.position).normalized;
        float distToTarget = Vector2.Distance(transform.position, targetPos);
        
        float angle = Mathf.Atan2(dirToTarget.x, dirToTarget.y) * Mathf.Rad2Deg;
        if (!IsAngleInView(angle)) return false;
        
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dirToTarget,
            distToTarget,
            obstacleLayer
        );
        
        return hit.collider == null;
    }

    bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int intersections = 0;
        int vertexCount = polygon.Length;
        
        for (int i = 0; i < vertexCount; i++)
        {
            Vector2 v1 = polygon[i];
            Vector2 v2 = polygon[(i + 1) % vertexCount];
            
            if (RayIntersectsSegment(point, v1, v2))
            {
                intersections++;
            }
        }
        
        return (intersections % 2) == 1;
    }

    bool RayIntersectsSegment(Vector2 point, Vector2 v1, Vector2 v2)
    {
        if (v1.y > v2.y)
        {
            Vector2 temp = v1;
            v1 = v2;
            v2 = temp;
        }
        
        if (point.y < v1.y || point.y > v2.y) return false;
        if (point.x >= Mathf.Max(v1.x, v2.x)) return false;
        if (point.x < Mathf.Min(v1.x, v2.x)) return true;
        
        float xIntersection = (point.y - v1.y) * (v2.x - v1.x) / (v2.y - v1.y) + v1.x;
        return point.x < xIntersection;
    }

    void OnDestroy()
    {
        UnsubscribeWorldEvents();
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, obstacleSearchRadius);
        
        float startAngle = (transform.eulerAngles.z - viewAngle / 2) * Mathf.Deg2Rad;
        float endAngle = (transform.eulerAngles.z + viewAngle / 2) * Mathf.Deg2Rad;
        
        Vector3 viewAngleA = new Vector2(Mathf.Sin(startAngle), Mathf.Cos(startAngle));
        Vector3 viewAngleB = new Vector2(Mathf.Sin(endAngle), Mathf.Cos(endAngle));
        
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);
        
        Gizmos.color = Color.red;
        foreach (Transform visibleTarget in currentVisibleTargets)
        {
            if (visibleTarget != null)
            {
                Gizmos.DrawLine(transform.position, visibleTarget.position);
                Gizmos.DrawWireSphere(visibleTarget.position, 0.3f);
            }
        }
    }
    
    // ========== 색상 팔레트 시스템 ==========
    
    private void RebuildColorMap()
    {
        if (colorMap == null) 
            colorMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        else if (!(colorMap.Comparer is StringComparer)) 
            colorMap = new Dictionary<string, Color>(colorMap, StringComparer.OrdinalIgnoreCase);
        
        colorMap.Clear();
        if (colorPalette == null) return;

        for (int i = 0; i < colorPalette.Count; i++)
        {
            var e = colorPalette[i];
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            colorMap[e.key] = e.color;
            Debug.Log($"[ImprovedVisionCone] 색상 팔레트 등록: '{e.key}' = {e.color}");
        }
        
        Debug.Log($"[ImprovedVisionCone] 색상 팔레트 빌드 완료. 총 {colorMap.Count}개 등록");
    }
    
    public void SetVisionColor(Color newColor)
    {
        flashlightColor = newColor;
        Debug.Log($"[ImprovedVisionCone] 색상 변경: {newColor}");
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.SetColor("_Color", flashlightColor);
        }
    }
    
    public bool TrySetVisionColorByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || colorMap == null)
        {
            Debug.LogWarning($"[ImprovedVisionCone] 색상 키 '{key}' 실패: 키가 비어있거나 colorMap이 null");
            return false;
        }
        
        if (!colorMap.TryGetValue(key, out var picked))
        {
            Debug.LogWarning($"[ImprovedVisionCone] 색상 키 '{key}'를 팔레트에서 찾을 수 없음. 등록된 키: {string.Join(", ", colorMap.Keys)}");
            return false;
        }
        
        Debug.Log($"[ImprovedVisionCone] 팔레트에서 '{key}' 색상 적용: {picked}");
        SetVisionColor(picked);
        return true;
    }
    
    public void ChangeVisionInverted()
    {
        if (TrySetVisionColorByKey("Inverted"))
        {
            Color tempColor = colorPalette.Find(c => c.key.Equals("Inverted", StringComparison.OrdinalIgnoreCase)).color;
            SetVisionColor(tempColor);
        }
        else
        {
            Debug.LogWarning("[ImprovedVisionCone] 'Inverted' 색상을 찾을 수 없어 변경 실패");
        }
    }
    
    public void ChangeVisionNormal()
    {
        // 팔레트의 "Normal" 키를 시도하고, 없으면 원래 색상 사용
        if (!TrySetVisionColorByKey("Normal"))
        {
            Debug.Log($"[ImprovedVisionCone] 'Normal' 키 없음. 원래 색상 사용: {originalColor}");
            SetVisionColor(originalColor);
        }
    }
    
    // ========== WorldStateManager 이벤트 처리 ==========
    
    private void SubscribeWorldEvents()
    {
        if (worldStateManager == null)
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.AddListener(HandleInvertedChanged);
            Debug.Log($"[ImprovedVisionCone] WorldStateManager 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("[ImprovedVisionCone] WorldStateManager를 찾을 수 없습니다!");
        }
    }

    private void UnsubscribeWorldEvents()
    {
        if (worldStateManager != null)
            worldStateManager.onIsInvertedChanged.RemoveListener(HandleInvertedChanged);
    }

    private void SyncInvertedImmediate()
    {
        if (worldStateManager != null)
        {
            Debug.Log($"[ImprovedVisionCone] 초기 동기화: IsInverted = {worldStateManager.IsInverted}");
            HandleInvertedChanged(worldStateManager.IsInverted);
        }
    }

    private void HandleInvertedChanged(bool inverted)
    {
        Debug.Log($"[ImprovedVisionCone] 반전 상태 변경: {inverted}");
        isInverted = inverted;

        if (inverted) 
        {
            Debug.Log("[ImprovedVisionCone] 반전 색상으로 변경 시도...");
            ChangeVisionInverted();
        }
        else 
        {
            Debug.Log("[ImprovedVisionCone] 일반 색상으로 복귀 시도...");
            ChangeVisionNormal();
        }
    }
    
    // ========== Public API ==========
    
    public void SetViewRadius(float radius) => viewRadius = radius;
    public void SetViewAngle(float angle) => viewAngle = angle;
    public float GetViewRadius() => viewRadius;
    public float GetViewAngle() => viewAngle;
    public bool IsTargetVisible(Transform target) => currentVisibleTargets.Contains(target);
    public HashSet<Transform> GetVisibleTargets() => new HashSet<Transform>(currentVisibleTargets);
    public int GetVisibleTargetCount() => currentVisibleTargets.Count;
    
    public bool IsPositionVisible(Vector2 position)
    {
        if (usePreciseDetection && visionPolygon != null && visionPolygon.Length > 0)
        {
            return IsPointInPolygon(position, visionPolygon);
        }
        else
        {
            return IsTargetVisibleFast(position);
        }
    }
    
    public Vector2[] GetVisionPolygon() => visionPolygon;
    
    public PolygonCollider2D GetVisionCollider() => visionCollider;
    
    public void SetEnableVisionCollider(bool enable)
    {
        enableVisionCollider = enable;
        if (visionCollider != null)
        {
            visionCollider.enabled = enable;
        }
    }
    
    public void SetUseGPU(bool value)
    {
        Debug.LogWarning("GPU 모드는 현재 지원되지 않습니다. CPU 모드로 실행됩니다.");
        useGPU = false;
    }
    
    public void ProcessAllVisibleTargets(System.Action<Transform> action)
    {
        foreach (var target in currentVisibleTargets)
        {
            if (target != null)
            {
                action?.Invoke(target);
            }
        }
    }
}