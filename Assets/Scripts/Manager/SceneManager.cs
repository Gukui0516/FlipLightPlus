using UnityEngine;
using UnityEngine.SceneManagement;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct SceneReference
{
    [SerializeField, Tooltip("에디터에서만 보이는 씬 에셋 참조")]
    private UnityEngine.Object sceneAsset;

    [SerializeField, Tooltip("런타임에서 실제로 로드할 씬 이름(자동 캐싱)")]
    private string sceneName;

    public string SceneName => sceneName;

#if UNITY_EDITOR
    // 인스펙터에서 씬 드롭 시 자동으로 이름 캐싱
    public void OnValidate()
    {
        if (sceneAsset != null)
        {
            var path = AssetDatabase.GetAssetPath(sceneAsset);
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            sceneName = asset != null ? asset.name : string.Empty;
        }
        else
        {
            sceneName = string.Empty;
        }
    }
#endif
}


public class SceneManager : MonoBehaviour
{
    [Header("고정 씬 참조")]
    [SerializeField] private SceneReference titleScene;
    [SerializeField] private SceneReference endingScene;

    [Header("스테이지 씬 리스트")]
    [SerializeField] private SceneReference[] stageScenes;

    [Header("스테이지 시스템 타입")]
    [SerializeField, Tooltip("true: 같은 씬에서 스테이지만 증가 | false: 스테이지별 다른 씬")]
    private bool useSingleStageScene = true;

    public bool UseSingleStageScene => useSingleStageScene;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // SceneReference 내부 캐싱 갱신
        titleScene.OnValidate();
        endingScene.OnValidate();
        
        // 스테이지 씬들도 캐싱 갱신
        if (stageScenes != null)
        {
            for (int i = 0; i < stageScenes.Length; i++)
            {
                stageScenes[i].OnValidate();
            }
        }
    }
#endif

    public string TitleSceneName => titleScene.SceneName;
    public string EndingSceneName => endingScene.SceneName;
    
    // 스테이지 관련 프로퍼티
    public int StageCount => stageScenes?.Length ?? 0;
    
    /// <summary>
    /// 스테이지 씬 이름 반환 (1-based 인덱스)
    /// </summary>
    public string GetStageSceneName(int stage)
    {
        if (stageScenes == null || stageScenes.Length == 0)
        {
            Debug.LogError("SceneManager: 스테이지 씬이 설정되지 않았습니다!");
            return string.Empty;
        }

        // 단일 씬 반복 모드
        if (useSingleStageScene)
        {
            return stageScenes[0].SceneName;
        }

        // 다중 씬 모드: 1-based stage를 0-based 배열 인덱스로 변환
        int arrayIndex = stage - 1;
        
        if (arrayIndex < 0 || arrayIndex >= stageScenes.Length)
        {
            Debug.LogError($"SceneManager: 유효하지 않은 스테이지 번호입니다. (Stage: {stage}, 배열 크기: {stageScenes.Length})");
            return string.Empty;
        }
        
        return stageScenes[arrayIndex].SceneName;
    }

    // 씬 로드 메서드들
    public void LoadTitle() => LoadByName(TitleSceneName);
    public void LoadEnding() => LoadByName(EndingSceneName);
    
    /// <summary>
    /// 스테이지 로드 (1-based 인덱스)
    /// </summary>
    public void LoadStage(int stage) => LoadByName(GetStageSceneName(stage));

    public void LoadByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) 
        {
            Debug.LogWarning($"SceneManager: 빈 씬 이름으로 로드를 시도했습니다.");
            return;
        }
        
        Debug.Log($"SceneManager: '{sceneName}' 씬을 로드합니다.");
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}