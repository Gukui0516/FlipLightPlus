using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ImprovedVisionCone 사용 예제
/// 타겟 감지 이벤트 처리 및 활용 방법
/// </summary>
public class VisionConeUsageExample : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private ImprovedVisionCone visionCone;
    
    [Header("타겟 처리 설정")]
    [SerializeField] private Color detectedColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;
    
    void Start()
    {
        if (visionCone == null)
        {
            visionCone = GetComponent<ImprovedVisionCone>();
        }
        
        // 이벤트 구독
        SubscribeToEvents();
    }

    void SubscribeToEvents()
    {
        // 1️⃣ 타겟 진입 이벤트
        visionCone.OnTargetEnter += HandleTargetEnter;
        
        // 2️⃣ 타겟 이탈 이벤트
        visionCone.OnTargetExit += HandleTargetExit;
        
        // 3️⃣ 매 프레임 전체 타겟 업데이트 이벤트
        visionCone.OnVisibleTargetsUpdate += HandleVisibleTargetsUpdate;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (visionCone != null)
        {
            visionCone.OnTargetEnter -= HandleTargetEnter;
            visionCone.OnTargetExit -= HandleTargetExit;
            visionCone.OnVisibleTargetsUpdate -= HandleVisibleTargetsUpdate;
        }
    }

    // ========== 이벤트 핸들러 ==========

    /// <summary>
    /// 타겟이 빛 영역에 진입했을 때
    /// </summary>
    void HandleTargetEnter(Transform target)
    {
        Debug.Log($"🔦 [{target.name}] 빛에 들어옴!");
        
        // 예제 1: 색상 변경
        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = detectedColor;
        }
        
        // 예제 2: 적 AI 알림
        EnemyAI enemy = target.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.OnSpotted();
        }
        
        // 예제 3: 파티클 효과
        PlayDetectionEffect(target.position);
    }

    /// <summary>
    /// 타겟이 빛 영역에서 이탈했을 때
    /// </summary>
    void HandleTargetExit(Transform target)
    {
        Debug.Log($"🌑 [{target.name}] 빛에서 벗어남!");
        
        // 예제 1: 색상 복구
        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = normalColor;
        }
        
        // 예제 2: 적 AI 알림
        EnemyAI enemy = target.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.OnLostSight();
        }
    }

    /// <summary>
    /// 매 프레임 모든 보이는 타겟 처리
    /// </summary>
    void HandleVisibleTargetsUpdate(HashSet<Transform> visibleTargets)
    {
        // 예제 1: UI 업데이트
        UpdateDetectionUI(visibleTargets.Count);
        
        // 예제 2: 모든 타겟에게 지속 효과
        foreach (var target in visibleTargets)
        {
            if (target != null)
            {
                // 예: 지속 데미지, 슬로우 등
                ApplyContinuousEffect(target);
            }
        }
    }

    // ========== 보조 메서드 ==========

    void PlayDetectionEffect(Vector2 position)
    {
        // 파티클 효과 예제
        // ParticleSystem effect = Instantiate(detectionEffectPrefab, position, Quaternion.identity);
//        Debug.Log($"💥 감지 이펙트: {position}");
    }

    void UpdateDetectionUI(int count)
    {
        // UI 업데이트 예제
//        Debug.Log($"📊 현재 감지된 타겟: {count}개");
    }

    void ApplyContinuousEffect(Transform target)
    {
        // 지속 효과 예제
        // 예: 빛에 노출되면 초당 5 데미지
        // Health health = target.GetComponent<Health>();
        // if (health != null)
        // {
        //     health.TakeDamage(5 * Time.deltaTime);
        // }
    }

    // ========== 수동 체크 예제 ==========

    void Update()
    {
        // 예제 1: 특정 타겟이 보이는지 체크
        // if (specificTarget != null && visionCone.IsTargetVisible(specificTarget))
        // {
        //     Debug.Log("특정 타겟이 보입니다!");
        // }
        
        // 예제 2: 특정 위치가 빛 영역 안인지 체크
        // Vector2 checkPosition = new Vector2(5, 3);
        // if (visionCone.IsPositionVisible(checkPosition))
        // {
        //     Debug.Log("해당 위치가 빛 영역 안입니다!");
        // }
    }

    // ========== 특정 프레임 처리 예제 ==========

    /// <summary>
    /// 특정 조건에서 모든 보이는 타겟 일괄 처리
    /// 예: 스킬 사용 시 빛 영역 내 모든 적에게 데미지
    /// </summary>
    public void ExecuteFlashlightAttack()
    {
        visionCone.ProcessAllVisibleTargets(target =>
        {
            Health health = target.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(50f);
                Debug.Log($"⚡ [{target.name}]에게 섬광 공격!");
            }
        });
    }
}

// ========== 더미 클래스 (예제용) ==========

public class EnemyAI : MonoBehaviour
{
    public void OnSpotted()
    {
        Debug.Log($"🚨 [{gameObject.name}] 적이 플레이어를 발견했습니다!");
        // 추격 시작, 경보 등
    }
    
    public void OnLostSight()
    {
        Debug.Log($"❓ [{gameObject.name}] 적이 시야를 잃었습니다!");
        // 탐색 모드, 마지막 위치로 이동 등
    }
}

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    
    void Start()
    {
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"💔 [{gameObject.name}] 데미지: {damage}, 남은 체력: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log($"💀 [{gameObject.name}] 사망!");
        Destroy(gameObject);
    }
}