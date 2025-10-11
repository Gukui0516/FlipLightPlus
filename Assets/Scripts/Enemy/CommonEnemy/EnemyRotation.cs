using UnityEngine;

/// <summary>
/// 적의 회전 로직을 담당하는 모듈
/// </summary>
public class EnemyRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float rotationOffset = 180f; // 스프라이트가 아래를 보고 있으면 180

    /// <summary>
    /// 플레이어 방향으로 회전
    /// </summary>
    public void RotateTowardsPlayer(Transform player)
    {
        if (!enableRotation || player == null) return;

        // 플레이어 방향 벡터 계산
        Vector2 direction = (player.position - transform.position).normalized;

        // 각도 계산
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        targetAngle -= rotationOffset;

        // 부드러운 회전
        float currentAngle = transform.eulerAngles.z;
        float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);

        // 회전 적용
        transform.rotation = Quaternion.Euler(0, 0, smoothAngle);
    }
}