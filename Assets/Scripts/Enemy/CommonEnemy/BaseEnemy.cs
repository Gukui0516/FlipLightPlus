using UnityEngine;

/// <summary>
/// 모든 적의 기본 클래스 - 공통 기능 제공
/// </summary>
public abstract class BaseEnemy : MonoBehaviour
{
    #region Protected Variables

    [Header("Basic Settings")]
    [SerializeField] protected float speed = 3f;
    [SerializeField] protected float stoppingDistance = 1.5f;

    [Header("References")]
    [SerializeField] protected WorldStateManager worldStateManager;

    protected Transform player;
    protected bool isInLight = false;
    protected bool isInverted = false;

    // 컴포넌트 모듈들
    protected EnemyMovement movementModule;
    protected EnemyRotation rotationModule;
    protected EnemyVisibility visibilityModule;
    protected EnemyDespawn despawnModule;
    protected Rigidbody2D rb;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // WorldStateManager 찾기
        if (worldStateManager == null)
        {
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        }

        // 모듈 컴포넌트들 가져오기
        movementModule = GetComponent<EnemyMovement>();
        rotationModule = GetComponent<EnemyRotation>();
        visibilityModule = GetComponent<EnemyVisibility>();
        despawnModule = GetComponent<EnemyDespawn>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable()
    {
        InitializeEnemy();
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 이벤트 구독
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.AddListener(OnInversionChanged);
            isInverted = worldStateManager.IsInverted;
        }
    }

    protected virtual void OnDestroy()
    {
        // 이벤트 구독 해제
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.RemoveListener(OnInversionChanged);
        }
    }

    protected virtual void Update()
    {
        // Despawn 체크
        if (despawnModule != null)
        {
            despawnModule.CheckDespawn(player);
        }

        // Visibility 업데이트
        if (visibilityModule != null)
        {
            visibilityModule.UpdateVisibility(player, isInLight, isInverted);
        }

        // 반전시 정지 여부 확인
        if (IsStoppedByInversion()) return;

        // 회전
        if (rotationModule != null && player != null && ShouldRotate())
        {
            rotationModule.RotateTowardsPlayer(player);
        }

        // 이동
        if (ShouldMove())
        {
            if (movementModule != null)
            {
                movementModule.MoveTowardsPlayer(player, GetCurrentSpeed(), stoppingDistance);
            }
        }
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// 적이 움직여야 하는지 판단 (각 타입마다 다른 로직)
    /// </summary>
    protected abstract bool ShouldMove();

    /// <summary>
    /// 적이 회전해야 하는지 판단 (각 타입마다 다른 로직)
    /// </summary>
    protected abstract bool ShouldRotate();

    /// <summary>
    /// 현재 속도 반환 (LightSeeker는 가변 속도)
    /// </summary>
    protected abstract float GetCurrentSpeed();

    /// <summary>
    /// 적 초기화 (타입별 초기화 로직)
    /// </summary>
    protected abstract void InitializeEnemy();

    #endregion

    #region Virtual Methods

    /// <summary>
    /// 반전 상태일 때 멈춰야 하는지 (LightSeeker는 override)
    /// </summary>
    protected virtual bool IsStoppedByInversion()
    {
        return worldStateManager != null && worldStateManager.IsInverted;
    }

    /// <summary>
    /// 반전 상태 변경 이벤트 처리
    /// </summary>
    protected virtual void OnInversionChanged(bool inverted)
    {
        isInverted = inverted;
        Debug.Log($"{gameObject.name} 반전 상태: {inverted}");

        // Visibility 즉시 업데이트
        if (visibilityModule != null && player != null)
        {
            visibilityModule.UpdateVisibility(player, isInLight, isInverted);
        }
    }

    #endregion

    #region Flashlight Events

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Flashlight"))
        {
            isInLight = true;
            Debug.Log($"{gameObject.name} 손전등 진입!");

            // 반전 상태에서 손전등 맞으면 모두 죽음
            if (isInverted)
            {
                Die();
                return;
            }

            OnEnterLight();
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Flashlight"))
        {
            isInLight = false;
            Debug.Log($"{gameObject.name} 손전등 벗어남!");

            OnExitLight();

            // Visibility 업데이트
            if (visibilityModule != null)
            {
                visibilityModule.UpdateVisibility(player, isInLight, isInverted);
            }
        }
    }

    /// <summary>
    /// 손전등 진입 시 추가 처리 (타입별로 override)
    /// </summary>
    protected virtual void OnEnterLight() { }

    /// <summary>
    /// 손전등 벗어남 시 추가 처리 (타입별로 override)
    /// </summary>
    protected virtual void OnExitLight() { }

    #endregion

    #region Public Methods

    public virtual void Die()
    {
        if (visibilityModule != null)
        {
            visibilityModule.HideAll();
        }

        //풀에 반환
        EnemySpawner.Instance?.ReturnEnemy(gameObject);
    }

    #endregion
}