using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
/// <summary>
/// ExitDoorController의 커스텀 인스펙터
/// autoSetRequiredCount에 따라 필드를 동적으로 표시합니다
/// </summary>
[CustomEditor(typeof(ExitDoorController))]
public class ExitDoorControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ExitDoorController controller = (ExitDoorController)target;

        serializedObject.Update();

        // === 문 회전 설정 ===
        EditorGUILayout.LabelField("문 회전 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_leftDoorRotatePoint"), new GUIContent("Left Door Rotate Point", "왼쪽 문의 회전 중심점"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_rightDoorRotatePoint"), new GUIContent("Right Door Rotate Point", "오른쪽 문의 회전 중심점"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_openAngle"), new GUIContent("Open Angle", "문이 열릴 때의 각도 (양쪽 각각)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_openTime"), new GUIContent("Open Time", "문이 열리는 데 걸리는 시간"));

        EditorGUILayout.Space(10);

        // === 카메라 뷰포트 체크 ===
        EditorGUILayout.LabelField("카메라 뷰포트 체크", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mainCamera"), new GUIContent("Main Camera", "비워두면 Camera.main 자동 탐색"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("doorCheckPoint"), new GUIContent("Door Check Point", "문의 중심점 (비워두면 자신의 Transform 사용)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("viewportCheckInterval"), new GUIContent("Viewport Check Interval", "카메라 뷰포트 체크 간격 (초)"));

        EditorGUILayout.HelpBox(
            "조건 만족 후, 문이 카메라 화면에 보일 때까지 대기했다가 열립니다.\n" +
            "이를 통해 플레이어가 문이 열리는 순간을 놓치지 않게 합니다.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // === 게이지 조건 설정 ===
        EditorGUILayout.LabelField("게이지 조건 설정", EditorStyles.boldLabel);

        // Auto Set Required Count 체크박스
        SerializedProperty autoSetProp = serializedObject.FindProperty("autoSetRequiredCount");
        EditorGUILayout.PropertyField(autoSetProp, new GUIContent("Auto Set Required Count", "자동으로 필요 게이지 개수를 설정"));

        // 체크박스 상태에 따라 다른 필드 표시
        if (autoSetProp.boolValue)
        {
            // 자동 모드 - 드롭다운 표시
            EditorGUI.indentLevel++;
            SerializedProperty autoModeProp = serializedObject.FindProperty("autoMode");
            EditorGUILayout.PropertyField(autoModeProp, new GUIContent("Auto Mode", "자동 설정 방식 선택"));
            EditorGUI.indentLevel--;

            // 현재 선택된 모드 설명
            EditorGUILayout.HelpBox(GetAutoModeDescription(autoModeProp.enumValueIndex), MessageType.Info);
        }
        else
        {
            // 수동 모드 - 숫자 입력 필드 표시
            EditorGUI.indentLevel++;
            SerializedProperty manualCountProp = serializedObject.FindProperty("manualRequiredGaugeCount");
            EditorGUILayout.PropertyField(manualCountProp, new GUIContent("Manual Required Count", "수동으로 설정할 필요 게이지 개수"));
            EditorGUI.indentLevel--;

            EditorGUILayout.HelpBox("수동 모드: 직접 입력한 개수만큼 게이지를 만족해야 문이 열립니다.", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        // === 이펙트 설정 ===
        EditorGUILayout.LabelField("이펙트 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("openEffectGameObject"), new GUIContent("Open Effect GameObject", "조건 만족 시 활성화할 이펙트"));

        EditorGUILayout.HelpBox(
            "모든 게이지 조건이 만족되면 이 이펙트가 활성화됩니다.\n" +
            "플레이어가 다가와서 카메라에 문이 보이면 문이 열립니다.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // === 탈출 트리거 설정 ===
        EditorGUILayout.LabelField("탈출 트리거 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("exitTriggerCollider"), new GUIContent("Exit Trigger Collider", "문이 열렸을 때 활성화할 BoxCollider2D"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playerTag"), new GUIContent("Player Tag", "플레이어를 식별할 태그"));

        EditorGUILayout.Space(10);

        // === 등록 완료 대기 시간 ===
        EditorGUILayout.LabelField("등록 완료 대기 시간", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoFinalizeDelay"), new GUIContent("Auto Finalize Delay", "씬 시작 후 이 시간이 지나면 자동으로 등록 완료"));

        // === 런타임 정보 표시 ===
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("=== 런타임 정보 ===", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);

            // 게이지 정보
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("게이지 상태", EditorStyles.miniLabel);
            EditorGUILayout.IntField("등록된 게이지 수", controller.RegisteredGaugeCount);
            EditorGUILayout.IntField("필요 게이지 수", controller.RequiredGaugeCount);
            EditorGUILayout.IntField("만족한 게이지 수", controller.SatisfiedGaugeCount);

            // 진행률 표시
            if (controller.RequiredGaugeCount > 0)
            {
                float progress = (float)controller.SatisfiedGaugeCount / controller.RequiredGaugeCount;
                EditorGUILayout.LabelField("진행률", $"{progress * 100:F1}% ({controller.SatisfiedGaugeCount}/{controller.RequiredGaugeCount})");
                Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(progressRect, progress, $"{controller.SatisfiedGaugeCount}/{controller.RequiredGaugeCount}");
            }
            EditorGUILayout.EndVertical();

            // 상태 정보
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("문 상태", EditorStyles.miniLabel);
            EditorGUILayout.Toggle("등록 완료", controller.IsRegistrationFinalized);
            EditorGUILayout.EndVertical();

            EditorGUI.EndDisabledGroup();

            // 상태 출력 버튼
            EditorGUILayout.Space(5);
            if (GUILayout.Button("📋 상태 출력 (Console)", GUILayout.Height(30)))
            {
                controller.PrintStatus();
            }

            // 테스트 버튼
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🚪 문 즉시 열기 (테스트)", GUILayout.Height(25)))
            {
                controller.OpenDoorImmediately();
            }
            if (GUILayout.Button("🚪 문 닫기", GUILayout.Height(25)))
            {
                controller.CloseDoor();
            }
            EditorGUILayout.EndHorizontal();

            // 경고 메시지
            if (controller.SatisfiedGaugeCount >= controller.RequiredGaugeCount && controller.IsRegistrationFinalized)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("✅ 모든 조건이 만족되었습니다! 카메라 뷰포트에 문이 보이면 자동으로 열립니다.", MessageType.Warning);
            }
        }
        else
        {
            // Play 모드가 아닐 때 안내
            EditorGUILayout.Space(15);
            EditorGUILayout.HelpBox("▶️ Play 모드에서 런타임 정보와 테스트 버튼이 표시됩니다.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private string GetAutoModeDescription(int enumIndex)
    {
        switch (enumIndex)
        {
            case 0: // MatchRegisteredCount
                return "📊 등록 개수 모드\n씬에 등록된 모든 게이지를 만족해야 문이 열립니다.";
            case 1: // MatchStageNumber
                return "🎮 스테이지 숫자 모드\nGameManager의 CurrentStage 값만큼 게이지를 만족해야 문이 열립니다.\n(예: 스테이지 3이면 3개만 만족하면 됨)";
            default:
                return "";
        }
    }
}
#endif