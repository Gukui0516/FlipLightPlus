using UnityEngine;

/// <summary>
/// Normal 타입 적 - 평소 움직이고 손전등에 멈춤
/// </summary>
public class NormalEnemy : BaseEnemy
{
    protected override void InitializeEnemy()
    {
        // Visibility 모듈 초기화
        if (visibilityModule != null)
        {
            visibilityModule.Initialize(EnemyType.Normal);
        }

        isInLight = false;
    }

    protected override bool ShouldMove()
    {
        // 손전등 없을 때만 움직임
        return !isInLight;
    }

    protected override bool ShouldRotate()
    {
        // 손전등 밖에서만 회전
        return !isInLight;
    }

    protected override float GetCurrentSpeed()
    {
        return speed;
    }

    public override void Die()
    {
        base.Die();
        // Normal 타입 특수 처리 (필요시)
    }
}