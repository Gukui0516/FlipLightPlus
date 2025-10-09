using UnityEngine;
using TMPro;

/// <summary>
/// TextMeshPro 텍스트의 색상을 반전 상태에 따라 변경하는 컴포넌트
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class TMPInvertibleColor : MonoBehaviour, IInvertibleColor
{
    [Header("Color Settings")]
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private Color invertedColor = Color.black;

    [Header("Options")]
    [Tooltip("활성화하면 시작 시 TMP_Text의 현재 색상을 Original Color로 자동 설정")]
    [SerializeField] private bool useCurrentColorAsOriginal = true;

    private TMP_Text tmpText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();

        if (useCurrentColorAsOriginal && tmpText != null)
        {
            originalColor = tmpText.color;
        }
    }

    void OnValidate()
    {
        // 에디터에서 색상 미리보기
        if (!Application.isPlaying && useCurrentColorAsOriginal)
        {
            TMP_Text text = GetComponent<TMP_Text>();
            if (text != null)
            {
                originalColor = text.color;
            }
        }
    }

    #region IInvertibleColor Implementation

    public void SetInvertedColor()
    {
        if (tmpText != null)
        {
            tmpText.color = invertedColor;
        }
    }

    public void SetOriginalColor()
    {
        if (tmpText != null)
        {
            tmpText.color = originalColor;
        }
    }

    #endregion

    #region Public Methods (수동 호출용)

    public void SetOriginalColorValue(Color color)
    {
        originalColor = color;
    }

    public void SetInvertedColorValue(Color color)
    {
        invertedColor = color;
    }

    public void CaptureCurrentColorAsOriginal()
    {
        if (tmpText != null)
        {
            originalColor = tmpText.color;
        }
    }

    #endregion
}