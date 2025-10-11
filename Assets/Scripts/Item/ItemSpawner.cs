using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    #region Item Type Definition

    [System.Serializable]
    public class ItemSpawnData
    {
        [Header("Basic Info")]
        public string itemName;
        public GameObject itemPrefab;

        [Header("Spawn Settings")]
        public float initialSpawnDelay = 2f;  // 첫 스폰 대기 시간
        public float spawnInterval = 2f;
        public int maxCount = 5;

        [Header("Pool Settings")]
        public int poolCapacity = 5;
        public int poolMaxSize = 10;

        [Header("Despawn Settings")]
        public float despawnDistance = 25f;
        public float optionalMaxLifetime = -1f;

        [HideInInspector] public ObjectPool<GameObject> pool;
        [HideInInspector] public int currentCount = 0;
        [HideInInspector] public Coroutine spawnCoroutine;
    }

    #endregion

    #region Variables

    [Header("Spawn Settings")]
    [SerializeField] private List<ItemSpawnData> itemTypes = new List<ItemSpawnData>();
    [SerializeField] private float spawnDistance = 15f;

    [Header("Map Boundaries")]
    [SerializeField] private Vector2 mapMin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 mapMax = new Vector2(50f, 50f);
    [SerializeField] private bool enableMapBoundary = true;

    [Header("Wall Collision")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float itemDistance = 10f;  // 아이템 간의 최소 간격

    [Header("References")]
    [SerializeField] private WorldStateManager world;

    private Camera mainCamera;
    private Transform player;
    private int totalItemCount = 0;

    private Dictionary<GameObject, ItemSpawnData> instanceToData = new Dictionary<GameObject, ItemSpawnData>();

    public static ItemSpawner Instance { get; private set; }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // WorldStateManager 찾기
        if (!world)
        {
#if UNITY_2023_1_OR_NEWER
            world = FindFirstObjectByType<WorldStateManager>();
#else
            world = FindObjectOfType<WorldStateManager>();
#endif
            if (!world)
                Debug.LogWarning("[ItemSpawner] WorldStateManager를 찾지 못했습니다.");
        }

        InitializePools();
    }

    void Start()
    {
        mainCamera = Camera.main;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO) player = playerGO.transform;
        else Debug.LogWarning("[ItemSpawner] Player 태그 오브젝트를 찾지 못했습니다.");

        // 모든 아이템 스포너 시작
        StartAllSpawners();

        Debug.Log("[ItemSpawner] Started spawning items");
        PrintPoolStatus();
    }

    void OnDestroy()
    {
        StopAllSpawners();
    }

    #endregion

    #region Pool Initialization

    void InitializePools()
    {
        foreach (var itemData in itemTypes)
        {
            itemData.pool = new ObjectPool<GameObject>(
                createFunc: () => CreateItem(itemData),
                actionOnGet: (item) => OnGetItem(item, itemData),
                actionOnRelease: (item) => OnReleaseItem(item, itemData),
                actionOnDestroy: (item) => OnDestroyItem(item, itemData),
                collectionCheck: true,
                defaultCapacity: itemData.poolCapacity,
                maxSize: itemData.poolMaxSize
            );
        }
    }

    #endregion

    #region Object Pool Callbacks

    GameObject CreateItem(ItemSpawnData data)
    {
        GameObject item = Instantiate(data.itemPrefab);
        item.name = $"{data.itemName}_Pooled";

        instanceToData[item] = data;

        // PooledItem 컴포넌트 추가
        if (!item.TryGetComponent<PooledItem>(out _))
            item.AddComponent<PooledItem>();

        item.SetActive(false);
        return item;
    }

    void OnGetItem(GameObject item, ItemSpawnData data)
    {
        data.currentCount++;
        totalItemCount++;

        item.SetActive(true);

        // PooledItem 설정
        var pooled = item.GetComponent<PooledItem>();
        if (pooled != null)
        {
            pooled.Setup(
                player,
                data.despawnDistance,
                (go) => ReleaseItem(go),
                data.optionalMaxLifetime
            );
        }
        else
        {
            Debug.LogWarning($"[ItemSpawner] PooledItem 누락: {item.name}");
        }

        // InvertPickup 설정
        if (item.TryGetComponent<InvertPickup>(out var pickup)
            || item.GetComponentInChildren<InvertPickup>(true) is InvertPickup childPickup && (pickup = childPickup) != null)
        {
            pickup.Init(world);
            pickup.onConsumed = () => ReleaseItem(item);
        }
    }

    void OnReleaseItem(GameObject item, ItemSpawnData data)
    {
        item.SetActive(false);
        data.currentCount--;
        totalItemCount--;
    }

    void OnDestroyItem(GameObject item, ItemSpawnData data)
    {
        instanceToData.Remove(item);
        Destroy(item);
    }

    #endregion

    #region Spawn Logic

    void StartAllSpawners()
    {
        foreach (var itemData in itemTypes)
        {
            if (itemData.maxCount > 0 && itemData.spawnCoroutine == null)
            {
                itemData.spawnCoroutine = StartCoroutine(SpawnCoroutine(itemData));
                Debug.Log($"[ItemSpawner] Started spawner for {itemData.itemName} " +
                         $"(initial delay: {itemData.initialSpawnDelay}s, " +
                         $"interval: {itemData.spawnInterval}s, maxCount: {itemData.maxCount})");
            }
        }

        Debug.Log($"[ItemSpawner] Map Boundary: ({mapMin.x}, {mapMin.y}) ~ ({mapMax.x}, {mapMax.y})");
    }

    void StopAllSpawners()
    {
        foreach (var itemData in itemTypes)
        {
            if (itemData.spawnCoroutine != null)
            {
                StopCoroutine(itemData.spawnCoroutine);
                itemData.spawnCoroutine = null;
            }
        }
    }

    IEnumerator SpawnCoroutine(ItemSpawnData data)
    {
        // 첫 스폰은 초기 대기 시간 후
        yield return new WaitForSeconds(data.initialSpawnDelay);

        // 첫 스폰 시도
        if (data.currentCount < data.maxCount)
        {
            Vector2 spawnPosition = GetRandomSpawnPosition();

            if (IsOutsideCameraView(spawnPosition) && IsInsideMapBoundary(spawnPosition))
            {
                SpawnItem(data, spawnPosition);
            }
        }

        // 이후부터는 인터벌마다 스폰
        while (true)
        {
            yield return new WaitForSeconds(data.spawnInterval);

            if (data.currentCount < data.maxCount)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();

                // 카메라 밖 + 맵 범위 안 체크
                if (IsOutsideCameraView(spawnPosition) && IsInsideMapBoundary(spawnPosition))
                {
                    SpawnItem(data, spawnPosition);
                }
            }
        }
    }

    void SpawnItem(ItemSpawnData data, Vector2 position)
    {
        if (data.pool != null)
        {
            GameObject item = data.pool.Get();
            item.transform.SetPositionAndRotation(position, Quaternion.identity);
        }
    }

    Vector2 GetRandomSpawnPosition()
    {
        Vector2 center = player != null ? (Vector2)player.position : (Vector2)mainCamera.transform.position;

        // 최대 5000번 시도
        for (int i = 0; i < 5000; i++)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(randomAngle) * spawnDistance;
            float y = center.y + Mathf.Sin(randomAngle) * spawnDistance;
            Vector2 candidatePos = new Vector2(x, y);

            // 아이템 간 거리 체크
            Collider2D[] hits = Physics2D.OverlapCircleAll(candidatePos, itemDistance);
            bool hasItem = false;
            foreach (var h in hits)
            {
                if (h != null && h.CompareTag("Item"))
                {
                    hasItem = true;
                    break;
                }
            }

            // 아이템이 없고 벽에 안 닿으면 해당 위치 반환
            if (!hasItem && Physics2D.OverlapBox(candidatePos, new Vector2(1, 1), 0f, wallLayer) == null)
            {
                return candidatePos;
            }
        }

        // 실패 시 기본 위치 반환
        Debug.LogWarning("[ItemSpawner] Failed to find valid spawn position after 5000 attempts");
        return center;
    }

    bool IsOutsideCameraView(Vector2 position)
    {
        if (mainCamera == null) return true;

        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);
        return viewportPoint.x < 0 || viewportPoint.x > 1 ||
               viewportPoint.y < 0 || viewportPoint.y > 1;
    }

    bool IsInsideMapBoundary(Vector2 position)
    {
        if (!enableMapBoundary) return true;

        return position.x >= mapMin.x && position.x <= mapMax.x &&
               position.y >= mapMin.y && position.y <= mapMax.y;
    }

    #endregion

    #region Public Methods

    public void ReleaseItem(GameObject item)
    {
        if (instanceToData.TryGetValue(item, out ItemSpawnData data))
        {
            data.pool.Release(item);
        }
    }

    public void PauseSpawner(string itemName)
    {
        var data = itemTypes.Find(i => i.itemName == itemName);
        if (data != null && data.spawnCoroutine != null)
        {
            StopCoroutine(data.spawnCoroutine);
            data.spawnCoroutine = null;
        }
    }

    public void ResumeSpawner(string itemName)
    {
        var data = itemTypes.Find(i => i.itemName == itemName);
        if (data != null && data.spawnCoroutine == null)
        {
            data.spawnCoroutine = StartCoroutine(SpawnCoroutine(data));
        }
    }

    public void PrintPoolStatus()
    {
        Debug.Log("=== Item Pool Status ===");
        foreach (var data in itemTypes)
        {
            Debug.Log($"{data.itemName}: {data.currentCount}/{data.maxCount} active " +
                      $"(Initial Delay: {data.initialSpawnDelay}s, Interval: {data.spawnInterval}s, " +
                      $"Pool: {data.poolCapacity}/{data.poolMaxSize})");
        }
        Debug.Log($"Total Active Items: {totalItemCount}");
    }

    #endregion

    #region Gizmos

    void OnDrawGizmos()
    {
        if (!enableMapBoundary) return;

        // 맵 경계 박스
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            (mapMin.x + mapMax.x) / 2f,
            (mapMin.y + mapMax.y) / 2f,
            0f
        );
        Vector3 size = new Vector3(
            mapMax.x - mapMin.x,
            mapMax.y - mapMin.y,
            0.1f
        );
        Gizmos.DrawWireCube(center, size);

        // 스폰 거리 원 (플레이 중일 때)
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.cyan;
            DrawCircle(player.position, spawnDistance, 64);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!enableMapBoundary) return;

        // 선택되었을 때 반투명 박스
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 center = new Vector3(
            (mapMin.x + mapMax.x) / 2f,
            (mapMin.y + mapMax.y) / 2f,
            0f
        );
        Vector3 size = new Vector3(
            mapMax.x - mapMin.x,
            mapMax.y - mapMin.y,
            0.1f
        );
        Gizmos.DrawCube(center, size);

        // 코너 마커
        Gizmos.color = Color.red;
        float markerSize = 1f;
        Gizmos.DrawWireSphere(new Vector3(mapMin.x, mapMin.y, 0), markerSize);
        Gizmos.DrawWireSphere(new Vector3(mapMax.x, mapMin.y, 0), markerSize);
        Gizmos.DrawWireSphere(new Vector3(mapMin.x, mapMax.y, 0), markerSize);
        Gizmos.DrawWireSphere(new Vector3(mapMax.x, mapMax.y, 0), markerSize);
    }

    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    #endregion
}