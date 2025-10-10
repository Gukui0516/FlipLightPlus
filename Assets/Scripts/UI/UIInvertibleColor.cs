using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 요소의 색상을 반전 상태에 따라 변경하는 컴포넌트
/// Image, Text, RawImage 등 Graphic을 상속받은 모든 UI 컴포넌트에 사용 가능
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class UIInvertibleColor : MonoBehaviour, IInvertibleColor
{
    [Header("Color Settings")]
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private Color invertedColor = Color.black;

    [Header("Options")]
    [Tooltip("활성화하면 시작 시 Graphic 컴포넌트의 현재 색상을 Original Color로 자동 설정")]
    [SerializeField] private bool useCurrentColorAsOriginal = true;

    private Graphic targetGraphic;

    void Awake()
    {
        targetGraphic = GetComponent<Graphic>();

        if (useCurrentColorAsOriginal && targetGraphic != null)
        {
            originalColor = targetGraphic.color;
        }
    }

    void OnValidate()
    {
        // 에디터에서 색상 미리보기 (선택 사항)
        if (!Application.isPlaying && useCurrentColorAsOriginal)
        {
            Graphic graphic = GetComponent<Graphic>();
            if (graphic != null)
            {
                originalColor = graphic.color;
            }
        }
    }

    #region IInvertibleColor Implementation

    public void SetInvertedColor()
    {
        if (targetGraphic != null)
        {
            targetGraphic.color = invertedColor;
        }
    }

    public void SetOriginalColor()
    {
        if (targetGraphic != null)
        {
            targetGraphic.color = originalColor;
        }
    }

    #endregion

    #region Public Methods (수동 호출용)

    /// <summary>
    /// Original Color를 수동으로 설정
    /// </summary>
    public void SetOriginalColorValue(Color color)
    {
        originalColor = color;
    }

    /// <summary>
    /// Inverted Color를 수동으로 설정
    /// </summary>
    public void SetInvertedColorValue(Color color)
    {
        invertedColor = color;
    }

    /// <summary>
    /// 현재 Graphic의 색상을 Original Color로 저장
    /// </summary>
    public void CaptureCurrentColorAsOriginal()
    {
        if (targetGraphic != null)
        {
            originalColor = targetGraphic.color;
        }
    }

    #endregion
}