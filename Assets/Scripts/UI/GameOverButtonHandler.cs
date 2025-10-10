using UnityEngine;

/// <summary>
/// GameOver UI 버튼용 간단한 헬퍼 클래스
/// 버튼의 OnClick 이벤트에 인스펙터에서 직접 연결해서 사용
/// </summary>
public class GameOverButtonHandler : MonoBehaviour
{
    /// <summary>
    /// 현재 스테이지 재시작
    /// </summary>
    public void RestartStage()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance가 null입니다!");
            return;
        }

        GameManager.Instance.ReloadStage();
    }

    /// <summary>
    /// 타이틀 씬으로 이동
    /// </summary>
    public void GoToTitle()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance가 null입니다!");
            return;
        }

        GameManager.Instance.GoTitle();
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance가 null입니다!");
            return;
        }

        GameManager.Instance.QuitGame();
    }
}