using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public class UIElement
    {
        public string key;
        public GameObject uiObject;
        public bool startActive = false;
    }

    [SerializeField]
    private List<UIElement> uiElements = new List<UIElement>();

    private Dictionary<string, GameObject> uiDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {
        InitializeUI();
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterUIManager(this);
    }
    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterUIManager(this);
    }

    private void InitializeUI()
    {
        // 리스트를 Dictionary로 변환
        uiDictionary.Clear();
        foreach (var element in uiElements)
        {
            if (!string.IsNullOrEmpty(element.key) && element.uiObject != null)
            {
                if (!uiDictionary.ContainsKey(element.key))
                {
                    uiDictionary.Add(element.key, element.uiObject);
                    if(element.startActive)
                        element.uiObject.SetActive(true);
                    else
                        element.uiObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning($"중복된 UI 키: {element.key}");
                }
            }
        }
        

        
    }

    // UI 활성화
    public void ShowUI(string key)
    {
        if (uiDictionary.ContainsKey(key))
        {
            uiDictionary[key].SetActive(true);
        }
        else
        {
            Debug.LogWarning($"UI를 찾을 수 없음: {key}");
        }
    }

    // UI 비활성화
    public void HideUI(string key)
    {
        if (uiDictionary.ContainsKey(key))
        {
            uiDictionary[key].SetActive(false);
        }
        else
        {
            Debug.LogWarning($"UI를 찾을 수 없음: {key}");
        }
    }

    // UI 토글
    public void ToggleUI(string key)
    {
        if (uiDictionary.ContainsKey(key))
        {
            GameObject ui = uiDictionary[key];
            ui.SetActive(!ui.activeSelf);
        }
        else
        {
            Debug.LogWarning($"UI를 찾을 수 없음: {key}");
        }
    }

    // UI 활성화 상태 확인
    public bool IsUIActive(string key)
    {
        if (uiDictionary.ContainsKey(key))
        {
            return uiDictionary[key].activeSelf;
        }
        Debug.LogWarning($"UI를 찾을 수 없음: {key}");
        return false;
    }

    // 모든 UI 비활성화
    public void HideAllUI()
    {
        foreach (var ui in uiDictionary.Values)
        {
            ui.SetActive(false);
        }
    }
}