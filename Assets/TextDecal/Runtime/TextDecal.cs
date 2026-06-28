using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace space.chikalin.textdecal
{
/// <summary>
///
/// Should execute after <c>TMPro.TextMeshPro</c> script
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-80)]
[AddComponentMenu("Rendering/Text Decal")]
[HelpURL("https://assetstore.unity.com/packages/tools/level-design/text-decal-302653")]
[RequireComponent(typeof(TMP_Text))]
#if UNITY_2021_2_OR_NEWER
[Icon("Packages/space.chikalin.textdecal/Editor/Resources/Icons/textdecalicon.png")]
#endif
public class TextDecal : MonoBehaviour
{
    [Serializable]
    public class TextDecalSettings
    {
        [Tooltip("Volumetric mesh depth")] public float projectionDepth = 1;
        
        public float startAngleFade = 180f; 
        public float endAngleFade = 180f;

        public static UVChannel vertexDataDefault = UVChannel.TEXCOORD5;
        public static UVChannel UVDataDefault = UVChannel.TEXCOORD6;
        public static UVChannel extraDataDefault = UVChannel.TEXCOORD7;

        public bool useDefaultUV = true;

        [Tooltip("Default value is TEXCOORD5")]
        public UVChannel vertexData;

        [Tooltip("Default value is TEXCOORD6")]
        public UVChannel UVData;

        [Tooltip("Extra data: angle fade start, angle fade end. Default value is TEXCOORD7")]
        public UVChannel extraData;
    }

    [SerializeField] public TextDecalSettings settings = new();

    private TMP_Text _text;
    private readonly Dictionary<int, TextDecalMeshInfo> _decalMeshInfo = new();
    private readonly Dictionary<Mesh, MeshFilter> _subTextMeshes = new();

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        _text.OnPreRenderText += OnPreRenderText;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChangedEvent);
        OnPreRenderText(_text.textInfo);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChangedEvent);
        _text.OnPreRenderText -= OnPreRenderText;

        _text.ForceMeshUpdate();

        var textInfoMeshInfo = _text.textInfo.meshInfo;
        for (var i = 0; i < textInfoMeshInfo.Length; i++)
        {
            if (!_decalMeshInfo.ContainsKey(i)) continue;

            var meshInfo = textInfoMeshInfo[i];
            var textDecalMeshInfo = _decalMeshInfo[i];
            textDecalMeshInfo.Mesh = meshInfo.mesh;
            if (textDecalMeshInfo.VolumetricMesh != null)
            {
                DestroyImmediate(textDecalMeshInfo.VolumetricMesh);
            }

            textDecalMeshInfo.Dispose();
        }
        // MeshFilter.sharedMesh = _text.mesh;
        // if (_volumetricMesh != null)
        // {
        // DestroyImmediate(_volumetricMesh);
        // }
    }

    private void OnTextChangedEvent(Object obj)
    {
        if (obj != _text || !string.IsNullOrEmpty(_text.text)) return;

        // force update if text is empty
        OnPreRenderText(_text.textInfo);
    }

    private void OnPreRenderText(TMP_TextInfo textInfo)
    {
        if (textInfo == null)
        {
            Debug.LogError("TMP_TextInfo is null. Please <b>import the TMP Essential Resources</b>.");
            return;
        }

        PrepareMeshData(textInfo);
        for (var i = 0; i < textInfo.characterCount; i++)
        {
            var characterInfo = textInfo.characterInfo[i];
            if (!characterInfo.isVisible) continue;
            var materialIndex = characterInfo.materialReferenceIndex;
            var meshInfo = textInfo.meshInfo[materialIndex];
            if (_decalMeshInfo.ContainsKey(materialIndex))
            {
                _decalMeshInfo[materialIndex].AddCharacter(characterInfo, meshInfo, settings);
            }
        }

        foreach (var decalMeshInfo in _decalMeshInfo.Values)
        {
            decalMeshInfo.UpdateMesh(settings);
        }
    }


    private void PrepareMeshData(TMP_TextInfo textInfo)
    {
        _subTextMeshes.Clear();
        var subTextObjects = GetComponentsInChildren<TMP_SubMesh>();
        foreach (var subTextObject in subTextObjects)
        {
            _subTextMeshes.Add(subTextObject.mesh, subTextObject.meshFilter);
        }
        _subTextMeshes.Add(textInfo.meshInfo[0].mesh, GetComponent<MeshFilter>());
        
        for (var i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var tmpMeshInfo = textInfo.meshInfo[i];

            if (!_subTextMeshes.ContainsKey(tmpMeshInfo.mesh)) continue;
            if (!_decalMeshInfo.ContainsKey(i))
            {
                _decalMeshInfo.Add(i, new TextDecalMeshInfo());
            }

            _decalMeshInfo[i].PrepareMeshData(tmpMeshInfo.vertexCount, _subTextMeshes[tmpMeshInfo.mesh]);
        }
    }

    public void ForceDecalUpdate()
    {
        OnPreRenderText(_text.textInfo);
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_text != null)
        {
            _text.ForceMeshUpdate();
        }
    }
#endif
}

public enum UVChannel
{
    TEXCOORD0,
    TEXCOORD1,
    TEXCOORD2,
    TEXCOORD3,
    TEXCOORD4,
    TEXCOORD5,
    TEXCOORD6,
    TEXCOORD7
}
}