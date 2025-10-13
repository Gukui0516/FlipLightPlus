// C:/Users/jungle/Documents/GitHub/W5PlusTeam1/Assets/Scripts\Enemy\CommonEnemy\BaseEnemy.cs

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 모든 적의 기본 클래스 - 공통 기능 제공
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class BaseEnemy : MonoBehaviour
{
    #region Protected Variables

    [Header("Basic Settings")]
    [SerializeField] protected float speed = 3f;
    [SerializeField] protected float stoppingDistance = 1.5f;

    [Header("Object Pooling Settings")]
    [SerializeField] protected bool useObjectPooling = true; // ✅ 오브젝트 풀링 사용 여부 (기본값: true)

    [Header("References")]
    [SerializeField] protected WorldStateManager worldStateManager;

    protected Transform player;
    protected bool isInLight = false;
    protected bool isInverted = false;
    protected bool isDead = false;

    // 컴포넌트 모듈들
    protected EnemyMovement movementModule;
    protected EnemyRotation rotationModule;
    protected EnemyVisibility visibilityModule;
    protected EnemyDespawn despawnModule;
    protected EnemyDieAnimation dieAnimationModule;
    protected Rigidbody2D rb;
    protected NavMeshAgent agent;

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
        dieAnimationModule = GetComponent<EnemyDieAnimation>();
        rb = GetComponent<Rigidbody2D>();

        // NavMeshAgent 컴포넌트 가져오기 및 2D 환경 설정
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    protected virtual void OnEnable()
    {
        isDead = false;
        InitializeEnemy();

        if (dieAnimationModule != null)
        {
            dieAnimationModule.ResetAnimation();
            dieAnimationModule.onDeathAnimationComplete -= OnDeathComplete;
            dieAnimationModule.onDeathAnimationComplete += OnDeathComplete;
        }

        // NavMeshAgent 활성화 시 위치 동기화
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                agent.enabled = true;
            }
        }
        else if (agent != null)
        {
            agent.enabled = true;
        }
    }

    protected virtual void OnDisable()
    {
        if (dieAnimationModule != null)
        {
            dieAnimationModule.onDeathAnimationComplete -= OnDeathComplete;
        }
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.AddListener(OnInversionChanged);
            isInverted = worldStateManager.IsInverted;
        }
    }

    protected virtual void OnDestroy()
    {
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.RemoveListener(OnInversionChanged);
        }
        if (dieAnimationModule != null)
        {
            dieAnimationModule.onDeathAnimationComplete -= OnDeathComplete;
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (despawnModule != null)
        {
            despawnModule.CheckDespawn(player);
        }
        if (visibilityModule != null)
        {
            visibilityModule.UpdateVisibility(player, isInLight, isInverted);
        }
        if (rotationModule != null && player != null && ShouldRotate())
        {
            rotationModule.RotateTowardsPlayer(player);
        }
    }

    protected virtual bool UseNavMeshMovement => true;

    protected virtual void FixedUpdate()
    {
        if (isDead)
        {
            movementModule?.StopImmediate();
            return;
        }

        if (IsStoppedByInversion())
        {
            movementModule?.StopImmediate();
            return;
        }

        if (ShouldMove())
        {
            if (movementModule != null)
            {
                if (UseNavMeshMovement)
                    movementModule.MoveTowardsPlayer(player, GetCurrentSpeed(), stoppingDistance);
                else
                    movementModule.MoveForwardRB(GetCurrentSpeed());
            }
        }
        else
        {
            movementModule?.StopImmediate();
        }
    }

    #endregion

    #region Abstract & Virtual Methods

    protected abstract bool ShouldMove();
    protected abstract bool ShouldRotate();
    protected abstract float GetCurrentSpeed();
    protected abstract void InitializeEnemy();

    protected virtual bool UsesNavMesh() => true;

    protected virtual bool IsStoppedByInversion()
    {
        return worldStateManager != null && worldStateManager.IsInverted;
    }

    protected virtual void OnInversionChanged(bool inverted)
    {
        isInverted = inverted;
        Debug.Log($"{gameObject.name} 반전 상태: {inverted}");
        if (visibilityModule != null && player != null)
        {
            visibilityModule.UpdateVisibility(player, isInLight, isInverted);
        }
    }

    #endregion

    #region Flashlight Events

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Flashlight"))
        {
            isInLight = true;
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
        if (isDead) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Flashlight"))
        {
            isInLight = false;
            OnExitLight();
            if (visibilityModule != null)
            {
                visibilityModule.UpdateVisibility(player, isInLight, isInverted);
            }
        }
    }

    protected virtual void OnEnterLight() { }
    protected virtual void OnExitLight() { }

    #endregion

    #region Death Methods

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (dieAnimationModule != null)
        {
            dieAnimationModule.PlayDeathAnimation();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: EnemyDieAnimation 모듈이 없습니다!");
            OnDeathComplete();
        }
    }

    /// <summary>
    /// ✅ 사망 애니메이션 완료 후 호출 - 오브젝트 풀링 여부에 따라 처리
    /// </summary>
    private void OnDeathComplete()
    {
        if (useObjectPooling)
        {
            // 오브젝트 풀링 사용: 풀로 반환
            EnemySpawner.Instance?.ReturnEnemy(gameObject);
        }
        else
        {
            // 오브젝트 풀링 미사용: 즉시 파괴
            Destroy(gameObject);
        }
    }

    #endregion
}