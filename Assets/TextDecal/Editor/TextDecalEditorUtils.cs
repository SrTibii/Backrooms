#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace space.chikalin.textdecal.Editor
{
public static class TextDecalEditorUtils
{
    public static void WarningMessagePanel()
    {
        // Make sure TMP Essential Resources have been imported in the user project.
        try
        {
            if (TMPro.TMP_Settings.instance == null)
            {
                EditorGUILayout.HelpBox(
                    "Please import the TMP Essential Resources. To import please use the \"Window → TextMeshPro → Import TMP Essential Resources\" menu option.",
                    MessageType.Warning, true);
                if (GUILayout.Button("Import TMP Essential Resources"))
                {
                    TMPro.TMP_PackageUtilities.ImportProjectResourcesMenu();
                }

                EditorGUILayout.Space();
            }
        }
        catch (Exception)
        {
            // ignored
        }

        try
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipelineAsset == null)
            {
                EditorGUILayout.HelpBox("TextDecal was tested on URP only", MessageType.Info, true);
                return;
            }

            // Получаем ScriptableRendererData из Pipeline Asset
            var serializedObject = new SerializedObject(pipelineAsset);
            var rendererDataListProp = serializedObject.FindProperty("m_RendererDataList");

            var isDecalRendererFeatureAdded = false;
            var isTextDecalRendererFeatureAdded = false;

            if (rendererDataListProp is { arraySize: > 0 })
            {
                for (var i = 0; i < rendererDataListProp.arraySize; i++)
                {
                    var rendererData =
                        rendererDataListProp.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                    if (rendererData == null) continue;
                    
                    var features = rendererData.rendererFeatures;
                    foreach (var feature in features)
                    {
                        switch (feature.GetType().Name)
                        {
                            case "DecalRendererFeature" when feature.isActive:
                                isDecalRendererFeatureAdded = true;
                                break;
                            case "TextDecalRendererFeature" when feature.isActive:
                                isTextDecalRendererFeatureAdded = true;
                                break;
                        }

                        if (isDecalRendererFeatureAdded && isTextDecalRendererFeatureAdded) break;
                    }

                    if (isDecalRendererFeatureAdded && isTextDecalRendererFeatureAdded) break;
                }
            }

            if (!isDecalRendererFeatureAdded)
            {
                EditorGUILayout.HelpBox("The current renderer has no Decal Renderer Feature added.",
                    MessageType.Error, true);
                EditorGUILayout.Space();
            }

            if (!isTextDecalRendererFeatureAdded)
            {
                EditorGUILayout.HelpBox("The current renderer has no Text Decal Renderer Feature added.",
                    MessageType.Error, true);
                EditorGUILayout.Space();
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static bool EditorToggle(Rect position, bool value, GUIContent content, GUIStyle style)
    {
        var id = GUIUtility.GetControlID(content, FocusType.Keyboard, position);
        var evt = Event.current;

        // Toggle selected toggle on space or return key
        if (GUIUtility.keyboardControl == id && evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Space ||
                evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
        {
            value = !value;
            evt.Use();
            GUI.changed = true;
        }

        if (evt.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
        {
            GUIUtility.keyboardControl = id;
            EditorGUIUtility.editingTextField = false;
            HandleUtility.Repaint();
        }

        return GUI.Toggle(position, id, value, content, style);
    }
}
}
#endif