using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// LightSeeker 타입 적 - 손전등 비춰질 때만 움직임 (평소), 반전시 패턴 변경
/// </summary>
/// enumv
enum state
{
    Sleep,Wake,Rush
}
public class AgwiEnemy : BaseEnemy
{
    [SerializeField] state state=state.Sleep;
    [Header("LightSeeker Speed Settings")]
    [SerializeField] private float baseSpeed = 2.0f;
    [SerializeField] private float detectionRange = 8.0f;
    [SerializeField] private Transform eyebrows;
    [SerializeField] private bool wakeCondition;//기상 조건(가깝거나 빛에 닿거나)
    [SerializeField] private float rushDelay = 1f;//돌진 딜레이
    [SerializeField] private float wakeSpeed = 0.8f;//눈 뜨는 속도
    [SerializeField] private float sleepSpeed = 0.4f;//눈 감는 속도

    private float currentSpeed;
    private float timeInLight = 0f;

    protected override void Awake()
    {
        base.Awake();
        currentSpeed = baseSpeed;
    }

    protected override void InitializeEnemy()
    {
        // Visibility 모듈 초기화
        if (visibilityModule != null)
        {
            visibilityModule.Initialize(EnemyType.Agwi);
        }
        // 속도 초기화
        currentSpeed = baseSpeed;
        timeInLight = 0f;
        isInLight = false;
    }

    protected override void Update()
    {
        base.Update();
        switch (state)
        {
            case state.Sleep:
                if (Vector2.Distance(transform.position, player.transform.position) <= detectionRange || isInLight)//감지거리 안이라면
                {
                    eyebrows.localScale = new Vector2(eyebrows.localScale.x-wakeSpeed, eyebrows.localScale.y);//눈썹 크기 조절

                    if (eyebrows.localScale.x <= 0)//눈을 다 뜬 상태라면
                    {
                        state = state.Wake;//일어남
                    }
                }
                else
                {
                    eyebrows.localScale = new Vector2(eyebrows.localScale.x + sleepSpeed, eyebrows.localScale.y);//눈썹 크기 조절
                }
                break;
            case state.Wake:
                if ( < 0f)
                {
                    timeInLight = 0f;
                }
                break;
        }
        // 속도 증가 로직
    }

    protected override bool ShouldMove()
    {        
        if (state==state.Rush)//돌진 상태가 아니면
        {

            //이동 코드
            return false;//이동 방향이 고정되어야 하기 때문에 베이스 코드 안거치고 그냥 이동시킴
        }

        // 평소: 돌진 안함
        return false;
    }

    protected override bool ShouldRotate()
    {
        // 반전 상태: Normal처럼 손전등 밖에서만 회전
        if (state == state.Rush)//돌진 상태가 아닐때 회전 안함
        {
            return false;
        }

        // 평소: 항상 회전
        return true;
    }

    protected override bool IsStoppedByInversion()
    {
        // LightSeeker는 반전 상태에서도 계속 움직임 (패턴만 변경)
        return false;
    }

    protected override float GetCurrentSpeed()
    {
        return currentSpeed;
    }


    protected override void OnEnterLight()
    {

    }

    protected override void OnExitLight()
    {

    }


    public override void Die()
    {
        base.Die();
    }
}