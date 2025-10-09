using UnityEngine;

/// <summary>
/// 적 타입 열거형
/// </summary>
public enum EnemyType
{
    Normal,
    LightSeeker
}

/// <summary>
/// 적의 시각적 요소(Eyes, Outline)를 관리하는 모듈
/// </summary>
public class EnemyVisibility : MonoBehaviour
{
    [Header("Visibility Settings")]
    [SerializeField] private bool useOutline = false;
    [SerializeField] private float eyesVisibleDistance = 10f;
    [SerializeField] private float lightSeekerVisibilityDistance = 10f;

    [Header("Component References")]
    [SerializeField] private EnemyOutline enemyOutline;
    [SerializeField] private GameObject eyesObject;

    private EnemyType enemyType;

    private void Awake()
    {
        // 자동 참조 찾기
        if (enemyOutline == null)
        {
            enemyOutline = GetComponent<EnemyOutline>();
        }

        if (eyesObject == null)
        {
            Transform eyesTransform = transform.Find("Eyes");
            if (eyesTransform != null)
            {
                eyesObject = eyesTransform.gameObject;
            }
        }
    }

    /// <summary>
    /// 적 타입에 따라 초기화
    /// </summary>
    public void Initialize(EnemyType type)
    {
        enemyType = type;

        // 아웃라인 초기 설정
        if (useOutline && enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(true);
        }
        else if (enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(false);
        }

        // Eyes 초기 설정
        if (eyesObject != null)
        {
            eyesObject.SetActive(false);
        }
    }

    /// <summary>
    /// 거리와 상태에 따라 Visibility 업데이트
    /// </summary>
    public void UpdateVisibility(Transform player, bool isInLight, bool isInverted)
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Eyes 업데이트
        UpdateEyesVisibility(distance, isInLight, isInverted);
        UpdateEyesColor(isInLight, isInverted);

        // 아웃라인 업데이트
        if (useOutline && enemyOutline != null)
        {
            UpdateOutlineColor(distance, isInLight, isInverted);
        }
    }

    /// <summary>
    /// 모든 시각 요소 숨기기 (죽거나 Despawn될 때)
    /// </summary>
    public void HideAll()
    {
        if (useOutline && enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(false);
        }

        if (eyesObject != null)
        {
            eyesObject.SetActive(false);
        }
    }

    #region Private Methods

    private void UpdateEyesVisibility(float distance, bool isInLight, bool isInverted)
    {
        if (eyesObject == null) return;

        bool shouldBeActive;

        if (enemyType == EnemyType.LightSeeker)
        {
            // 반전 상태: 거리 상관없이 항상 보임
            if (isInverted)
            {
                shouldBeActive = true;
            }
            // 평소: 손전등 안 또는 거리 내에 있으면 보임
            else
            {
                shouldBeActive = isInLight || distance <= lightSeekerVisibilityDistance;
            }
        }
        else // Normal
        {
            shouldBeActive = distance <= eyesVisibleDistance;
        }

        if (shouldBeActive != eyesObject.activeSelf)
        {
            eyesObject.SetActive(shouldBeActive);
        }
    }

    private void UpdateEyesColor(bool isInLight, bool isInverted)
    {
        if (eyesObject == null) return;

        Color targetColor = Color.white;

        if (enemyType == EnemyType.LightSeeker)
        {
            // 반전 상태: 무조건 검은색
            if (isInverted)
            {
                targetColor = Color.black;
            }
            else if (isInLight)
            {
                // 평소 + 손전등 안 → 검은색
                targetColor = Color.black;
            }
            else
            {
                // 평소 + 손전등 밖 → 흰색
                targetColor = Color.white;
            }
        }
        // Normal은 항상 흰색

        // Eyes의 모든 자식 SpriteRenderer 색상 변경
        SpriteRenderer[] renderers = eyesObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.color = targetColor;
        }
    }

    private void UpdateOutlineColor(float distance, bool isInLight, bool isInverted)
    {
        if (enemyOutline == null) return;

        Color outlineColor = Color.black;

        if (enemyType == EnemyType.LightSeeker)
        {
            // 반전 상태: 무조건 검은색
            if (isInverted)
            {
                outlineColor = Color.black;
            }
            else if (isInLight)
            {
                // 평소 + 손전등 안 → 검은색
                outlineColor = Color.black;
            }
            else
            {
                // 평소 + 손전등 밖
                if (distance <= lightSeekerVisibilityDistance)
                {
                    outlineColor = Color.white; // 가까움 → 흰색
                }
                else
                {
                    outlineColor = Color.black; // 멀음 → 검은색
                }
            }
        }
        else // Normal
        {
            // 반전 상태에 따라 색상 변경
            outlineColor = isInverted ? Color.white : Color.black;
        }

        enemyOutline.SetOutlineColor(outlineColor);
    }

    #endregion
}