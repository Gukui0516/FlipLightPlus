using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerLife : MonoBehaviour
{
    [SerializeField] public int startLife = 3;//시작 목숨
    [SerializeField] public int maxLife = 3;//최대 목숨
    public int currentLife;//현제체력
    [SerializeField] LifeUI lifeUI;//체력 UI
    [SerializeField] float invincibilityTime=2;//무적시간
    [SerializeField] float blinkTime = 0.1f;//깜박이는 시간
    [SerializeField] bool invincibile;//무적중
    [SerializeField] SpriteRenderer playerImage;//플레이어 이미지
    //[SerializeField] Flashlight2D flashlight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentLife=startLife;
        lifeUI.LifeUIUpdate(currentLife);
    }

    public void LifeIncrease()
    {
        if (currentLife >= maxLife)//라이프 최대치 이상이면
        {
            return;//걍 끝냄
        }
        else
        {
            currentLife++;
        }
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
                currentLife -= 1;
            }
            lifeUI.LifeUIUpdate(currentLife);
            StartCoroutine(invincibilityTimes());//무적 시작
        }
    }
    private IEnumerator invincibilityTimes()
    {
        invincibile = true;//무적 온
        float timer = 0f;
        while (timer < invincibilityTime) //무적 시간중이면
        {
            playerImage.color = playerImage.color == Color.white ? Color.black : Color.white;//플레이어 이미지 전환
            yield return new WaitForSeconds(blinkTime);//깜박임 시간동안 대기
            timer += blinkTime;
        }//반복 끝나면
        playerImage.color = Color.white;//흰색으로
        invincibile = false;//무적 오프
    }
}
