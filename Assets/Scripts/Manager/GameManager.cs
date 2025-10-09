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
    [SerializeField, Tooltip("현재 스테이지. 새 게임은 1부터 시작")]
    private int currentStage = 1;
    public int CurrentStage => currentStage;

    public bool IsPaused => current == GameState.Paused;

    

    [Header("UI 참조")]
    //public GameOverUI gameOverUI;

    [SerializeField] int endingStage = 4;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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

    public void StartNewGame()
    {
        current = GameState.Boot;
        currentStage = 0;



        sceneManager.LoadStage(0);
        current = GameState.Playing;
        Resume();
    }

    public void GoTitle()
    {
        current = GameState.Boot;

        sceneManager.LoadTitle();
        Resume();
    }

    public void Restart()
    {
        StartNewGame();
    }

    // 현재 스테이지 유지한 채 게임 씬 재로드
    public void ReloadStage()
    {
        current = GameState.Boot;

        sceneManager.LoadStage(currentStage - 1);
        current = GameState.Playing;
        Resume();
    }

    // 스테이지 +1 올리고 같은 게임 씬 재로드
    // 예: 3 클리어 → 호출되면 currentStage=4 → 즉시 엔딩 표시
    public void AdvanceStageAndReload()
    {
        currentStage = Mathf.Max(1, currentStage);

        if (currentStage >= endingStage)
        {
            PlayEnding();
            return;
        }

        sceneManager.LoadStage(currentStage - 1);
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
        UIManager.ShowUI("GameOverUI");
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
    
}
