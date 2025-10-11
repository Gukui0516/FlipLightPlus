using UnityEngine;

/// <summary>
/// 타이틀 씬 전용 탈출구
/// 플레이어가 진입하면 스테이지 1로 새 게임을 시작합니다
/// </summary>
public class TitleSceneExitDoor : ExitDoorController
{
    [Header("타이틀 씬 전용 설정")]
    [SerializeField] private bool showDebugLogs = true;
    
    /// <summary>
    /// 플레이어가 문에 진입했을 때 호출
    /// 스테이지를 증가시키지 않고 새 게임을 시작합니다
    /// </summary>
    protected override void OnPlayerEscape()
    {
        if (_hasPlayerEscaped)
            return;
        
        _hasPlayerEscaped = true;
        
        if (showDebugLogs)
            Debug.Log("TitleSceneExitDoor: 플레이어가 게임을 시작합니다!");
        
        if (GameManager.Instance != null)
        {
            // 스테이지를 올리는 대신 새 게임 시작
            GameManager.Instance.StartNewGame();
        }
        else
        {
            Debug.LogError("TitleSceneExitDoor: GameManager.Instance를 찾을 수 없습니다!");
        }
    }
}