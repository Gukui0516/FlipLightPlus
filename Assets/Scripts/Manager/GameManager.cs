using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Boot, Playing, Paused, GameOver }

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Header("self-Register Managers")]
    [SerializeField] private SceneManager sceneManager;
    [SerializeField] private WorldStateManager worldStateManager;
    [SerializeField] private UIManager uiManager;

    public SceneManager SceneManager => sceneManager;
    public WorldStateManager WorldStateManager => worldStateManager;
    public UIManager UIManager => uiManager;


    [Header("상태")]
    [SerializeField] private GameState current = GameState.Boot;
    public GameState Current => current;

    [Header("설정")]
    [SerializeField, Tooltip("게임 시작 시 타이틀로 진입할지")]
    private bool enterTitleOnBoot = true;

    [Header("스테이지")]
    [SerializeField, Tooltip("현재 스테이지 (1부터 시작)")]
    private int currentStage = 1;
    public int CurrentStage => currentStage;

    public bool IsPaused => current == GameState.Paused;



    [Header("UI 참조")]
    //public GameOverUI gameOverUI;

    [SerializeField, Tooltip("엔딩 진입 스테이지 번호")]
    private int endingStage = 4;
    
    public Action OnGameOver;


    private void Awake()
    {
        // Instance를 먼저 체크하되, 자신이 진짜인지 판단
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // 자신이 가짜라면 즉시 파괴하고 초기화 중단
            Destroy(gameObject);
            return; // ⭐ 중요: 이후 초기화 로직 실행 안함
        }

        // 진짜 GameManager만 여기까지 도달
        if (!sceneManager)
            sceneManager = GetComponent<SceneManager>();

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    private void Start()
    {
        if (enterTitleOnBoot) GoTitle();
        else StartNewGame();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 로드 시 참조 초기화
        ClearSceneReferences();
    }
    private void ClearSceneReferences()
    {
        uiManager = null;
        worldStateManager = null;
    }
    // ======= 공개 API =======

    /// <summary>
    /// 새 게임 시작 (스테이지 1부터)
    /// </summary>
    public void StartNewGame()
    {
        current = GameState.Boot;
        currentStage = 1;

        sceneManager.LoadStage(currentStage);
        current = GameState.Playing;
        Resume();
    }

    public void GoTitle()
    {
        current = GameState.Boot;
        currentStage = 1; // 타이틀로 돌아가면 스테이지도 초기화

        sceneManager.LoadTitle();
        Resume();
    }

    public void Restart()
    {
        StartNewGame();
    }

    /// <summary>
    /// 현재 스테이지 유지한 채 게임 씬 재로드
    /// </summary>
    public void ReloadStage()
    {
        current = GameState.Boot;

        sceneManager.LoadStage(currentStage);
        current = GameState.Playing;
        Resume();
    }

    /// <summary>
    /// 스테이지 +1 올리고 씬 로드
    /// - 단일 씬 모드: 같은 씬 재로드, currentStage만 증가
    /// - 다중 씬 모드: 다음 스테이지 씬으로 이동
    /// </summary>
    public void AdvanceStageAndReload()
    {
        currentStage++;

        if (currentStage >= endingStage)
        {
            PlayEnding();
            return;
        }

        sceneManager.LoadStage(currentStage);
        current = GameState.Playing;
    }

    public void Pause()
    {
        if (IsPaused) return;
        Time.timeScale = 0f;
        current = GameState.Paused;
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        if (current == GameState.Paused) current = GameState.Playing;
    }

    public void GameOver()
    {
        current = GameState.GameOver;
        Time.timeScale = 0f;

        //gameOverUI.Show();
        
        OnGameOver?.Invoke();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ======= 엔딩: 단순 즉시 표시 =======

    public void PlayEnding()
    {
        sceneManager.LoadEnding();
    }



/*
    기존 게임매니저는 인스펙터 연결이 힘들었고, 매니저를 비롯한 여러 클래스를 쉽게 접근하기 위해서
    게임매니저에 자기자신을 등록하여 게임매니저 중앙관리형으로 변경, 
    각 클래스들은 Awake에서 게임매니저에 자기자신을 등록해야 함. 
*/

#region Manager self-Register Methods

    
    public void RegisterWorldStateManager(WorldStateManager manager)
    {
        if (worldStateManager == null)
            worldStateManager = manager;
        else
            Debug.LogWarning("[GameManager] WorldStateManager가 이미 등록되어 있습니다.");
    }
    public void RegisterUIManager(UIManager manager)
    {
        if (uiManager == null)
            uiManager = manager;
        else
            Debug.LogWarning("[GameManager] UIManager가 이미 등록되어 있습니다.");
    }

#endregion
    public void GameOverUIActive()
    {
        UIManager.ShowUI("GameOverUI");
    }
}