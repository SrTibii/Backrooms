#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using static space.chikalin.textdecal.TextDecal.TextDecalSettings;

namespace space.chikalin.textdecal.Editor
{
[CustomEditor(typeof(TextDecal))]
[CanEditMultipleObjects]
class TextDecalEditor : UnityEditor.Editor
{
    private static string[] s_PanelStateLabel = { "\t- <i>Click to collapse</i> -", "\t- <i>Click to expand</i>  -" };

    public static readonly GUIContent AngleFadeContent = EditorGUIUtility.TrTextContent("Angle Fade",
        "Controls the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface. Requires 'Decal Layers' to be enabled in the URP Asset and Frame Settings.");

    private SerializedProperty _settings;
    private SerializedProperty _projectionDepth;
    private SerializedProperty _useDefaultUV;
    private SerializedProperty _vertexData;
    private SerializedProperty _UVData;
    private SerializedProperty _extraData;
    private SerializedProperty _startAngleFade;
    private SerializedProperty _endAngleFade;

    private static bool s_Advanced = false;

    private void OnEnable()
    {
        _settings = serializedObject.FindProperty("settings");
        _projectionDepth = _settings.FindPropertyRelative("projectionDepth");
        _useDefaultUV = _settings.FindPropertyRelative("useDefaultUV");
        _vertexData = _settings.FindPropertyRelative("vertexData");
        _UVData = _settings.FindPropertyRelative("UVData");
        _extraData = _settings.FindPropertyRelative("extraData");
        _startAngleFade = _settings.FindPropertyRelative("startAngleFade");
        _endAngleFade = _settings.FindPropertyRelative("endAngleFade");
    }

    public override void OnInspectorGUI()
    {
        // base.OnInspectorGUI();
        serializedObject.Update();

        TextDecalEditorUtils.WarningMessagePanel();

        _projectionDepth.floatValue = EditorGUILayout.FloatField("Projection Depth", _projectionDepth.floatValue);
        EditorGUILayout.Space();

        var angleFadeSupport = false;
        foreach (var t in targets)
        {
            var mat = (t as TextDecal)?.GetComponent<TMP_Text>()?.fontSharedMaterial;
            if (mat == null)
                continue;
            angleFadeSupport = mat.IsKeywordEnabled(TextDecalShaderEditor.ID_ANGLE_FADE);
            if (!angleFadeSupport) break;
        }

        using (new EditorGUI.DisabledScope(!angleFadeSupport))
        {
            var angleFadeMinValue = _startAngleFade.floatValue;
            var angleFadeMaxValue = _endAngleFade.floatValue;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(AngleFadeContent, ref angleFadeMinValue, ref angleFadeMaxValue, 0.0f, 180.0f);
            if (EditorGUI.EndChangeCheck())
            {
                _startAngleFade.floatValue = angleFadeMinValue;
                _endAngleFade.floatValue = angleFadeMaxValue;
            }
        }

        if (!angleFadeSupport)
        {
            EditorGUILayout.HelpBox(
                $"Decal Angle Fade is not enabled in Shader. In ShaderGraph enable Angle Fade option.",
                MessageType.Info);
        }


        if (GUILayout.Button("Force Update"))
        {
            ((TextDecal)target).ForceDecalUpdate();
        }

        EditorGUILayout.Space();

        s_Advanced = BeginPanel("Advanced Settings", s_Advanced);
        if (s_Advanced)
        {
            DoAdvancedPanel();
        }

        EndPanel();

        serializedObject.ApplyModifiedProperties();
    }


    private void DoAdvancedPanel()
    {
        _useDefaultUV.boolValue = EditorGUILayout.Toggle("Use Default UV", _useDefaultUV.boolValue);

        if (_useDefaultUV.boolValue)
        {
            EditorGUILayout.LabelField("Vertex Data",
                Enum.GetName(typeof(UVChannel), vertexDataDefault));
            EditorGUILayout.LabelField("UV Data", Enum.GetName(typeof(UVChannel), UVDataDefault));
            EditorGUILayout.LabelField("Extra Data", Enum.GetName(typeof(UVChannel), extraDataDefault));
        }
        else
        {
            _vertexData.intValue =
                (int)(UVChannel)EditorGUILayout.EnumPopup("Vertex Data", (UVChannel)_vertexData.intValue);
            _UVData.intValue = (int)(UVChannel)EditorGUILayout.EnumPopup("UV Data", (UVChannel)_UVData.intValue);
            _extraData.intValue =
                (int)(UVChannel)EditorGUILayout.EnumPopup("Extra Data", (UVChannel)_extraData.intValue);
        }
    }

    private bool BeginPanel(string panel, bool expanded)
    {
        EditorGUI.indentLevel = 0;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Rect r = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(20, 18));
        r.x += 20;
        r.width += 6;

        bool enabled = GUI.enabled;
        GUI.enabled = true;
        expanded = TextDecalEditorUtils.EditorToggle(r, expanded, new GUIContent(panel), UIStyles.panelTitle);
        r.width -= 30;
        EditorGUI.LabelField(r, new GUIContent(expanded ? s_PanelStateLabel[0] : s_PanelStateLabel[1]),
            UIStyles.rightLabel);
        GUI.enabled = enabled;

        EditorGUI.indentLevel += 1;
        EditorGUI.BeginDisabledGroup(false);

        return expanded;
    }

    private void EndPanel()
    {
        EditorGUI.EndDisabledGroup();
        EditorGUI.indentLevel -= 1;
        EditorGUILayout.EndVertical();
    }
}
}

#endif