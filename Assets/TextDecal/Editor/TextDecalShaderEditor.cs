#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;

namespace space.chikalin.textdecal.Editor
{
[Preserve]
public class TextDecalShaderEditor : ShaderGUI
{
    private static readonly GUIContent RenderingLayerMaskContentStyle = EditorGUIUtility.TrTextContent(
        "Rendering Layers",
        "Specify the rendering layer mask for this 'TextMeshPro - Decal'. Unity renders decals on all meshes where at least one Rendering Layer value matches.");

    static readonly GUIContent RotationContent = EditorGUIUtility.TrTextContent(
        "Calculate Rotation",
        "Applies rotation to text decals when manually changing symbol rotation in the text mesh (e.g., during animation or text on a spline)");
    
    // static readonly GUIContent GammaColorContent = EditorGUIUtility.TrTextContent(
    //     "Vertex color in Gamma Space",
    //     "");

    private static bool isInitialized;
    private static bool s_Lit = true, s_Face = true, s_Outline = true, s_Bevel = true, s_Emissive = true;
    private static ShaderFeature s_OutlineFeature;
    private static ShaderFeature s_NormalBevelFeature;
    private static ShaderFeature s_AngleFadeFeature;

    private static string ID_EmissiveShaderPass = TextDecalForwardEmissivePass.TextDecalForwardEmissive;
    private static string ID_Smoothness = "_Smoothness";
    private static string ID_Metallic = "_Metallic";
    private static string ID_DrawOrder = "_DrawOrder";
    private static string ID_Emission = "_Emission";
    private static string ID_AFFECT_NORMAL = "AFFECT_NORMAL";
    public static string ID_ANGLE_FADE = "ANGLE_FADE";
    private static string ID_TEXT_DECAL_ROTATION = "TEXT_DECAL_ROTATION";
    // private static string _UI_VERTEX_COLOR_ALWAYS_GAMMA_SPACE = "_UIVertexColorAlwaysGammaSpace";

    static string[] s_PanelStateLabel = { "\t- <i>Click to collapse</i> -", "\t- <i>Click to expand</i>  -" };
    static GUIContent s_TempLabel = new();

    private MaterialEditor m_Editor;
    private Material m_Material;
    private MaterialProperty[] m_Properties;


    static TextDecalShaderEditor()
    {
        s_OutlineFeature = new ShaderFeature
        {
            undoLabel = "Outline",
            keywords = new[] { "OUTLINE_ON" }
        };

        s_NormalBevelFeature = new ShaderFeature
        {
            undoLabel = "Normal",
            keywords = new[] { ID_AFFECT_NORMAL }
        };
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        m_Editor = materialEditor;
        m_Material = materialEditor.target as Material;
        m_Properties = properties;

        DrawRenderingLayerMask("_DecalLayerMaskFromDecal", "Rendering Layer", RenderingLayerMaskContentStyle);
        DoToggle(ID_ANGLE_FADE, TextDecalEditor.AngleFadeContent);
        DoToggle(ID_TEXT_DECAL_ROTATION, RotationContent);
        // DoBool(_UI_VERTEX_COLOR_ALWAYS_GAMMA_SPACE, GammaColorContent);

        EditorGUILayout.Space();

        s_Face = BeginPanel("Face", s_Face);
        if (s_Face)
        {
            DoFacePanel();
        }

        EndPanel();

        s_Outline = BeginPanel("Outline", s_OutlineFeature, s_Outline);
        if (s_Outline)
        {
            DoOutlinePanel();
        }

        EndPanel();

        if (m_Material.HasProperty(ID_Smoothness))
        {
            s_Lit = BeginPanel("Lit", s_Lit);
            if (s_Lit)
            {
                DoLitPanel();
            }

            EndPanel();

            s_Emissive = BeginPanel("Emissive", ID_EmissiveShaderPass, s_Emissive, out var isEmissive);
            if (!isEmissive)
            {
                m_Material.SetColor(ID_Emission, Color.black);
            }

            if (s_Emissive)
            {
                DoEmissivePanel();
            }

            EndPanel();

            s_Bevel = BeginPanel("Normal", s_NormalBevelFeature, s_Bevel);
            if (s_Bevel)
            {
                DoBevelPanel();
            }

            EndPanel();
        }

        DrawOrder();
        // base.OnGUI(materialEditor, properties);
    }

    private void DoToggle(string id, GUIContent content)
    {
        var isToggleEnabled = EditorGUILayout.Toggle(content, m_Material.IsKeywordEnabled(id));
        GUI.enabled = isToggleEnabled;
        if (!EditorGUI.EndChangeCheck())
        {
            GUI.enabled = true;
            return;
        }

        m_Editor.RegisterPropertyChangeUndo(content.text);
        if (isToggleEnabled)
        {
            m_Material.EnableKeyword(id);
        }
        else
        {
            m_Material.DisableKeyword(id);
        }

        GUI.enabled = true;
    }

    // private void DoBool(string id, GUIContent content)
    // {
    //     var isToggleEnabled = EditorGUILayout.Toggle(content, m_Material.GetInt(id) == 1);
    //     GUI.enabled = isToggleEnabled;
    //     if (!EditorGUI.EndChangeCheck())
    //     {
    //         GUI.enabled = true;
    //         return;
    //     }
    //
    //     m_Editor.RegisterPropertyChangeUndo(content.text);
    //     m_Material.SetInt(id, isToggleEnabled ? 1 : 0);
    //     GUI.enabled = true;
    // }

    void DoFacePanel()
    {
        EditorGUI.indentLevel += 1;

        DoColor("_FaceColor", "Color");

        if (m_Material.HasProperty("_OutlineSoftness"))
        {
            DoSlider("_OutlineSoftness", "Softness");
        }

        if (m_Material.HasProperty("_FaceDilate"))
        {
            DoSlider("_FaceDilate", "Dilate");
        }

        if (m_Material.HasProperty("_Sharpness"))
        {
            DoSlider("_Sharpness", "Sharpness");
        }

        EditorGUI.indentLevel -= 1;
        EditorGUILayout.Space();
    }

    void DoOutlinePanel()
    {
        EditorGUI.indentLevel += 1;
        DoColor("_OutlineColor", "Color");
        DoSlider("_OutlineWidth", "Thickness");
        EditorGUI.indentLevel -= 1;
        EditorGUILayout.Space();
    }

    private void DoLitPanel()
    {
        // DoColor(ID_Specular, "Specular");
        DoSlider(ID_Smoothness, new Vector2(0, 1), "Smoothness");
        DoSlider(ID_Metallic, new Vector2(0, 1), "Metallic");
    }

    private void DoEmissivePanel()
    {
        DoColor(ID_Emission, "Emission", hdr: true);
    }

    void DoBevelPanel()
    {
        EditorGUI.indentLevel += 1;
        // DoPopup("_BevelType", "Type", s_BevelTypeLabels);
        DoSlider("_NormalBlend", "Normal Blend");
        DoSlider("_Bevel", "Amount");
        DoSlider("_BevelOffset", "Offset");
        DoSlider("_BevelWidth", "Width");
        DoSlider("_BevelRoundness", "Roundness");
        DoSlider("_BevelClamp", "Clamp");
        EditorGUI.indentLevel -= 1;
        EditorGUILayout.Space();
    }

    private void DoColor(string name, string label, bool hdr = false)
    {
        MaterialProperty property = BeginProperty(name);
        s_TempLabel.text = label;
        Color value = EditorGUI.ColorField(EditorGUILayout.GetControlRect(), s_TempLabel, property.colorValue, false,
            true, hdr);
        if (EndProperty())
        {
            property.colorValue = value;
        }
    }

    private void DoSlider(string name, string label)
    {
        MaterialProperty property = BeginProperty(name);
        Vector2 range = property.rangeLimits;
        s_TempLabel.text = label;
        float value = EditorGUI.Slider(EditorGUILayout.GetControlRect(), s_TempLabel, property.floatValue, range.x,
            range.y);
        if (EndProperty())
        {
            property.floatValue = value;
        }
    }

    private void DoSlider(string name, Vector2 range, string label)
    {
        MaterialProperty property = BeginProperty(name);
        s_TempLabel.text = label;
        float value = EditorGUI.Slider(EditorGUILayout.GetControlRect(), s_TempLabel, property.floatValue, range.x,
            range.y);
        if (EndProperty())
        {
            property.floatValue = value;
        }
    }

    private void DrawOrder()
    {
        var property = BeginProperty(ID_DrawOrder);
        s_TempLabel.text = "Priority";
        var queue = EditorGUI.IntSlider(EditorGUILayout.GetControlRect(), s_TempLabel, (int)property.floatValue, -50,
            50);
        if (EndProperty())
        {
            foreach (var target in m_Editor.targets)
            {
                var material = target as Material;
                if (material != null) material.renderQueue = 2000 + queue;
            }

            property.floatValue = queue;
        }
    }

    private MaterialProperty BeginProperty(string name)
    {
        MaterialProperty property = FindProperty(name, m_Properties);
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = property.hasMixedValue;
        m_Editor.BeginAnimatedCheck(Rect.zero, property);

        return property;
    }

    private bool EndProperty()
    {
        m_Editor.EndAnimatedCheck();
        EditorGUI.showMixedValue = false;
        return EditorGUI.EndChangeCheck();
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
        expanded = EditorToggle(r, expanded, new GUIContent(panel), UIStyles.panelTitle);
        r.width -= 30;
        EditorGUI.LabelField(r, new GUIContent(expanded ? s_PanelStateLabel[0] : s_PanelStateLabel[1]),
            UIStyles.rightLabel);
        GUI.enabled = enabled;

        EditorGUI.indentLevel += 1;
        EditorGUI.BeginDisabledGroup(false);

        return expanded;
    }

    protected bool BeginPanel(string panel, ShaderFeature feature, bool expanded, bool readState = true)
    {
        EditorGUI.indentLevel = 0;

        if (readState)
        {
            feature.ReadState(m_Material);
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.BeginHorizontal();

        Rect r = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20f)));
        bool active = EditorGUI.Toggle(r, feature.Active);

        if (EditorGUI.EndChangeCheck())
        {
            m_Editor.RegisterPropertyChangeUndo(feature.undoLabel);
            feature.SetActive(active, m_Material);
        }

        r = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(20, 18));
        r.width += 6;

        bool enabled = GUI.enabled;
        GUI.enabled = true;
        expanded = EditorToggle(r, expanded, new GUIContent(panel), UIStyles.panelTitle);
        r.width -= 10;
        EditorGUI.LabelField(r, new GUIContent(expanded ? s_PanelStateLabel[0] : s_PanelStateLabel[1]),
            UIStyles.rightLabel);
        GUI.enabled = enabled;

        GUILayout.EndHorizontal();

        EditorGUI.indentLevel += 1;
        EditorGUI.BeginDisabledGroup(!active);

        return expanded;
    }

    protected bool BeginPanel(string panel, string shaderPass, bool expanded, out bool active)
    {
        EditorGUI.indentLevel = 0;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.BeginHorizontal();

        Rect r = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20f)));
        active = EditorGUI.Toggle(r, m_Material.GetShaderPassEnabled(shaderPass));

        if (EditorGUI.EndChangeCheck())
        {
            m_Editor.RegisterPropertyChangeUndo(shaderPass);
            m_Material.SetShaderPassEnabled(shaderPass, active);
        }

        r = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(20, 18));
        r.width += 6;

        bool enabled = GUI.enabled;
        GUI.enabled = true;
        expanded = EditorToggle(r, expanded, new GUIContent(panel), UIStyles.panelTitle);
        r.width -= 10;
        EditorGUI.LabelField(r, new GUIContent(expanded ? s_PanelStateLabel[0] : s_PanelStateLabel[1]),
            UIStyles.rightLabel);
        GUI.enabled = enabled;

        GUILayout.EndHorizontal();

        EditorGUI.indentLevel += 1;
        EditorGUI.BeginDisabledGroup(!active);

        return expanded;
    }

    private void EndPanel()
    {
        EditorGUI.EndDisabledGroup();
        EditorGUI.indentLevel -= 1;
        EditorGUILayout.EndVertical();
    }

    private static bool EditorToggle(Rect position, bool value, GUIContent content, GUIStyle style)
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

    /// <summary>Representation of a #pragma shader_feature.</summary>
    /// <description>It is assumed that the first feature option is for no keyword (underscores).</description>
    protected class ShaderFeature
    {
        public string undoLabel;

        public GUIContent label;

        /// <summary>The keyword labels, for display. Include the no-keyword as the first option.</summary>
        public GUIContent[] keywordLabels;

        /// <summary>The shader keywords. Exclude the no-keyword option.</summary>
        public string[] keywords;

        int m_State;

        public bool Active
        {
            get { return m_State >= 0; }
        }

        public int State
        {
            get { return m_State; }
        }

        public void ReadState(Material material)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (material.IsKeywordEnabled(keywords[i]))
                {
                    m_State = i;
                    return;
                }
            }

            m_State = -1;
        }

        public void SetActive(bool active, Material material)
        {
            m_State = active ? 0 : -1;
            SetStateKeywords(material);
        }

        public void DoPopup(MaterialEditor editor, Material material)
        {
            EditorGUI.BeginChangeCheck();
            int selection = EditorGUILayout.Popup(label, m_State + 1, keywordLabels);
            if (EditorGUI.EndChangeCheck())
            {
                m_State = selection - 1;
                editor.RegisterPropertyChangeUndo(undoLabel);
                SetStateKeywords(material);
            }
        }

        private void SetStateKeywords(Material material)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (i == m_State)
                {
                    material.EnableKeyword(keywords[i]);
                }
                else
                {
                    material.DisableKeyword(keywords[i]);
                }
            }
        }
    }

#if UNITY_6000_0_OR_NEWER
    private void DrawRenderingLayerMask(string name, string label, GUIContent style)
    {
        var property = BeginProperty(name);
        s_TempLabel.text = label;
        var renderingLayer = (uint)property.floatValue;
        renderingLayer = EditorGUILayout.RenderingLayerMaskField(style, renderingLayer);
        if (EndProperty())
        {
            property.floatValue = renderingLayer;
        }
    }
#else
    private void DrawRenderingLayerMask(string name, string label, GUIContent style)
    {
        var property = BeginProperty(name);
        s_TempLabel.text = label;

        Rect controlRect = EditorGUILayout.GetControlRect(true);
        var renderingLayer = (int)property.floatValue;

        var renderingLayerMaskNames = new string[32];
        for (var i = 0; i < renderingLayerMaskNames.Length; i++)
        {
            renderingLayerMaskNames[i] = $"Light Layer {i}";
        }

        var maskCount = (int)Mathf.Log(renderingLayer, 2) + 1;
        if (renderingLayerMaskNames.Length < maskCount && maskCount <= 32)
        {
            var newRenderingLayerMaskNames = new string[maskCount];
            for (var i = 0; i < maskCount; ++i)
            {
                newRenderingLayerMaskNames[i] = i < renderingLayerMaskNames.Length
                    ? renderingLayerMaskNames[i]
                    : $"Unused Layer {i}";
            }

            renderingLayerMaskNames = newRenderingLayerMaskNames;

            EditorGUILayout.HelpBox(
                $"One or more of the Rendering Layers is not defined in the Universal Global Settings asset.",
                MessageType.Warning);
        }

        // EditorGUI.BeginProperty(controlRect, style, property);

        // EditorGUI.BeginChangeCheck();
        renderingLayer = EditorGUI.MaskField(controlRect, style, renderingLayer, renderingLayerMaskNames);

        if (EndProperty())
            property.floatValue = renderingLayer;
    }
#endif
}

static class UIStyles
{
    public static GUIStyle panelTitle;
    public static GUIStyle rightLabel;

    static UIStyles()
    {
        rightLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, richText = true };
        panelTitle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
    }
}
}

#endif