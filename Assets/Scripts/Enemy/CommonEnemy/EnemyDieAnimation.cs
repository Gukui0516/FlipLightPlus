using UnityEngine;
using System;

/// <summary>
/// 적의 죽음 애니메이션을 담당하는 모듈
/// </summary>
public class EnemyDieAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;

    [Header("Events")]
    public Action onDeathAnimationComplete; // 애니메이션 완료 이벤트

    private Rigidbody2D rb;
    private EnemyVisibility visibilityModule;

    // Animator 파라미터 해시
    private static readonly int DeathTriggerHash = Animator.StringToHash("Death");

    private void Awake()
    {
        // Animator 자동 할당
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        rb = GetComponent<Rigidbody2D>();
        visibilityModule = GetComponent<EnemyVisibility>();
    }

    /// <summary>
    /// 죽음 애니메이션 재생
    /// </summary>
    public void PlayDeathAnimation()
    {
        // 물리 정지
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // 충돌 비활성화
        }

        // Visibility 모듈 숨기기 (Eyes, Outline 등)
        if (visibilityModule != null)
        {
            visibilityModule.HideAll();
        }

        // 죽음 애니메이션 재생
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(DeathTriggerHash);
        }
        else
        {
            // Animator가 없거나 Controller가 없으면 바로 완료 이벤트 호출
            Debug.LogWarning($"{gameObject.name}: Animator 또는 Controller가 없어 죽음 애니메이션을 재생할 수 없습니다!");
            OnDeathAnimationComplete();
        }
    }

    /// <summary>
    /// 초기화 (풀에서 재사용 시)
    /// </summary>
    public void ResetAnimation()
    {
        // Rigidbody 다시 활성화
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        // Animator가 없거나 Controller가 없으면 스킵
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        // Animator 완전 초기화
        // 트리거 리셋
        animator.ResetTrigger(DeathTriggerHash);

        // Animator 상태를 Entry(Idle)로 강제 이동
        animator.Rebind();
        animator.Update(0f);

        // Animator 활성화
        animator.enabled = true;
    }

    /// <summary>
    /// 애니메이션 이벤트로 호출됨 (Animation Event)
    /// EnemyDeath 애니메이션 클립의 마지막 프레임에 이 함수를 추가
    /// </summary>
    public void OnDeathAnimationComplete()
    {
        // Rigidbody 다시 활성화 (풀 반환 전 정리)
        if (rb != null)
        {
            rb.simulated = true;
        }

        // 완료 이벤트 발생 (BaseEnemy에서 구독)
        onDeathAnimationComplete?.Invoke();
    }

    /// <summary>
    /// Animator가 있는지 확인
    /// </summary>
    public bool HasAnimator()
    {
        return animator != null;
    }
}