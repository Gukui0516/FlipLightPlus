using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
/// <summary>
/// GeneratorIndicator의 커스텀 인스펙터
/// </summary>
[CustomEditor(typeof(GeneratorIndicator))]
public class GeneratorIndicatorEditor : Editor
{
    private SerializedProperty lightGaugeSystem;
    private SerializedProperty pingText;

    private SerializedProperty dotDuration;
    private SerializedProperty dashDuration;
    private SerializedProperty symbolGap;
    private SerializedProperty letterGap;
    private SerializedProperty wordGap;

    private SerializedProperty signalColor;
    private SerializedProperty signalThickness;
    private SerializedProperty signalSpeed;
    private SerializedProperty sortingLayerName;
    private SerializedProperty orderInLayer;

    private SerializedProperty typingSpeed;
    private SerializedProperty textDisplayDuration;

    private SerializedProperty repeatCount;
    private SerializedProperty repeatInterval;
    private SerializedProperty keepTextDuringRepeat;

    private SerializedProperty startWhenCompleted;
    private SerializedProperty requirePlayerNearby;
    private SerializedProperty detectionRadius;
    private SerializedProperty playerLayer;

    private bool showMorseSettings = false;
    private bool showAdvancedSettings = false;

    private void OnEnable()
    {
        lightGaugeSystem = serializedObject.FindProperty("lightGaugeSystem");
        pingText = serializedObject.FindProperty("pingText");

        dotDuration = serializedObject.FindProperty("dotDuration");
        dashDuration = serializedObject.FindProperty("dashDuration");
        symbolGap = serializedObject.FindProperty("symbolGap");
        letterGap = serializedObject.FindProperty("letterGap");
        wordGap = serializedObject.FindProperty("wordGap");

        signalColor = serializedObject.FindProperty("signalColor");
        signalThickness = serializedObject.FindProperty("signalThickness");
        signalSpeed = serializedObject.FindProperty("signalSpeed");
        sortingLayerName = serializedObject.FindProperty("sortingLayerName");
        orderInLayer = serializedObject.FindProperty("orderInLayer");

        typingSpeed = serializedObject.FindProperty("typingSpeed");
        textDisplayDuration = serializedObject.FindProperty("textDisplayDuration");

        repeatCount = serializedObject.FindProperty("repeatCount");
        repeatInterval = serializedObject.FindProperty("repeatInterval");
        keepTextDuringRepeat = serializedObject.FindProperty("keepTextDuringRepeat");

        startWhenCompleted = serializedObject.FindProperty("startWhenCompleted");
        requirePlayerNearby = serializedObject.FindProperty("requirePlayerNearby");
        detectionRadius = serializedObject.FindProperty("detectionRadius");
        playerLayer = serializedObject.FindProperty("playerLayer");
    }

    public override void OnInspectorGUI()
    {
        GeneratorIndicator indicator = (GeneratorIndicator)target;
        serializedObject.Update();

        // === 헤더 ===
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Generator Morse Indicator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "발전기가 완료되면 다음 발전기 위치를 모스 부호(GENERATOR)로 표시합니다.\n" +
            "• 터미널 스타일 텍스트 표시 (미리 배치된 TMP 사용)\n" +
            "• 모스 부호로 신호 전송 (약 3초)\n" +
            "• 신호가 목표 지점에 도달하면 사라짐\n" +
            "• 반복 횟수 설정 가능",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // === 참조 설정 ===
        EditorGUILayout.LabelField("참조", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(lightGaugeSystem, new GUIContent("Light Gauge System", "비워두면 자동으로 같은 오브젝트에서 찾음"));
        EditorGUILayout.PropertyField(pingText, new GUIContent("Ping Text (TMP)", "미리 배치된 TextMeshProUGUI를 수동으로 할당"));

        if (pingText.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("⚠️ Ping Text가 할당되지 않았습니다. 미리 배치된 TMP를 인스펙터에서 드래그하여 할당하세요.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("✅ Ping Text가 할당되었습니다.", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        // === 활성화 설정 ===
        EditorGUILayout.LabelField("활성화 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startWhenCompleted, new GUIContent("Start When Completed", "이 발전기가 완료되면 자동 시작"));

        EditorGUILayout.PropertyField(requirePlayerNearby, new GUIContent("Require Player Nearby", "플레이어가 근처에 있을 때만 활성화"));

        if (requirePlayerNearby.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(detectionRadius, new GUIContent("Detection Radius", "플레이어 감지 반경"));
            EditorGUILayout.PropertyField(playerLayer, new GUIContent("Player Layer", "플레이어 레이어"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // === 신호 외형 ===
        EditorGUILayout.LabelField("신호 외형", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(signalColor, new GUIContent("Signal Color", "신호 색상"));
        EditorGUILayout.PropertyField(signalThickness, new GUIContent("Signal Thickness", "신호 두께"));
        EditorGUILayout.PropertyField(signalSpeed, new GUIContent("Signal Speed", "신호 이동 속도 (빠른 전송: 15)"));
        EditorGUILayout.PropertyField(sortingLayerName, new GUIContent("Sorting Layer"));
        EditorGUILayout.PropertyField(orderInLayer, new GUIContent("Order in Layer"));

        EditorGUILayout.HelpBox(
            "💨 빠른 전송 모드 권장 설정:\n" +
            "• Signal Speed: 15 (기본: 8)\n" +
            "• Thickness: 0.2\n" +
            "이 설정으로 약 3초만에 전송 완료",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // === 텍스트 설정 ===
        EditorGUILayout.LabelField("텍스트 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(typingSpeed, new GUIContent("Typing Speed", "글자 타이핑 속도 (초/글자)"));
        EditorGUILayout.PropertyField(textDisplayDuration, new GUIContent("Display Duration", "텍스트 표시 시간"));

        EditorGUILayout.Space(10);

        // === 반복 설정 ===
        EditorGUILayout.LabelField("반복 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(repeatCount, new GUIContent("Repeat Count", "모스 부호 반복 횟수 (0 = 무한)"));

        if (repeatCount.intValue == 0)
        {
            EditorGUILayout.HelpBox("⚠️ 무한 반복 모드: 조건이 만족되거나 타겟이 없을 때까지 계속 전송됩니다.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"✅ {repeatCount.intValue}회 반복 후 자동 종료됩니다.", MessageType.Info);
        }

        EditorGUILayout.PropertyField(repeatInterval, new GUIContent("Repeat Interval", "각 반복 사이의 대기 시간 (초)"));
        EditorGUILayout.PropertyField(keepTextDuringRepeat, new GUIContent("Keep Text During Repeat", "반복 중에도 텍스트 유지"));

        // 총 소요 시간 계산
        float morseTime = CalculateTotalMorseTime(
            dotDuration.floatValue,
            dashDuration.floatValue,
            symbolGap.floatValue,
            letterGap.floatValue
        );

        float totalCycleTime = morseTime + repeatInterval.floatValue;

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("시간 계산", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  • 1회 모스 전송: {morseTime:F2}초", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  • 대기 시간: {repeatInterval.floatValue:F2}초", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  • 1회 사이클: {totalCycleTime:F2}초", EditorStyles.miniLabel);

        if (repeatCount.intValue > 0)
        {
            float totalTime = (totalCycleTime * repeatCount.intValue) - repeatInterval.floatValue;
            EditorGUILayout.LabelField($"  • 총 소요 시간: {totalTime:F2}초", EditorStyles.boldLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"  • 총 소요 시간: 무한 ∞", EditorStyles.boldLabel);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // === 모스 부호 타이밍 (접을 수 있음) ===
        showMorseSettings = EditorGUILayout.Foldout(showMorseSettings, "모스 부호 타이밍 설정", true);
        if (showMorseSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "모스 부호 표준 타이밍 (빠른 전송 모드):\n" +
                "• 점(Dot): 0.05초\n" +
                "• 선(Dash): 0.15초 (점의 3배)\n" +
                "• 기호 간 간격: 0.05초\n" +
                "• 글자 간 간격: 0.15초\n" +
                "• GENERATOR 전송 시간: 약 3초",
                MessageType.None
            );

            EditorGUILayout.PropertyField(dotDuration, new GUIContent("Dot Duration", "점(·) 지속 시간"));
            EditorGUILayout.PropertyField(dashDuration, new GUIContent("Dash Duration", "선(—) 지속 시간"));
            EditorGUILayout.PropertyField(symbolGap, new GUIContent("Symbol Gap", "점/선 사이 간격"));
            EditorGUILayout.PropertyField(letterGap, new GUIContent("Letter Gap", "글자 사이 간격"));
            EditorGUILayout.PropertyField(wordGap, new GUIContent("Word Gap", "단어 사이 간격 (사용 안 함)"));

            // 전체 시간 계산
            EditorGUILayout.Space(5);
            float totalTime = CalculateTotalMorseTime(
                dotDuration.floatValue,
                dashDuration.floatValue,
                symbolGap.floatValue,
                letterGap.floatValue
            );
            EditorGUILayout.LabelField($"💡 GENERATOR 전송 시간: 약 {totalTime:F2}초", EditorStyles.boldLabel);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // === 런타임 정보 ===
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("=== 런타임 정보 ===", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);

            // 반사를 통해 private 필드 접근
            var isIndicatingField = typeof(GeneratorIndicator).GetField("isIndicating",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hasCompletedOnceField = typeof(GeneratorIndicator).GetField("hasCompletedOnce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var currentRepeatCountField = typeof(GeneratorIndicator).GetField("currentRepeatCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetGeneratorField = typeof(GeneratorIndicator).GetField("targetGenerator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool isIndicating = isIndicatingField != null && (bool)isIndicatingField.GetValue(indicator);
            bool hasCompletedOnce = hasCompletedOnceField != null && (bool)hasCompletedOnceField.GetValue(indicator);
            int currentRepeat = currentRepeatCountField != null ? (int)currentRepeatCountField.GetValue(indicator) : 0;
            var targetGen = targetGeneratorField != null ? targetGeneratorField.GetValue(indicator) : null;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("상태", EditorStyles.miniLabel);
            EditorGUILayout.Toggle("Is Indicating", isIndicating);
            EditorGUILayout.Toggle("Has Completed Once", hasCompletedOnce);

            if (hasCompletedOnce)
            {
                EditorGUILayout.HelpBox("✅ 인디케이터가 완료되었습니다. 자동으로 다시 시작하지 않습니다.", MessageType.Info);
            }

            if (isIndicating)
            {
                string repeatText = repeatCount.intValue == 0
                    ? $"{currentRepeat + 1} / ∞"
                    : $"{currentRepeat + 1} / {repeatCount.intValue}";
                EditorGUILayout.LabelField("Current Repeat", repeatText);

                if (targetGen != null)
                {
                    var targetGauge = targetGen as LightGaugeSystem;
                    if (targetGauge != null)
                    {
                        EditorGUILayout.LabelField("Target", targetGauge.gameObject.name);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !isIndicating;
            if (GUILayout.Button("🎯 수동 인디케이터 시작", GUILayout.Height(30)))
            {
                indicator.StartIndicating();
            }
            GUI.enabled = true;

            if (isIndicating && GUILayout.Button("⏹️ 중단", GUILayout.Height(30)))
            {
                indicator.StopIndicating();
            }

            EditorGUILayout.EndHorizontal();

            // 완료 상태일 때 리셋 버튼 표시
            if (hasCompletedOnce && !isIndicating)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("🔄 완료 상태 리셋", GUILayout.Height(25)))
                {
                    indicator.ResetCompletion();
                }
            }

            // ExitDoor 조건 체크
            var generatorManagerField = typeof(GeneratorIndicator).GetField("generatorManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genManager = generatorManagerField?.GetValue(indicator) as GeneratorManager;

            if (genManager != null && genManager.AreAllConditionsMet())
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("🚪 모든 ExitDoor 조건이 만족되어 인디케이터가 비활성화됩니다.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("▶️ Play 모드에서 테스트 버튼이 표시됩니다.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// GENERATOR 모스 부호의 총 전송 시간 계산
    /// </summary>
    private float CalculateTotalMorseTime(float dot, float dash, float symbolGap, float letterGap)
    {
        // GENERATOR: --. . -. . .-. .- - --- .-.
        // G(--.) = 2dash + 1dot + 2gap
        // E(.) = 1dot
        // N(-.) = 1dash + 1dot + 1gap
        // E(.) = 1dot
        // R(.-.) = 1dot + 1dash + 1dot + 2gap
        // A(.-) = 1dot + 1dash + 1gap
        // T(-) = 1dash
        // O(---) = 3dash + 2gap
        // R(.-.) = 1dot + 1dash + 1dot + 2gap

        int totalDots = 9;      // G(1) + E(1) + N(1) + E(1) + R(2) + A(1) + O(0) + R(2)
        int totalDashes = 14;   // G(2) + N(1) + R(1) + A(1) + T(1) + O(3) + R(1)
        int totalSymbolGaps = 15; // 각 글자 내부 간격
        int totalLetterGaps = 8;  // 9개 글자 사이의 8개 간격

        float total = (totalDots * dot) +
                      (totalDashes * dash) +
                      (totalSymbolGaps * symbolGap) +
                      (totalLetterGaps * letterGap);

        return total;
    }
}
#endif