using UnityEngine;
using NavMeshPlus.Components;

public class NavmeshBaker : MonoBehaviour
{
    [SerializeField] TerrainSpawner terrainSpawner;

    public NavMeshSurface Surface2D;
    void Awake()
    {
        terrainSpawner = FindFirstObjectByType<TerrainSpawner>();
        if (terrainSpawner == null)
        {
            Debug.LogError("TerrainSpawner not found");
        }

        // NavMeshSurface가 직접 할당되지 않았으면 자동으로 찾기
        if (Surface2D == null)
        {
            Surface2D = GetComponent<NavMeshSurface>();
            if (Surface2D == null)
            {
                Surface2D = FindFirstObjectByType<NavMeshSurface>();
            }
        }

        if (Surface2D == null)
        {
            Debug.LogError("NavMeshSurface not found");
        }
        // TerrainSpawner의 스폰 완료 이벤트 구독
        if (terrainSpawner != null)
        {
            terrainSpawner.OnSpawnComplete.AddListener(BakeNavMesh);
        }
        else
        {
            Debug.LogError("TerrainSpawner is null, cannot subscribe to OnSpawnComplete event.");
        }
    }

    void Start()
    {
        
    }

    void BakeNavMesh()
    {
        if (Surface2D != null)
        {
            Debug.Log("NavMesh 베이킹 시작...");
            Surface2D.BuildNavMesh();
            Debug.Log("NavMesh 베이킹 완료!");
        }
        else
        {
            Debug.LogError("NavMeshSurface가 없어서 베이킹할 수 없습니다.");
        }
    }
}