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
    
    public string GetStageSceneName(int stageIndex)
    {
        if (stageScenes == null || stageIndex < 0 || stageIndex >= stageScenes.Length)
            return string.Empty;
        return stageScenes[stageIndex].SceneName;
    }

    // 씬 로드 메서드들
    public void LoadTitle() => LoadByName(TitleSceneName);
    public void LoadEnding() => LoadByName(EndingSceneName);
    public void LoadStage(int stageIndex) => LoadByName(GetStageSceneName(stageIndex));

    public void LoadByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) 
        {
            Debug.LogWarning($"SceneManager: 빈 씬 이름으로 로드를 시도했습니다.");
            return;
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
