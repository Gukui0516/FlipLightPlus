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
    [SerializeField] private float detectionRange = 8.0f;
    [SerializeField] private Transform eyebrows;
    [SerializeField] private GameObject wakeNotice;//일어난거 알림할 오브젝트(아이템 오프할거임)
    [SerializeField] private float eyebrowMaxSize;//눈썹 최대 길이
    [SerializeField] private float wakeDelay = 0f;//돌진 딜레이 누적치
    [SerializeField] private float wakeDelayMax = 1f;//돌진 딜레이 최대치
    [SerializeField] private float wakeSpeed = 0.4f;//눈 뜨는 속도
    [SerializeField] private float sleepSpeed = 0.2f;//눈 감는 속도

    private float timeInLight = 0f;

    protected override void Awake()
    {
        eyebrowMaxSize = eyebrows.localScale.x;
        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        // Visibility 모듈 초기화
        if (visibilityModule != null)
        {
            visibilityModule.Initialize(EnemyType.Agwi);
        }
        // 속도 초기화
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
                    eyebrows.localScale = new Vector2(eyebrows.localScale.x-wakeSpeed*Time.deltaTime, eyebrows.localScale.y);//눈썹 크기 조절
                    
                    if (eyebrows.localScale.x <= 0)//눈을 다 뜬 상태라면
                    {
                        wakeNotice.SetActive(false);
                        state = state.Wake;//일단 대기상태
                    }
                }
                else
                {
                    eyebrows.localScale = new Vector2(eyebrows.localScale.x + sleepSpeed * Time.deltaTime, eyebrows.localScale.y);//눈썹 크기 조절

                    if (eyebrows.localScale.x >= eyebrowMaxSize)//눈썹 최대 크기 넘기면
                    {
                        eyebrows.localScale = new Vector2(eyebrowMaxSize, eyebrows.localScale.y);
                    }
                }
            break;
            case state.Wake:
                if (wakeDelay < wakeDelayMax)
                {
                    wakeDelay += Time.deltaTime;//딜레이 다 될까지 참는다
                }
                else
                {
                    state=state.Rush;//돌진ㄱ
                }
            break;
        }
    }
    

    protected override bool ShouldMove()
    {        
        if (state==state.Rush)//돌진 상태면
        {
            rb.freezeRotation = true;//각도 고정
            return true;//이동
        }

        // 평소: 돌진 안함
        return false;
    }

    protected override bool ShouldRotate()
    {
        // 반전 상태: Normal처럼 손전등 밖에서만 회전
        if (state == state.Rush)//돌진 상태일 때 회전 안함
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
        return speed;
    }


    public override void Die()
    {
        base.Die();
    }
}