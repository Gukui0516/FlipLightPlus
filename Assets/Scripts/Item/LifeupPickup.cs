using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LifeupPickup : MonoBehaviour, Item
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    
    private bool consumed = false;

    /// <summary>스포너/팩토리에서 주입</summary>
    public void Init(WorldStateManager manager)
    {
        // 이 아이템은 WorldStateManager 필요 없음 (참고용으로 남겨둠)
    }

    private void OnEnable()
    {
        consumed = false; // 풀에서 다시 꺼낼 때 리셋
    }

    /// <summary>
    /// 스포너가 연결해줄 수 있는 콜백.
    /// 예) pickup.onConsumed = () => ReleaseItem(item);
    /// </summary>
    public Action onConsumed;

    public void ActiveItem()
    {
        if (consumed) return; // 이미 소비된 아이템
        consumed = true;

        // 핵심 기능
        Debug.Log("라이프 업");
        //플래시라이트 업그레이드타입 가져와서 1레벨업
        var playerLife = FindFirstObjectByType<PlayerLife>();
        if (playerLife != null)
        {
            playerLife.LifeIncrease();
        }
        else
        {
            Debug.LogWarning("[LifeUPItem] playerLife 찾을 수 없음");
        }

        // PooledItem이 있으면 그것만 사용
        var pooled = GetComponent<PooledItem>();
        if (pooled != null)
        {
            pooled.ReturnToPoolNow();
            return;
        }

        // PooledItem이 없을 때만 onConsumed 직접 호출
        onConsumed?.Invoke();

        // 마지막 폴백: 그냥 비활성화
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}