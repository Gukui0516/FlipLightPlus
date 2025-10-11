// C:/Users/jungle/Documents/GitHub/W5PlusTeam1/Assets/Scripts\Enemy\CommonEnemy\BaseEnemy.cs

using UnityEngine;
using UnityEngine.AI; // NavMeshAgent를 사용하기 위해 추가

/// <summary>
/// 모든 적의 기본 클래스 - 공통 기능 제공
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // NavMeshAgent를 사용하더라도 Rigidbody는 있는 것이 좋음
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
    protected bool isDead = false;

    // 컴포넌트 모듈들
    protected EnemyMovement movementModule;
    protected EnemyRotation rotationModule;
    protected EnemyVisibility visibilityModule;
    protected EnemyDespawn despawnModule;
    protected EnemyDieAnimation dieAnimationModule;
    protected Rigidbody2D rb;
    protected NavMeshAgent agent; // NavMeshAgent 참조 추가

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
            agent.updateRotation = false; // 회전은 EnemyRotation.cs에서 처리
            agent.updateUpAxis = false;   // 2D 환경에서는 Z축 사용 안 함
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
            // 위치가 유효한지 확인 후 워프
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
        // NavMeshAgent 비활성화
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

        // 월드 인버전 등으로 정지해야 한다면 즉시 제동
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
            // 손전등 조건 등으로 '멈춤' 판정일 때도 관성 없이 즉시 정지
            movementModule?.StopImmediate();
        }
    }

    /// <summary>
    /// NavMeshAgent를 사용한 이동 처리
    /// </summary>
    private void HandleNavMeshMovement()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (ShouldMove() && player != null)
        {
            agent.isStopped = false;
            agent.speed = GetCurrentSpeed();
            agent.stoppingDistance = stoppingDistance;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
            // 멈출 때 관성을 없애기 위해 속도를 직접 0으로 설정
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    /// <summary>
    /// 기존 Rigidbody를 사용한 이동 처리 (AgwiEnemy 등)
    /// </summary>
    private void HandleRigidbodyMovement()
    {
        if (ShouldMove())
        {
            if (movementModule != null)
            {
                movementModule.MoveTowardsPlayer(player, GetCurrentSpeed(), stoppingDistance);
            }
        }
        else
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    #endregion

    #region Abstract & Virtual Methods

    protected abstract bool ShouldMove();
    protected abstract bool ShouldRotate();
    protected abstract float GetCurrentSpeed();
    protected abstract void InitializeEnemy();

    /// <summary>
    /// 이 적이 NavMeshAgent를 사용하는지 여부를 반환. (AgwiEnemy처럼 사용하지 않는 경우 false를 반환하도록 오버라이드)
    /// </summary>
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

    // ... (OnTriggerEnter2D, OnTriggerExit2D, OnEnterLight, OnExitLight) 기존 코드 유지 ...
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

    private void OnDeathComplete()
    {
        EnemySpawner.Instance?.ReturnEnemy(gameObject);
    }

    #endregion
}