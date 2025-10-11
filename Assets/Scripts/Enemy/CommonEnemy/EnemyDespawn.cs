using UnityEngine;

/// <summary>
/// 적의 Despawn 로직을 담당하는 모듈
/// </summary>
public class EnemyDespawn : MonoBehaviour
{
    [Header("Despawn Settings")]
    [SerializeField] private float despawnDistance = 25f;
    [SerializeField] private float despawnCheckInterval = 1f;

    private float nextDespawnCheckTime;
    private BaseEnemy baseEnemy;

    private void Awake()
    {
        baseEnemy = GetComponent<BaseEnemy>();
    }

    private void OnEnable()
    {
        // 재활성화될 때 타이머 초기화
        nextDespawnCheckTime = Time.time + despawnCheckInterval;
    }

    /// <summary>
    /// 플레이어와 거리가 멀어지면 Despawn 체크
    /// </summary>
    public void CheckDespawn(Transform player)
    {
        // 일정 간격마다만 체크 (성능 최적화)
        if (Time.time < nextDespawnCheckTime) return;
        nextDespawnCheckTime = Time.time + despawnCheckInterval;

        if (player == null) return;

        // sqrMagnitude 사용으로 성능 최적화
        float distanceSqr = (transform.position - player.position).sqrMagnitude;

        if (distanceSqr > despawnDistance * despawnDistance)
        {
            Despawn();
        }
    }

    /// <summary>
    /// 거리가 멀어져서 반환
    /// </summary>
    private void Despawn()
    {
        if (baseEnemy != null)
        {
            baseEnemy.Die();
        }
    }
}