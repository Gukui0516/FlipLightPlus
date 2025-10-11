using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    #region Enemy Type Definition

    [System.Serializable]
    public class EnemySpawnData
    {
        [Header("Basic Info")]
        public string enemyName;
        public GameObject enemyPrefab;

        [Header("Spawn Settings")]
        public float initialSpawnDelay = 2f;  // ⭐ 첫 스폰 대기 시간
        public float spawnInterval = 2f;
        public int maxCount = 5;

        [Header("Pool Settings")]
        public int poolCapacity = 5;
        public int poolMaxSize = 10;

        [HideInInspector] public ObjectPool<GameObject> pool;
        [HideInInspector] public int currentCount = 0;
        [HideInInspector] public Coroutine spawnCoroutine;
    }

    #endregion

    #region Variables

    [Header("Spawn Settings")]
    [SerializeField] private List<EnemySpawnData> enemyTypes = new List<EnemySpawnData>();
    [SerializeField] private float spawnDistance = 15f;

    [Header("Map Boundaries")]
    [SerializeField] private Vector2 mapMin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 mapMax = new Vector2(50f, 50f);
    [SerializeField] private bool enableMapBoundary = true;

    private Camera mainCamera;
    private Transform player;
    private int totalEnemyCount = 0;

    private Dictionary<GameObject, EnemySpawnData> instanceToData = new Dictionary<GameObject, EnemySpawnData>();

    public static EnemySpawner Instance { get; private set; }

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

        InitializePools();
    }

    void Start()
    {
        mainCamera = Camera.main;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogWarning("[EnemySpawner] Player not found! Spawning around (0,0)");
        }

        // ⭐ GameManager 없이 바로 스폰 시작
        StartAllSpawners();

        Debug.Log("[EnemySpawner] Started spawning enemies without GameManager");
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
        foreach (var enemyData in enemyTypes)
        {
            enemyData.pool = new ObjectPool<GameObject>(
                createFunc: () => CreateEnemy(enemyData),
                actionOnGet: (enemy) => OnGetEnemy(enemy, enemyData),
                actionOnRelease: (enemy) => OnReleaseEnemy(enemy, enemyData),
                actionOnDestroy: (enemy) => OnDestroyEnemy(enemy, enemyData),
                collectionCheck: true,
                defaultCapacity: enemyData.poolCapacity,
                maxSize: enemyData.poolMaxSize
            );
        }
    }

    #endregion

    #region Object Pool Callbacks

    GameObject CreateEnemy(EnemySpawnData data)
    {
        GameObject enemy = Instantiate(data.enemyPrefab);
        enemy.name = $"{data.enemyName}_Pooled";

        instanceToData[enemy] = data;

        enemy.SetActive(false);
        return enemy;
    }

    void OnGetEnemy(GameObject enemy, EnemySpawnData data)
    {
        data.currentCount++;
        totalEnemyCount++;
    }

    void OnReleaseEnemy(GameObject enemy, EnemySpawnData data)
    {
        enemy.SetActive(false);
        data.currentCount--;
        totalEnemyCount--;
    }

    void OnDestroyEnemy(GameObject enemy, EnemySpawnData data)
    {
        instanceToData.Remove(enemy);
        Destroy(enemy);
    }

    #endregion

    #region Spawn Logic

    void StartAllSpawners()
    {
        foreach (var enemyData in enemyTypes)
        {
            if (enemyData.maxCount > 0 && enemyData.spawnCoroutine == null)
            {
                enemyData.spawnCoroutine = StartCoroutine(SpawnCoroutine(enemyData));
                Debug.Log($"[EnemySpawner] Started spawner for {enemyData.enemyName} " +
                         $"(initial delay: {enemyData.initialSpawnDelay}s, " +
                         $"interval: {enemyData.spawnInterval}s, maxCount: {enemyData.maxCount})");
            }
        }

        Debug.Log($"[EnemySpawner] Map Boundary: ({mapMin.x}, {mapMin.y}) ~ ({mapMax.x}, {mapMax.y})");
    }

    void StopAllSpawners()
    {
        foreach (var enemyData in enemyTypes)
        {
            if (enemyData.spawnCoroutine != null)
            {
                StopCoroutine(enemyData.spawnCoroutine);
                enemyData.spawnCoroutine = null;
            }
        }
    }

    IEnumerator SpawnCoroutine(EnemySpawnData data)
    {
        // ⭐ 첫 스폰은 초기 대기 시간 후
        yield return new WaitForSeconds(data.initialSpawnDelay);

        // 첫 스폰 시도
        if (data.currentCount < data.maxCount)
        {
            Vector2 spawnPosition = GetRandomSpawnPosition();

            if (IsOutsideCameraView(spawnPosition) && IsInsideMapBoundary(spawnPosition))
            {
                SpawnEnemy(data, spawnPosition);
            }
        }

        // ⭐ 이후부터는 인터벌마다 스폰
        while (true)
        {
            yield return new WaitForSeconds(data.spawnInterval);

            if (data.currentCount < data.maxCount)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();

                // ⭐ 카메라 밖 + 맵 범위 안 체크
                if (IsOutsideCameraView(spawnPosition) && IsInsideMapBoundary(spawnPosition))
                {
                    SpawnEnemy(data, spawnPosition);
                }
            }
        }
    }

    void SpawnEnemy(EnemySpawnData data, Vector2 position)
    {
        if (data.pool != null)
        {
            GameObject enemy = data.pool.Get();
            enemy.transform.position = position;
            enemy.transform.rotation = Quaternion.identity;
            enemy.SetActive(true);
        }
    }

    Vector2 GetRandomSpawnPosition()
    {
        // 플레이어 위치를 중심으로 스폰
        Vector2 center = player != null ? (Vector2)player.position : Vector2.zero;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float x = center.x + Mathf.Cos(randomAngle) * spawnDistance;
        float y = center.y + Mathf.Sin(randomAngle) * spawnDistance;

        return new Vector2(x, y);
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

    public void ReturnEnemy(GameObject enemy)
    {
        if (instanceToData.TryGetValue(enemy, out EnemySpawnData data))
        {
            data.pool.Release(enemy);
        }
    }

    public void PauseSpawner(string enemyName)
    {
        var data = enemyTypes.Find(e => e.enemyName == enemyName);
        if (data != null && data.spawnCoroutine != null)
        {
            StopCoroutine(data.spawnCoroutine);
            data.spawnCoroutine = null;
        }
    }

    public void ResumeSpawner(string enemyName)
    {
        var data = enemyTypes.Find(e => e.enemyName == enemyName);
        if (data != null && data.spawnCoroutine == null)
        {
            data.spawnCoroutine = StartCoroutine(SpawnCoroutine(data));
        }
    }

    public void PrintPoolStatus()
    {
        Debug.Log("=== Pool Status ===");
        foreach (var data in enemyTypes)
        {
            Debug.Log($"{data.enemyName}: {data.currentCount}/{data.maxCount} active " +
                      $"(Initial Delay: {data.initialSpawnDelay}s, Interval: {data.spawnInterval}s, " +
                      $"Pool: {data.poolCapacity}/{data.poolMaxSize})");
        }
        Debug.Log($"Total Active: {totalEnemyCount}");
    }

    #endregion


    #region Gizmos

    void OnDrawGizmos()
    {
        if (!enableMapBoundary) return;

        // 맵 경계 박스 그리기
        Gizmos.color = Color.yellow;
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

        // 스폰 거리 원 그리기 (플레이 중일 때)
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.cyan;
            DrawCircle(player.position, spawnDistance, 64);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!enableMapBoundary) return;

        // 선택되었을 때 더 진한 색으로 표시
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
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