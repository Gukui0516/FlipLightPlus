using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[DisallowMultipleComponent]
public class PlayerDeath : MonoBehaviour
{
    [Header("Frames (slice된 스프라이트들 순서대로)")]
    [SerializeField] private Sprite[] frames;

    [Header("Playback")]
    [SerializeField, Min(1f)] private float fps = 18f;
    [SerializeField] private bool hideRendererOnStart = false;   // 평소엔 숨겨두고
    [SerializeField] private bool hideRendererAfterPlay = true;  // 끝나면 숨기기

    [Header("Refs")]
    [SerializeField] private SpriteRenderer target; // 비우면 자동으로 GetComponent

    [Header("Events")]
    public UnityEvent onFinished; // 재생 끝났을 때

    void Awake()
    {
        if (!target) target = GetComponent<SpriteRenderer>();
        if (hideRendererOnStart && target) target.enabled = false;
    }

    /// <summary>한 번만 재생(타임스케일 0 무시하고 리얼타임 기준)</summary>
    public void PlayOnce()
    {
        if (frames == null || frames.Length == 0 || !target)
        {
            Debug.LogWarning("[SpriteSequencePlayer] frames/target 없음");
            onFinished?.Invoke();
            return;
        }
        StopAllCoroutines();
        StartCoroutine(Co_PlayOnceRealtime());
    }

    IEnumerator Co_PlayOnceRealtime()
    {
        if (!target.enabled) target.enabled = true;

        float frameTime = 1f / fps;
        for (int i = 0; i < frames.Length; i++)
        {
            target.sprite = frames[i];
            yield return new WaitForSecondsRealtime(frameTime);
        }

        if (hideRendererAfterPlay) target.enabled = false;
        onFinished?.Invoke();
    }
}
