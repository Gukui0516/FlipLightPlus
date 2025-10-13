using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerLife : MonoBehaviour
{
    [SerializeField] public int startLife = 3;//시작 목숨
    [SerializeField] public int maxLife = 5;//최대 목숨
    public int currentLife;//현제체력
    [SerializeField] LifeUI lifeUI;//체력 UI
    [SerializeField] float invincibilityTime=2;//무적시간
    [SerializeField] float blinkTime = 0.1f;//깜박이는 시간
    [SerializeField] bool invincibile;//무적중
    [SerializeField] SpriteRenderer playerImage;//플레이어 이미지
    [SerializeField] CircleCollider2D circleCollider;//무적시 적 충돌 안함 위해서
    [SerializeField] FlashlightUpgradeManager flashLight;

    //[SerializeField] Flashlight2D flashlight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    bool isDead;
    Animator anim;

    // 외곽선용 SpriteRenderer 
    [SerializeField] GameObject outlineRenderer;


    [SerializeField] PlayerContact playerContact;//플레이어 접촉 스크립트
    [SerializeField] PlayerControllerRB playerController;//플레이어 컨트롤러
    
    
    void Start()
    {
        if (lifeUI == null)
        {
            lifeUI = FindFirstObjectByType<LifeUI>();
            if(lifeUI == null)
            {
                Debug.LogError("PlayerLife: LifeUI를 찾을 수 없음");
                return;
            }
        }
        currentLife=startLife;
        if(lifeUI!=null)
        lifeUI.LifeUIUpdate(currentLife);
        
        anim = GetComponent<Animator>();
        playerContact = GetComponent<PlayerContact>();
        flashLight.SetLevel(currentLife + 1, flashLight.CurrentUpgradeType);//손전등 업데이트
        playerController = GetComponent<PlayerControllerRB>();
    }

    public void LifeIncrease()
    {
        if (currentLife >= maxLife)//라이프 최대치 이상이면
        {
            return;//걍 끝냄
        }
        else
        {
            currentLife+=1;
            flashLight.SetLevel(currentLife + 1, flashLight.CurrentUpgradeType);//손전등 업데이트
        }
        if (lifeUI != null)
            lifeUI.LifeUIUpdate(currentLife);
    }
    
    public void LifeDecrease()
    {
        if (!invincibile)//무적이 아니면
        {
            if (currentLife < 0)//라이프가 0미만이면
            {
                playerImage.color = Color.black;//검은색으로 
                return;
            }
            else
            {
                currentLife -=1;
                flashLight.SetLevel(currentLife+1, flashLight.CurrentUpgradeType);//손전등 업데이트
            }
            if (lifeUI != null)
                lifeUI.LifeUIUpdate(currentLife);
            StartCoroutine(invincibilityTimes());//무적 시작
        }
        
        if(currentLife <= 0)//라이프가 0이하면
        {
            if (isDead) return;
            isDead = true;

            if (outlineRenderer) outlineRenderer.SetActive(false);

            // (정지 전에 대비하려면) 애니를 리얼타임으로 돌리기
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            playerController.CanMove = false;
            anim.SetBool("IsDead", true);          // 죽음 애니 트리거
            GameManager.Instance.GameOverUIActive();

        }
    }

    bool gameOverRequested; // 중복 방지

    // 애니메이션 이벤트로 호출
    public void OnDeathAnimFinished()
    {
        if (gameOverRequested) return;
        gameOverRequested = true;

        GameManager.Instance.GameOver();
    }



    private IEnumerator invincibilityTimes()
    {
        invincibile = true;//무적 온
        circleCollider.excludeLayers = LayerMask.GetMask("Enemy");
        float timer = 0f;
        while (timer < invincibilityTime) //무적 시간중이면
        {
            playerImage.color = playerImage.color == Color.white ? Color.black : Color.white;//플레이어 이미지 전환
            yield return new WaitForSeconds(blinkTime);//깜박임 시간동안 대기
            timer += blinkTime;
        }//반복 끝나면

        playerImage.color = Color.white;//흰색으로
        invincibile = false;//무적 오프
        circleCollider.excludeLayers = 0;
        //무적 시간이 끝났을 때 isContact를 false로 리셋
        if (playerContact != null)
        {
            playerContact.ResetContact();
        }
    }
}