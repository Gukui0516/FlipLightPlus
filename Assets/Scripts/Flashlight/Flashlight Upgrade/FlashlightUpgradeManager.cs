using UnityEngine;
using System.Collections;

/// <summary>
/// 손전등 업그레이드 관리 시스템
/// - 레벨업/다운 처리
/// - 임시/씬/영구 업그레이드 타입 지원
/// - ImprovedVisionCone과 연동
/// - 정적 변수로 씬 전환에도 데이터 유지 (GameManager 독립적)
/// - UI 표시 기능 추가
/// </summary>
[RequireComponent(typeof(ImprovedVisionCone))]
public class FlashlightUpgradeManager : MonoBehaviour
{
    /// <summary>
    /// 업그레이드 타입
    /// </summary>
    public enum UpgradeType
    {
        Temporary,      // 잠시 (지정된 시간 후 복귀)
        ScenePersistent, // 씬 단위 (씬이 바뀌면 리셋)
        Permanent       // 영구 (게임 재시작 시에만 리셋)
    }
    
    // ========== 정적 변수 (씬 전환에도 유지) ==========
    private static int savedLevel = 1;
    private static UpgradeType savedUpgradeType = UpgradeType.ScenePersistent;
    private static bool isStaticDataInitialized = false;
    
    [Header("참조")]
    [SerializeField] private FlashlightUpgradeData upgradeData;
    
    [Header("UI 설정")]
    [Tooltip("UIManager에 등록된 UI 키")]
    [SerializeField] private string upgradeUIKey = "FlashlightUpgradeUI";
    
    [Header("현재 상태")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private UpgradeType currentUpgradeType = UpgradeType.ScenePersistent;
    public UpgradeType CurrentUpgradeType => currentUpgradeType;
    
    [Header("임시 업그레이드 설정")]
    [Tooltip("임시 업그레이드 지속 시간 (초)")]
    [SerializeField] private float temporaryUpgradeDuration = 10f;
    
    // 컴포넌트
    private ImprovedVisionCone visionCone;
    private UIManager uiManager;
    
    // 내부 변수
    private FlashlightLevel baseLevel; // 기본 레벨 (복귀용)
    private Coroutine temporaryUpgradeCoroutine;
    private bool isTemporaryUpgradeActive = false;
    
    // 이전 레벨 정보 (UI 표시용)
    private float previousAngle;
    private float previousRadius;
    
    // 이벤트
    public System.Action<int> OnLevelChanged;
    public System.Action<int, int> OnLevelUp; // oldLevel, newLevel
    public System.Action<int> OnMaxLevelReached;
    
    void Awake()
    {
        visionCone = GetComponent<ImprovedVisionCone>();
        
        if (upgradeData == null)
        {
            Debug.LogError("[FlashlightUpgradeManager] FlashlightUpgradeData가 할당되지 않았습니다!");
            enabled = false;
            return;
        }
        
        // 최초 1회만 정적 데이터 초기화 (게임 시작 시)
        if (!isStaticDataInitialized)
        {
            LoadFromPlayerPrefs();
            isStaticDataInitialized = true;
            
            // 씬 로드 이벤트 구독 (ScenePersistent 리셋용)
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        // 기본 레벨 저장
        baseLevel = upgradeData.GetDefaultLevel();
        
        // 이전 각도/반지름 초기화
        previousAngle = baseLevel.viewAngle;
        previousRadius = baseLevel.viewRadius;
    }
    
    void Start()
    {
        // UIManager 찾기
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("[FlashlightUpgradeManager] UIManager를 찾을 수 없습니다. UI 표시가 비활성화됩니다.");
        }
        
        // 정적 변수로부터 레벨 복원
        currentLevel = savedLevel;
        currentUpgradeType = savedUpgradeType;
        
        Debug.Log($"[FlashlightUpgradeManager] 레벨 복원: {currentLevel} (타입: {currentUpgradeType})");
        
        // 현재 레벨 적용 (UI 표시 없이)
        ApplyLevel(currentLevel, false);
        
        Debug.Log($"[FlashlightUpgradeManager] 초기화 완료 - 현재 레벨: {currentLevel}/{upgradeData.GetMaxLevel()}");
    }
    
    void OnDestroy()
    {
        // 마지막 인스턴스가 파괴될 때만 이벤트 구독 해제
        if (FindObjectsByType<FlashlightUpgradeManager>(FindObjectsSortMode.None).Length <= 1)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    /// <summary>
    /// 씬 로드 시 호출 (ScenePersistent 리셋용)
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // ScenePersistent 타입이면 레벨 1로 리셋
        if (savedUpgradeType == UpgradeType.ScenePersistent)
        {
            Debug.Log("[FlashlightUpgradeManager] 씬 전환: ScenePersistent 모드로 레벨 1로 리셋");
            savedLevel = 1;
            savedUpgradeType = UpgradeType.ScenePersistent;
        }
    }
    
    /// <summary>
    /// 레벨업 (amount만큼 증가)
    /// </summary>
    public bool LevelUp(int amount = 1, UpgradeType upgradeType = UpgradeType.ScenePersistent, float duration = 0f)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[FlashlightUpgradeManager] amount는 1 이상이어야 합니다.");
            return false;
        }
        
        int oldLevel = currentLevel;
        int targetLevel = currentLevel + amount;
        int maxLevel = upgradeData.GetMaxLevel();
        
        // 최대 레벨 체크
        if (currentLevel >= maxLevel)
        {
            Debug.Log($"[FlashlightUpgradeManager] 이미 최대 레벨입니다. (Level {maxLevel})");
            OnMaxLevelReached?.Invoke(maxLevel);
            return false;
        }
        
        // 레벨 제한
        targetLevel = Mathf.Min(targetLevel, maxLevel);
        
        // 임시 업그레이드 처리
        if (upgradeType == UpgradeType.Temporary)
        {
            float upgradeDuration = duration > 0 ? duration : temporaryUpgradeDuration;
            StartTemporaryUpgrade(targetLevel, upgradeDuration);
            return true;
        }
        
        // 일반 레벨업
        currentLevel = targetLevel;
        currentUpgradeType = upgradeType;
        
        ApplyLevel(currentLevel, true); // UI 표시 포함
        
        // 정적 변수에 저장
        SaveToStaticData();
        
        Debug.Log($"[FlashlightUpgradeManager] 레벨업: {oldLevel} → {currentLevel} (타입: {upgradeType})");
        
        OnLevelUp?.Invoke(oldLevel, currentLevel);
        OnLevelChanged?.Invoke(currentLevel);
        
        if (currentLevel >= maxLevel)
        {
            OnMaxLevelReached?.Invoke(maxLevel);
        }
        
        return true;
    }
    
    /// <summary>
    /// 특정 레벨로 직접 설정
    /// </summary>
    public bool SetLevel(int level, UpgradeType upgradeType = UpgradeType.ScenePersistent)
    {
        if (level < 1 || level > upgradeData.GetMaxLevel())
        {
            Debug.LogWarning($"[FlashlightUpgradeManager] 유효하지 않은 레벨: {level}");
            return false;
        }
        
        int oldLevel = currentLevel;
        currentLevel = level;
        currentUpgradeType = upgradeType;
        
        ApplyLevel(currentLevel, level > oldLevel); // 레벨이 올라갔을 때만 UI 표시
        
        // 정적 변수에 저장
        SaveToStaticData();
        
        Debug.Log($"[FlashlightUpgradeManager] 레벨 설정: {oldLevel} → {currentLevel}");
        OnLevelChanged?.Invoke(currentLevel);
        
        return true;
    }
    
    /// <summary>
    /// 레벨 1로 초기화
    /// </summary>
    public void ResetToLevel1()
    {
        Debug.Log("[FlashlightUpgradeManager] 레벨 1로 초기화");
        
        // 임시 업그레이드 취소
        if (isTemporaryUpgradeActive && temporaryUpgradeCoroutine != null)
        {
            StopCoroutine(temporaryUpgradeCoroutine);
            isTemporaryUpgradeActive = false;
        }
        
        currentLevel = 1;
        ApplyLevel(currentLevel, false); // UI 표시 없이
        
        // 정적 변수에 저장
        SaveToStaticData();
        
        OnLevelChanged?.Invoke(currentLevel);
    }
    
    /// <summary>
    /// 특정 레벨의 설정을 손전등에 적용
    /// </summary>
    private void ApplyLevel(int level, bool showUI = false)
    {
        FlashlightLevel levelData = upgradeData.GetLevel(level);
        
        if (levelData == null)
        {
            Debug.LogError($"[FlashlightUpgradeManager] 레벨 {level} 데이터를 찾을 수 없습니다!");
            return;
        }
        
        // UI 표시 (레벨업 시)
        if (showUI && uiManager != null)
        {
            // UIManager를 통해 업그레이드 정보 전달
            uiManager.ShowFlashlightUpgrade(
                upgradeUIKey,
                level,
                previousAngle,
                levelData.viewAngle,
                previousRadius,
                levelData.viewRadius
            );
        }
        
        // 이전 값 저장
        previousAngle = levelData.viewAngle;
        previousRadius = levelData.viewRadius;
        
        visionCone.SetViewAngle(levelData.viewAngle);
        visionCone.SetViewRadius(levelData.viewRadius);
        
        Debug.Log($"[FlashlightUpgradeManager] 레벨 {level} 적용: 각도={levelData.viewAngle}°, 반지름={levelData.viewRadius}m");
    }
    
    /// <summary>
    /// 정적 변수에 현재 레벨 저장
    /// </summary>
    private void SaveToStaticData()
    {
        savedLevel = currentLevel;
        savedUpgradeType = currentUpgradeType;
        
        // Permanent 타입이면 PlayerPrefs에도 저장
        if (currentUpgradeType == UpgradeType.Permanent)
        {
            SaveToPlayerPrefs();
        }
        
        Debug.Log($"[FlashlightUpgradeManager] 정적 데이터 저장: 레벨 {currentLevel}, 타입 {currentUpgradeType}");
    }
    
    /// <summary>
    /// 임시 업그레이드 시작
    /// </summary>
    private void StartTemporaryUpgrade(int targetLevel, float duration)
    {
        // 기존 임시 업그레이드 취소
        if (temporaryUpgradeCoroutine != null)
        {
            StopCoroutine(temporaryUpgradeCoroutine);
        }
        
        temporaryUpgradeCoroutine = StartCoroutine(TemporaryUpgradeCoroutine(targetLevel, duration));
    }
    
    /// <summary>
    /// 임시 업그레이드 코루틴
    /// </summary>
    private IEnumerator TemporaryUpgradeCoroutine(int targetLevel, float duration)
    {
        int originalLevel = currentLevel;
        FlashlightLevel originalLevelData = upgradeData.GetLevel(originalLevel);
        isTemporaryUpgradeActive = true;
        
        Debug.Log($"[FlashlightUpgradeManager] 임시 업그레이드 시작: {originalLevel} → {targetLevel} ({duration}초)");
        
        // 레벨업 (정적 변수에는 저장하지 않음)
        currentLevel = targetLevel;
        ApplyLevel(currentLevel, true); // UI 표시 포함
        OnLevelChanged?.Invoke(currentLevel);
        
        // 대기
        yield return new WaitForSeconds(duration);
        
        // 복귀
        currentLevel = originalLevel;
        
        // 이전 값을 원래 레벨의 값으로 복원
        if (originalLevelData != null)
        {
            previousAngle = originalLevelData.viewAngle;
            previousRadius = originalLevelData.viewRadius;
        }
        
        ApplyLevel(currentLevel, false); // UI 표시 없이 복귀
        OnLevelChanged?.Invoke(currentLevel);
        
        isTemporaryUpgradeActive = false;
        
        Debug.Log($"[FlashlightUpgradeManager] 임시 업그레이드 종료: {targetLevel} → {originalLevel}");
    }
    
    /// <summary>
    /// PlayerPrefs에서 레벨 로드
    /// </summary>
    private void LoadFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey("FlashlightLevel"))
        {
            savedLevel = PlayerPrefs.GetInt("FlashlightLevel", 1);
            
            // UpgradeType도 저장되어 있다면 로드
            if (PlayerPrefs.HasKey("FlashlightUpgradeType"))
            {
                int typeInt = PlayerPrefs.GetInt("FlashlightUpgradeType", 1);
                savedUpgradeType = (UpgradeType)typeInt;
            }
            
            Debug.Log($"[FlashlightUpgradeManager] PlayerPrefs에서 로드: {savedLevel} (타입: {savedUpgradeType})");
        }
    }
    
    /// <summary>
    /// PlayerPrefs에 레벨 저장
    /// </summary>
    private void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt("FlashlightLevel", currentLevel);
        PlayerPrefs.SetInt("FlashlightUpgradeType", (int)currentUpgradeType);
        PlayerPrefs.Save();
        
        Debug.Log($"[FlashlightUpgradeManager] PlayerPrefs에 저장: {currentLevel}");
    }
    
    /// <summary>
    /// 영구 레벨 초기화
    /// </summary>
    public void ResetPermanentLevel()
    {
        PlayerPrefs.DeleteKey("FlashlightLevel");
        PlayerPrefs.DeleteKey("FlashlightUpgradeType");
        PlayerPrefs.Save();
        
        savedLevel = 1;
        savedUpgradeType = UpgradeType.ScenePersistent;
        currentLevel = 1;
        
        ApplyLevel(currentLevel, false);
        Debug.Log("[FlashlightUpgradeManager] 영구 레벨 초기화 완료");
    }
    
    /// <summary>
    /// 정적 데이터 강제 리셋 (디버그용)
    /// </summary>
    public static void ResetStaticData()
    {
        savedLevel = 1;
        savedUpgradeType = UpgradeType.ScenePersistent;
        isStaticDataInitialized = false;
        
        Debug.Log("[FlashlightUpgradeManager] 정적 데이터 리셋");
    }
    
    // ========== Public API ==========
    
    public int GetCurrentLevel() => currentLevel;
    public int GetMaxLevel() => upgradeData.GetMaxLevel();
    public UpgradeType GetCurrentUpgradeType() => currentUpgradeType;
    public bool IsTemporaryUpgradeActive() => isTemporaryUpgradeActive;
    public FlashlightLevel GetCurrentLevelData() => upgradeData.GetLevel(currentLevel);

    /// <summary>
    /// 레벨업 가능 여부
    /// </summary>
    public bool CanLevelUp()
    {
        return currentLevel < upgradeData.GetMaxLevel();
    }
}