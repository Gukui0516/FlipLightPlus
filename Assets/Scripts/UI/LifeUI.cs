using UnityEngine;
using UnityEngine.UI;

public class LifeUI : MonoBehaviour
{
    [SerializeField] Image[] lifeImage;//체력 이미지
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void LifeUIUpdate(int currentLife)
    {
        for (int i = 0; i < lifeImage.Length; i++)
        {
            if (currentLife <= i)//현제 목숨이 지금 반복 수 이하인 경우
            {
                lifeImage[i].enabled = false;//비활성화
            }
            else
            {
                lifeImage[i].enabled = true;//활성화
            }
        }
    }
}
