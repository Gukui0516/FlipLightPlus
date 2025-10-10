using UnityEngine;
using UnityEditor;

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
        
        // 기본 문 설정
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_doorRotatePoint"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_openAngle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_openTime"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_originRotateY"));
        
        EditorGUILayout.Space(10);
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
        
        // 탈출 트리거 설정
        EditorGUILayout.LabelField("탈출 트리거 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("exitTriggerCollider"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playerTag"));
        
        EditorGUILayout.Space(10);
        
        // 등록 완료 대기 시간
        EditorGUILayout.LabelField("등록 완료 대기 시간", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoFinalizeDelay"));
        
        // 런타임 정보 표시
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("=== 런타임 정보 ===", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("등록된 게이지 수", controller.RegisteredGaugeCount);
            EditorGUILayout.IntField("필요 게이지 수", controller.RequiredGaugeCount);
            EditorGUILayout.IntField("만족한 게이지 수", controller.SatisfiedGaugeCount);
            EditorGUILayout.Toggle("등록 완료", controller.IsRegistrationFinalized);
            EditorGUI.EndDisabledGroup();
            
            // 상태 출력 버튼
            if (GUILayout.Button("상태 출력 (Console)"))
            {
                controller.PrintStatus();
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private string GetAutoModeDescription(int enumIndex)
    {
        switch (enumIndex)
        {
            case 0: // MatchRegisteredCount
                return "등록 개수 모드: 씬에 등록된 모든 게이지를 만족해야 문이 열립니다.";
            case 1: // MatchStageNumber
                return "스테이지 숫자 모드: GameManager의 CurrentStage 값만큼 게이지를 만족해야 문이 열립니다.\n(예: 스테이지 3이면 3개만 만족하면 됨)";
            default:
                return "";
        }
    }
}