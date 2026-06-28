#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace space.chikalin.textdecal.Editor
{
[CustomEditor(typeof(TextDecalRendererFeature))]
public class TextDecalRendererFeatureEditor : UnityEditor.Editor
{
        private struct Styles
        {
            public static GUIContent Technique = EditorGUIUtility.TrTextContent("Technique", "This option determines what method is used for rendering decals.");
            public static GUIContent MaxDrawDistance = EditorGUIUtility.TrTextContent("Max Draw Distance", "Maximum global draw distance of decals.");
            public static GUIContent SurfaceData = EditorGUIUtility.TrTextContent("Surface Data", "Allows specifying which decals surface data should be blended with surfaces.");
            public static GUIContent NormalBlend = EditorGUIUtility.TrTextContent("Normal Blend", "Controls the quality of normal reconstruction. The higher the value the more accurate normal reconstruction and the cost on performance.");
        }

        private SerializedProperty m_Technique;
        // private SerializedProperty m_MaxDrawDistance;
        private SerializedProperty m_ScreenSpaceSettings;
        private SerializedProperty m_ScreenSpaceNormalBlend;

        private bool m_IsInitialized = false;

        private void Init()
        {
            if (m_IsInitialized)
                return;
            SerializedProperty settings = serializedObject.FindProperty("settings");
            m_Technique = settings.FindPropertyRelative("technique");
            // m_MaxDrawDistance = settings.FindPropertyRelative("maxDrawDistance");
            m_ScreenSpaceSettings = settings.FindPropertyRelative("screenSpaceSettings");
            m_ScreenSpaceNormalBlend = m_ScreenSpaceSettings.FindPropertyRelative("normalBlend");
            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            Init();

            TextDecalEditorUtils.WarningMessagePanel();
            
            EditorGUILayout.PropertyField(m_Technique, Styles.Technique);

            TextDecalRendererFeature.DecalTechniqueOption technique = (TextDecalRendererFeature.DecalTechniqueOption)m_Technique.intValue;

            // if (technique == TextDecalRendererFeature.DecalTechniqueOption.DBuffer)
            // {
            //     EditorGUI.indentLevel++;
            //     EditorGUILayout.PropertyField(m_DBufferSurfaceData, Styles.SurfaceData);
            //     EditorGUI.indentLevel--;
            // }

            if (technique == TextDecalRendererFeature.DecalTechniqueOption.ScreenSpace)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ScreenSpaceNormalBlend, Styles.NormalBlend);
                EditorGUI.indentLevel--;
            }

            // EditorGUILayout.PropertyField(m_MaxDrawDistance, Styles.MaxDrawDistance);
        }
}
}
#endif