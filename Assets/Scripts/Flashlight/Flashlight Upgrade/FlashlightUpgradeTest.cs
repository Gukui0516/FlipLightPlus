using UnityEngine;

/// <summary>
/// 손전등 업그레이드 테스트 스크립트
/// 키보드 입력으로 업그레이드 테스트
/// - 1 키: 레벨 +1
/// - 2 키: 레벨 +5
/// - 3 키: 레벨 1로 초기화
/// - T 키: 임시 업그레이드 테스트 (5초)
/// - P 키: 영구 레벨 초기화
/// </summary>
[RequireComponent(typeof(FlashlightUpgradeManager))]
public class FlashlightUpgradeTest : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool enableTest = true;
    [SerializeField] private KeyCode levelUpKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode bigLevelUpKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode resetKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode temporaryUpgradeKey = KeyCode.T;
    [SerializeField] private KeyCode resetPermanentKey = KeyCode.P;
    
    [Header("테스트 옵션")]
    [Tooltip("기본 업그레이드 타입")]
    [SerializeField] private FlashlightUpgradeManager.UpgradeType defaultUpgradeType = 
        FlashlightUpgradeManager.UpgradeType.ScenePersistent;
    
    [Tooltip("임시 업그레이드 지속 시간")]
    [SerializeField] private float temporaryDuration = 5f;
    
    private FlashlightUpgradeManager upgradeManager;
    
    void Awake()
    {
        upgradeManager = GetComponent<FlashlightUpgradeManager>();
        
        // 이벤트 구독
        upgradeManager.OnLevelChanged += HandleLevelChanged;
        upgradeManager.OnLevelUp += HandleLevelUp;
        upgradeManager.OnMaxLevelReached += HandleMaxLevelReached;
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (upgradeManager != null)
        {
            upgradeManager.OnLevelChanged -= HandleLevelChanged;
            upgradeManager.OnLevelUp -= HandleLevelUp;
            upgradeManager.OnMaxLevelReached -= HandleMaxLevelReached;
        }
    }
    
    void Update()
    {
        if (!enableTest) return;
        
        // 1 키: 레벨 +1
        if (Input.GetKeyDown(levelUpKey))
        {
            Debug.Log("===== [TEST] 레벨 +1 =====");
            upgradeManager.LevelUp(1, defaultUpgradeType);
        }
        
        // 2 키: 레벨 +5
        if (Input.GetKeyDown(bigLevelUpKey))
        {
            Debug.Log("===== [TEST] 레벨 +5 =====");
            upgradeManager.LevelUp(5, defaultUpgradeType);
        }
        
        // 3 키: 레벨 1로 초기화
        if (Input.GetKeyDown(resetKey))
        {
            Debug.Log("===== [TEST] 레벨 1로 초기화 =====");
            upgradeManager.ResetToLevel1();
        }
        
        // T 키: 임시 업그레이드 (현재 레벨 + 2)
        if (Input.GetKeyDown(temporaryUpgradeKey))
        {
            Debug.Log($"===== [TEST] 임시 업그레이드 ({temporaryDuration}초) =====");
            upgradeManager.LevelUp(2, FlashlightUpgradeManager.UpgradeType.Temporary, temporaryDuration);
        }
        
        // P 키: 영구 레벨 초기화
        if (Input.GetKeyDown(resetPermanentKey))
        {
            Debug.Log("===== [TEST] 영구 레벨 초기화 =====");
            upgradeManager.ResetPermanentLevel();
        }
    }
    
    void OnGUI()
    {
        if (!enableTest) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        float width = 350f;
        float height = 200f;
        
        GUILayout.BeginArea(new Rect(10, 10, width, height), style);
        
        GUILayout.Label("<b>[ 손전등 업그레이드 테스트 ]</b>", new GUIStyle(GUI.skin.label) 
        { 
            fontSize = 16, 
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        });
        
        GUILayout.Space(5);
        
        // 현재 상태
        GUILayout.Label($"현재 레벨: <b>{upgradeManager.GetCurrentLevel()}</b> / {upgradeManager.GetMaxLevel()}");
        GUILayout.Label($"업그레이드 타입: <b>{upgradeManager.GetCurrentUpgradeType()}</b>");
        
        if (upgradeManager.IsTemporaryUpgradeActive())
        {
            GUILayout.Label("<color=cyan>⏰ 임시 업그레이드 활성화!</color>");
        }
        
        var levelData = upgradeManager.GetCurrentLevelData();
        if (levelData != null)
        {
            GUILayout.Label($"각도: <b>{levelData.viewAngle:F1}°</b> / 반지름: <b>{levelData.viewRadius:F1}m</b>");
        }
        
        GUILayout.Space(10);
        
        // 컨트롤
        GUILayout.Label("<b>[ 컨트롤 ]</b>", new GUIStyle(GUI.skin.label) 
        { 
            fontStyle = FontStyle.Bold 
        });
        GUILayout.Label($"<b>1</b> - 레벨 +1");
        GUILayout.Label($"<b>2</b> - 레벨 +5");
        GUILayout.Label($"<b>3</b> - 레벨 1로 초기화");
        GUILayout.Label($"<b>T</b> - 임시 업그레이드 ({temporaryDuration}초)");
        GUILayout.Label($"<b>P</b> - 영구 레벨 초기화");
        
        GUILayout.EndArea();
    }
    
    // ========== 이벤트 핸들러 ==========
    
    private void HandleLevelChanged(int newLevel)
    {
        Debug.Log($"<color=green>✅ [EVENT] 레벨 변경됨: {newLevel}</color>");
    }
    
    private void HandleLevelUp(int oldLevel, int newLevel)
    {
        Debug.Log($"<color=lime>⬆️ [EVENT] 레벨업: {oldLevel} → {newLevel}</color>");
    }
    
    private void HandleMaxLevelReached(int maxLevel)
    {
        Debug.Log($"<color=yellow>🎉 [EVENT] 최대 레벨 도달! (Level {maxLevel})</color>");
    }
}