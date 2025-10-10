using UnityEngine;

public class PlayerContact : MonoBehaviour
{
    [SerializeField] CircleCollider2D checkRadius;
    [SerializeField] PlayerLife playerLife;
    bool isContact = false;
    public LayerMask enemyLayer;
    bool isInverted = false;
    private WorldStateManager worldStateManager;
    
    private void Awake()
    {
        isContact = false;
        worldStateManager = FindFirstObjectByType<WorldStateManager>();
        if (worldStateManager == null)
        {
            Debug.LogError("WorldStateManager not found");
        }
    }
    
    private void Start()
    {
        isContact = false;
        if (worldStateManager == null) worldStateManager = FindFirstObjectByType<WorldStateManager>();
        if (worldStateManager == null)
        {
            Debug.LogError("WorldStateManager not found");
        }
    }
    
    //무적 시간 종료 시 isContact 리셋하는 메서드
    public void ResetContact()
    {
        isContact = false;
    }
    
    public void CheckContact()//체크할 태그 이름
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius.radius);
        foreach (Collider2D hit in hits)
        {
            

            if (hit.gameObject.CompareTag("Item"))
            {
                Debug.Log("아이템 " + hit.name + " 획득");
                Item item = hit.GetComponent<Item>();
                if (item != null)
                {
                    item.ActiveItem();
                }
                else
                {
                    Debug.Log("아이템 연결 안됨");
                }
                //아이템 효과 발동 시키는 코드
            }
            //Debug.Log(isContact + " " + hit.gameObject.layer + " " + enemyLayer);
            //if (isContact) break;
            // 올바른 레이어 비교 방식
            if ((enemyLayer.value & (1 << hit.gameObject.layer)) != 0)
            {
                playerLife.LifeDecrease();
                Debug.Log(hit.gameObject.name);
                isContact = true;
            }

            
        }
    }
    
    private void Update()
    {
        CheckContact();
    }
}