using UnityEngine;

/// <summary>
/// 적의 이동 로직을 담당하는 모듈
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    /// <summary>
    /// 플레이어를 향해 이동
    /// </summary>
    public void MoveTowardsPlayer(Transform player, float speed, float stoppingDistance)
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stoppingDistance)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }
}