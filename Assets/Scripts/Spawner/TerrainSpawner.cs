using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TerrainData
{
    public GameObject terrain;
    public int weight;
}

public class TerrainSpawner : MonoBehaviour
{
    [SerializeField] float mapHorizontal;//맵 최대길이
    [SerializeField] float mapVertical;
    float startHorizontal;//맵 초기위치(-,-)
    float startVertical;
    [SerializeField] float terrainHorizontal;//지형 간격
    [SerializeField] float terrainVertical;
    [SerializeField] LayerMask wallMask;
    [SerializeField] float clearCircle = 10f;//중앙지점에서 벽 제거
    [SerializeField] TerrainData[] terrains;


    private List<Vector2> availablePositions = new List<Vector2>();//지형 생성 가능 좌표


    private void Awake()
    {
        GeneratePositions();//최대 맵 길이에서 생성 가능 좌표 계산
        SpawnTerrains();//지형 생성

    }
    private void LateUpdate()
    {        
        ClearWalls();//중앙으로부터 벽 제거
        //생성 후 x축 반전 시 컬리이더는 기존걸로 남아있는 이슈가 있어서 여기서 처리함
        Destroy(gameObject);
    }
    void GeneratePositions()
    {
        startHorizontal = mapHorizontal / 2;
        startVertical = mapVertical / 2;
        int horizontalCount = Mathf.FloorToInt((mapHorizontal- terrainHorizontal) / terrainHorizontal);
        int verticalCount = Mathf.FloorToInt((mapVertical- terrainVertical) / terrainVertical);


        for (int x = 0; x <= horizontalCount; x++)
        {
            for (int y = 0; y <= verticalCount; y++)
            {
                Vector2 pos = new Vector2(x * terrainHorizontal - startHorizontal+(terrainHorizontal/2), y * terrainVertical - startVertical+(terrainVertical/2));
                availablePositions.Add(pos);//지형 생성 가능 좌표에 추가
            }
        }
    }

    // 2. 가중치 기반 랜덤 선택
    TerrainData GetRandomTerrain()
    {
        int totalWeight = 0;
        foreach (var t in terrains)
            totalWeight += t.weight;

        int randomValue = Random.Range(0, totalWeight);
        int sum = 0;

        foreach (var t in terrains)
        {
            sum += t.weight;
            if (randomValue < sum)
                return t;
        }

        return terrains[0]; // 안전 장치
    }

    // 3. 지형 생성
    void SpawnTerrains()
    {
        List<Vector2> positionsCopy = new List<Vector2>(availablePositions);

        while (positionsCopy.Count > 0)
        {
            int index = Random.Range(0, positionsCopy.Count);
            Vector2 spawnPos = positionsCopy[index];

            TerrainData terrain = GetRandomTerrain();
            float zRotation = (90 * Random.Range(0, 3));
            GameObject ter = Instantiate(terrain.terrain, new Vector2(spawnPos.x, spawnPos.y), Quaternion.Euler(0, 0, zRotation));
            if (Random.Range(0, 2) == 0) ter.transform.localScale = new Vector2(-1, 1);//반전 실행
            
            
            Debug.Log("XLoc"+spawnPos.x+" YLoc"+spawnPos.y+" Name"+terrain.ToString());
            positionsCopy.RemoveAt(index); // 이미 사용한 위치 제거
        }
    }
    void ClearWalls()
    {
        Collider2D[] walls = (Physics2D.OverlapCircleAll(Vector2.zero, clearCircle, wallMask));
        foreach (Collider2D wall in walls) 
        {
            Destroy(wall.gameObject);
        }            
    }
}

