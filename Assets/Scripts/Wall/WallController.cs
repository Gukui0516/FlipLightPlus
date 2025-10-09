using UnityEngine;

public class WallController : MonoBehaviour, IInvertibleColor
{
    WorldStateManager _worldStateManager;
    [SerializeField] SpriteRenderer _spriteRenderer;
    [SerializeField] Color _originalColor = Color.white; // 원본 색상
    [SerializeField] Color _invertedColor = Color.black; // 반전 색상

    void Awake()
    {
        _worldStateManager = FindFirstObjectByType<WorldStateManager>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError("WallController: SpriteRenderer 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        if (_worldStateManager == null)
        {
            Debug.LogError("WallController: WorldStateManager 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        else
        {
            AddListener();
        }
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _originalColor = _spriteRenderer.color;
        _invertedColor = Color.black;
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    #region WorldStateManager Event addListener

    private void AddListener()
    {
        if (_worldStateManager != null)
        {
            _worldStateManager.onIsInvertedChanged.AddListener(OnInversionChanged);
        }
    }


    #endregion

    #region Color Change Interface Methods
    /// <summary>
    /// 반전 색상으로 변경
    /// </summary>
    public void SetInvertedColor()
    {
        if(_spriteRenderer == null)
        {
            Debug.LogError("WallController: SpriteRenderer 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        _spriteRenderer.color = _invertedColor;
    }

    /// <summary>
    /// 원본 색상으로 복구
    /// </summary>
    public void SetOriginalColor()
    {
        if (_spriteRenderer == null)
        {
            Debug.LogError("WallController: SpriteRenderer 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        _spriteRenderer.color = _originalColor;
    }

    private void OnInversionChanged(bool isInverted)
    {
        if (isInverted)
        {
            SetInvertedColor();
        }
        else
        {
            SetOriginalColor();
        }
    }
    #endregion
}
