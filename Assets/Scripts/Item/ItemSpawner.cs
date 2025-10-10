using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class ItemSpawner : MonoBehaviour
{
    // 아이템 스포너

    #region Variables

    [Header("Spawn Settings")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxItems = 10;
    [SerializeField] private float spawnDistance = 15f;

    [Header("Map Boundaries")]
    [SerializeField] private Vector2 mapMin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 mapMax = new Vector2(50f, 50f);
    [SerializeField] private bool enableMapBoundary = true;

    [Header("Pool Settings")]
    [SerializeField] private int defaultPoolCapacity = 10;
    [SerializeField] private int maxPoolSize = 20;

    [Header("Despawn Settings")]
    [SerializeField] private float despawnDistance = 25f;
    [SerializeField] private float optionalMaxLifetime = -1f;

    [Header("Refs")]
    [SerializeField] private WorldStateManager world;

    private Camera mainCamera;
    private Transform player;
    private ObjectPool<GameObject> itemPool;

    public static ItemSpawner Instance { get; private set; }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

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

        itemPool = new ObjectPool<GameObject>(
            createFunc: CreateItem,
            actionOnGet: OnGetItem,
            actionOnRelease: OnReleaseItem,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,
            defaultCapacity: defaultPoolCapacity,
            maxSize: maxPoolSize
        );
    }

    void Start()
    {
        mainCamera = Camera.main;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO) player = playerGO.transform;
        else Debug.LogWarning("[ItemSpawner] Player 태그 오브젝트를 찾지 못했습니다.");

        SpawnInitialItem();
        StartCoroutine(SpawnCoroutine());

        Debug.Log($"[ItemSpawner] Map Boundary: ({mapMin.x}, {mapMin.y}) ~ ({mapMax.x}, {mapMax.y})");
    }

    void SpawnInitialItem()
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();

        // 맵 범위 안에 있을 때만 생성
        if (IsInsideMapBoundary(spawnPosition))
        {
            SpawnItem(spawnPosition);
            Debug.Log($"[ItemSpawner] Initial item spawned at {spawnPosition}");
        }
        else
        {
            Debug.LogWarning($"[ItemSpawner] Initial spawn position {spawnPosition} is outside map boundary");
        }
    }

    #endregion

    #region Object Pool Callbacks

    GameObject CreateItem()
    {
        GameObject item = Instantiate(itemPrefab);
        item.SetActive(false);

        if (!item.TryGetComponent<PooledItem>(out _))
            item.AddComponent<PooledItem>();

        return item;
    }

    void OnGetItem(GameObject item)
    {
        item.SetActive(true);

        var pooled = item.GetComponent<PooledItem>();
        if (pooled != null)
        {
            pooled.Setup(
                player,
                despawnDistance,
                ReleaseItem,
                optionalMaxLifetime
            );
        }
        else
        {
            Debug.LogWarning($"[ItemSpawner] PooledItem 누락: {item.name}");
        }

        if (item.TryGetComponent<InvertPickup>(out var pickup)
            || item.GetComponentInChildren<InvertPickup>(true) is InvertPickup childPickup && (pickup = childPickup) != null)
        {
            pickup.Init(world);
            pickup.onConsumed = () => ReleaseItem(item);
        }
    }

    void OnReleaseItem(GameObject item)
    {
        item.SetActive(false);
    }

    void OnDestroyItem(GameObject item)
    {
        Destroy(item);
    }

    private void ReleaseItem(GameObject go)
    {
        if (go != null)
            itemPool.Release(go);
    }

    #endregion

    #region Spawn Logic

    IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (itemPool.CountActive < maxItems)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();

                // 카메라 밖 + 맵 범위 안 체크
                if (IsOutsideCameraView(spawnPosition) && IsInsideMapBoundary(spawnPosition))
                {
                    SpawnItem(spawnPosition);
                }
            }
        }
    }

    Vector2 GetRandomSpawnPosition()
    {
        Vector2 center = mainCamera.transform.position;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float x = center.x + Mathf.Cos(randomAngle) * spawnDistance;
        float y = center.y + Mathf.Sin(randomAngle) * spawnDistance;
        return new Vector2(x, y);
    }

    bool IsOutsideCameraView(Vector2 position)
    {
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

    void SpawnItem(Vector2 position)
    {
        GameObject item = itemPool.Get();
        item.transform.SetPositionAndRotation(position, Quaternion.identity);
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