using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
#if UNITY_EDITOR
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif

namespace space.chikalin.textdecal
{
#if UNITY_2023_3_OR_NEWER
[SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
[DisallowMultipleRendererFeature("Text Decal")]
[Tooltip("With this Renderer Feature, Unity can project TextMeshPro objects onto other objects in the Scene.")]
public class TextDecalRendererFeature : ScriptableRendererFeature
{
    internal const string CompatibilityScriptingAPIObsolete =
        "This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.";

    public enum DecalTechnique
    {
        Invalid,
        DBuffer,
        ScreenSpace,
        GBuffer,
    }

    public enum DecalTechniqueOption
    {
        // [Tooltip("Automatically selects technique based on build platform.")]
        // Automatic,

        [Tooltip(
            "Renders decals into DBuffer and then applied during opaque rendering. Requires DepthNormal prepass which makes not viable solution for the tile based renderers common on mobile.")]
        [InspectorName("DBuffer")]
        DBuffer,

        [Tooltip(
            "Renders decals after opaque objects with normal reconstructed from depth. The decals are simply rendered as mesh on top of opaque ones, as result does not support blending per single surface data (etc. normal blending only).")]
        ScreenSpace,
    }

    private CopyDepthPass _copyPass;
    private TextDecalDBufferRenderPass _dBufferPass;
    private TextDecalScreenSpaceRenderPass _screenSpacePass;
    private TextDecalForwardEmissivePass _forwardEmissivePass;

    public DecalSettings settings;
    public bool decalLayers = false;

    private bool _recreate = true;
    private DecalTechniqueOption _technique = DecalTechniqueOption.ScreenSpace;
    

    public enum DecalNormalBlend
    {
        [Tooltip("Low quality of normal reconstruction (Uses 1 sample).")]
        Low,

        [Tooltip("Medium quality of normal reconstruction (Uses 5 samples).")]
        Medium,

        [Tooltip("High quality of normal reconstruction (Uses 9 samples).")]
        High,
    }

    [Serializable]
    public class DecalScreenSpaceSettings
    {
        public DecalNormalBlend normalBlend = DecalNormalBlend.Low;
    }

    [Serializable]
    public class DecalSettings
    {
        public DecalTechniqueOption technique = DecalTechniqueOption.DBuffer;
        // public float maxDrawDistance = 1000f;
        public DecalScreenSpaceSettings screenSpaceSettings;
    }

    public override void Create()
    {
        _recreate = true;
    }

    private void Recreate()
    {
        if (_technique != settings.technique) _recreate = true;
        if (!_recreate) return;

        _recreate = false;
        _technique = settings.technique;
        switch (settings.technique)
        {
            case DecalTechniqueOption.DBuffer:
                // After Decal Renderer Feature
                const RenderPassEvent atEvent = RenderPassEvent.AfterRenderingPrePasses + 2;
                _dBufferPass = new TextDecalDBufferRenderPass(atEvent);
                _forwardEmissivePass = new TextDecalForwardEmissivePass();
#if UNITY_2023_3_OR_NEWER
                var rendererShaders = GraphicsSettings.GetRenderPipelineSettings<UniversalRendererResources>();
                _copyPass = new TextDecalDBufferCopyDepthPass(atEvent,
                    rendererShaders.copyDepthPS, shouldClear: false, copyToDepth: true, copyResolvedDepth: false);
#endif
                break;
            case DecalTechniqueOption.ScreenSpace:
                _screenSpacePass = new TextDecalScreenSpaceRenderPass(settings.screenSpaceSettings);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

#if !UNITY_6000_4_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        switch (settings.technique)
        {
            case DecalTechniqueOption.DBuffer:
                _dBufferPass.Setup(renderingData.cameraData);

#if UNITY_2023_3_OR_NEWER
#pragma warning disable CS0618
                if (renderer is UniversalRenderer universalRenderer)
                    _copyPass.Setup(
                        universalRenderer.cameraDepthTargetHandle,
                        _dBufferPass._dBufferDepthHandle
                    );
#pragma warning restore CS0618
#endif
                break;
            case DecalTechniqueOption.ScreenSpace:
            default:
                break;
        }
    }
#endif
    
    public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
    {
        Recreate();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Recreate();
        switch (settings.technique)
        {
            case DecalTechniqueOption.DBuffer:
#if UNITY_2023_3_OR_NEWER
                renderer.EnqueuePass(_copyPass);
#endif
                renderer.EnqueuePass(_dBufferPass);
                renderer.EnqueuePass(_forwardEmissivePass);
                break;
            case DecalTechniqueOption.ScreenSpace:
                renderer.EnqueuePass(_screenSpacePass);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void Dispose(bool disposing)
    {
        _dBufferPass?.Dispose();
#if UNITY_2023_3_OR_NEWER
        _copyPass?.Dispose();
#endif
    }
}
}