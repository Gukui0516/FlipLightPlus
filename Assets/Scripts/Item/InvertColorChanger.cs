using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class InvertColorChanger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldStateManager worldStateManager;

    [Header("Auto-find on this GameObject")]
    [Tooltip("비활성화하면 수동으로 등록된 컴포넌트만 사용")]
    [SerializeField] private bool autoFindComponents = true;

    [Header("Additional GameObjects")]
    [Tooltip("색상 변경을 적용할 추가 GameObject 리스트 (자식 오브젝트 등)")]
    [SerializeField] private GameObject[] additionalObjects;

    private IInvertibleColor[] invertibleComponents;
    private bool currentlyInverted = false;

    void Awake()
    {
        // WorldStateManager 자동 탐색
        if (!worldStateManager)
            worldStateManager = FindFirstObjectByType<WorldStateManager>();

        if (!worldStateManager)
        {
            Debug.LogError("InvertColorChanger: WorldStateManager를 찾을 수 없습니다!", this);
            enabled = false;
            return;
        }

        // IInvertibleColor 구현체 수집
        CollectInvertibleComponents();
    }

    void OnEnable()
    {
        if (worldStateManager)
        {
            worldStateManager.onIsInvertedChanged.AddListener(OnInvertedChanged);
            // 현재 상태로 초기화
            OnInvertedChanged(worldStateManager.IsInverted);
        }
    }

    void OnDisable()
    {
        if (worldStateManager)
            worldStateManager.onIsInvertedChanged.RemoveListener(OnInvertedChanged);
    }

    private void CollectInvertibleComponents()
    {
        List<IInvertibleColor> componentList = new List<IInvertibleColor>();

        // 1. 현재 GameObject에서 자동 수집
        if (autoFindComponents)
        {
            IInvertibleColor[] localComponents = GetComponents<IInvertibleColor>();
            if (localComponents != null && localComponents.Length > 0)
                componentList.AddRange(localComponents);
        }

        // 2. 추가 GameObject들에서 수집
        if (additionalObjects != null && additionalObjects.Length > 0)
        {
            foreach (var obj in additionalObjects)
            {
                if (obj == null) continue;

                IInvertibleColor[] components = obj.GetComponents<IInvertibleColor>();
                if (components != null && components.Length > 0)
                    componentList.AddRange(components);
            }
        }

        invertibleComponents = componentList.ToArray();

        if (invertibleComponents.Length == 0)
            Debug.LogWarning("InvertColorChanger: IInvertibleColor를 구현한 컴포넌트를 찾을 수 없습니다.", this);
    }

    private void OnInvertedChanged(bool isInverted)
    {
        if (currentlyInverted == isInverted) return;
        currentlyInverted = isInverted;

        if (invertibleComponents == null || invertibleComponents.Length == 0)
            return;

        foreach (var component in invertibleComponents)
        {
            if (component == null) continue;

            if (isInverted)
                component.SetInvertedColor();
            else
                component.SetOriginalColor();
        }
    }
}