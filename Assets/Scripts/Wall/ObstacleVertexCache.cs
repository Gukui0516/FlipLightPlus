using UnityEngine;
using System.Collections.Generic;
/*
사용법
1. 이 컴포넌트를 장애물(벽) 오브젝트에 추가
2. BoxCollider2D를 추가하고 IsTrigger 해제
3. Rigidbody2D를 추가하고 Body Type을 Static으로 설정

세팅은 VertexMode Auto
커스텀 버텍스즈 0으로 비워둬도 괜찮음
CacheOnStart 체크
ShowGizmos 체크
(꼭지점 시작시 캐싱 + 꼭지점 시각화를 위한 체크)

!!!! 레이어는 Wall로 설정 !!!!
Flashlight 역시 ObstacleLayer를 Wall로 설정해줘야 함.

*/
/// <summary>
/// 장애물의 꼭지점을 미리 캐싱하는 컴포넌트
/// BoxCollider2D, PolygonCollider2D, CircleCollider2D 자동 감지
/// 복잡한 형상(ㄴ자, ㄹ자, 계단)은 커스텀 꼭지점 설정 가능
/// Start 시점에 한 번만 계산해서 성능 최적화
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObstacleVertexCache : MonoBehaviour, IObstacleWithVertices
{
    public enum VertexMode
    {
        Auto,       // Collider2D에서 자동 추출
        Custom      // 수동으로 꼭지점 설정
    }

    [Header("🔧 설정")]
    [Tooltip("Auto: Collider에서 자동 추출 | Custom: 수동 설정")]
    [SerializeField] private VertexMode vertexMode = VertexMode.Auto;
    
    [Header("📍 커스텀 꼭지점 (Custom 모드일 때만)")]
    [Tooltip("로컬 좌표계 기준 꼭지점들")]
    [SerializeField] private Vector2[] customVertices = new Vector2[0];
    
    [Header("⚙️ 옵션")]
    [Tooltip("Start 시점에 자동으로 캐싱 (false면 수동 호출 필요)")]
    [SerializeField] private bool cacheOnStart = true;
    
    [Tooltip("Gizmos로 꼭지점 시각화")]
    [SerializeField] private bool showGizmos = true;
    
    [Tooltip("Gizmos 색상")]
    [SerializeField] private Color gizmosColor = Color.green;
    
    // 캐싱된 월드 좌표 꼭지점
    private List<Vector2> cachedWorldVertices = new List<Vector2>();
    
    // 캐싱된 로컬 좌표 꼭지점 (Transform 변경 감지용)
    private List<Vector2> cachedLocalVertices = new List<Vector2>();
    
    // Transform 캐싱 (변경 감지)
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;
    
    private bool isCached = false;

    void Start()
    {
        if (cacheOnStart)
        {
            CacheVertices();
        }
    }

    void LateUpdate()
    {
        // Transform이 변경되었는지 체크
        if (HasTransformChanged())
        {
            UpdateWorldVertices();
        }
    }

    /// <summary>
    /// 꼭지점 캐싱 (Collider에서 추출 또는 Custom)
    /// </summary>
    public void CacheVertices()
    {
        cachedLocalVertices.Clear();
        
        if (vertexMode == VertexMode.Auto)
        {
            ExtractVerticesFromCollider();
        }
        else
        {
            cachedLocalVertices.AddRange(customVertices);
        }
        
        UpdateWorldVertices();
        isCached = true;
        
        Debug.Log($"✅ {gameObject.name}: {cachedLocalVertices.Count}개 꼭지점 캐싱 완료");
    }

    void ExtractVerticesFromCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        
        if (col == null)
        {
            Debug.LogError($"❌ {gameObject.name}: Collider2D가 없습니다!");
            return;
        }
        
        if (col is PolygonCollider2D polygon)
        {
            // PolygonCollider2D: 이미 꼭지점 정보 있음
            foreach (var point in polygon.points)
            {
                cachedLocalVertices.Add(point);
            }
            Debug.Log($"🔷 PolygonCollider2D 감지: {polygon.points.Length}개 꼭지점");
        }
        else if (col is BoxCollider2D box)
        {
            // BoxCollider2D: 4개 꼭지점 생성
            Vector2 halfSize = box.size * 0.5f;
            Vector2 offset = box.offset;
            
            cachedLocalVertices.Add(offset + new Vector2(-halfSize.x, -halfSize.y)); // 좌하
            cachedLocalVertices.Add(offset + new Vector2(halfSize.x, -halfSize.y));  // 우하
            cachedLocalVertices.Add(offset + new Vector2(halfSize.x, halfSize.y));   // 우상
            cachedLocalVertices.Add(offset + new Vector2(-halfSize.x, halfSize.y));  // 좌상
            
            Debug.Log($"⬛ BoxCollider2D 감지: 4개 꼭지점 생성");
        }
        else if (col is CircleCollider2D circle)
        {
            // CircleCollider2D: 8개 방향 샘플링 (근사)
            float radius = circle.radius;
            Vector2 offset = circle.offset;
            int sampleCount = 8;
            
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = i * (360f / sampleCount) * Mathf.Deg2Rad;
                Vector2 point = offset + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
                cachedLocalVertices.Add(point);
            }
            
            Debug.Log($"⭕ CircleCollider2D 감지: {sampleCount}개 샘플 포인트 생성");
        }
        else
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: 지원하지 않는 Collider 타입입니다. Custom 모드를 사용하세요.");
        }
    }

    void UpdateWorldVertices()
    {
        cachedWorldVertices.Clear();
        
        foreach (var localVertex in cachedLocalVertices)
        {
            Vector2 worldVertex = transform.TransformPoint(localVertex);
            cachedWorldVertices.Add(worldVertex);
        }
        
        // Transform 상태 저장
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.lossyScale;
    }

    bool HasTransformChanged()
    {
        return transform.position != lastPosition ||
               transform.rotation != lastRotation ||
               transform.lossyScale != lastScale;
    }

    // IObstacleWithVertices 구현
    public List<Vector2> GetWorldVertices()
    {
        if (!isCached)
        {
            CacheVertices();
        }
        
        return new List<Vector2>(cachedWorldVertices);
    }

    // Public API
    
    /// <summary>
    /// 커스텀 꼭지점 설정 (로컬 좌표)
    /// </summary>
    public void SetCustomVertices(Vector2[] vertices)
    {
        vertexMode = VertexMode.Custom;
        customVertices = vertices;
        CacheVertices();
    }
    
    /// <summary>
    /// 모드 변경
    /// </summary>
    public void SetVertexMode(VertexMode mode)
    {
        vertexMode = mode;
        CacheVertices();
    }
    
    /// <summary>
    /// 강제 리캐싱 (Collider 변경 시)
    /// </summary>
    public void ForceRecache()
    {
        CacheVertices();
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || cachedWorldVertices.Count == 0)
            return;
        
        Gizmos.color = gizmosColor;
        
        // 꼭지점 그리기
        foreach (var vertex in cachedWorldVertices)
        {
            Gizmos.DrawSphere(vertex, 0.1f);
        }
        
        // 엣지 그리기
        for (int i = 0; i < cachedWorldVertices.Count; i++)
        {
            Vector2 start = cachedWorldVertices[i];
            Vector2 end = cachedWorldVertices[(i + 1) % cachedWorldVertices.Count];
            Gizmos.DrawLine(start, end);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;
        
        // 선택 시 하이라이트
        Gizmos.color = Color.yellow;
        
        if (cachedWorldVertices.Count > 0)
        {
            foreach (var vertex in cachedWorldVertices)
            {
                Gizmos.DrawWireSphere(vertex, 0.15f);
            }
        }
    }

    // Editor 전용 메서드
    #if UNITY_EDITOR
    [ContextMenu("🔄 캐시 새로고침")]
    void EditorRecache()
    {
        CacheVertices();
    }
    
    [ContextMenu("📊 꼭지점 개수 출력")]
    void EditorPrintVertexCount()
    {
        if (isCached)
        {
            Debug.Log($"📊 {gameObject.name}: {cachedLocalVertices.Count}개 꼭지점");
        }
        else
        {
            Debug.LogWarning("⚠️ 아직 캐싱되지 않았습니다. Start() 이후에 확인하세요.");
        }
    }
    
    [ContextMenu("🎨 Gizmos 토글")]
    void EditorToggleGizmos()
    {
        showGizmos = !showGizmos;
    }
    #endif
}