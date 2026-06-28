#if UNITY_2023_3_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace space.chikalin.textdecal
{
/// <summary>
/// Used by DBuffer to copy depth into different texture.
/// In future this should be replaced by proper depth copy logic, as current CopyDepthPass do not implement RecordRenderGraph.
/// </summary>
internal class TextDecalDBufferCopyDepthPass : CopyDepthPass
{
    public TextDecalDBufferCopyDepthPass(RenderPassEvent evt, Shader copyDepthShader, bool shouldClear = false,
        bool copyToDepth = false, bool copyResolvedDepth = false)
        : base(evt, copyDepthShader, shouldClear, copyToDepth, copyResolvedDepth)
    {
        profilingSampler = new ProfilingSampler("Text Decal CopyDepth");
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var cameraData = frameData.Get<UniversalCameraData>();
        if (cameraData.isPreviewCamera) return;

        var resourceData = frameData.Get<UniversalResourceData>();
        // if dBufferDepth is already created by Decal Feature, then use it
        if (resourceData.dBufferDepth.IsValid()) return;


        // var universalRenderer = cameraData.renderer as UniversalRenderer;
        // We must create a temporary depth buffer for dbuffer rendering if the existing one isn't compatible.
        // The deferred path always has compatible depth
        // The forward path only has compatible depth when depth priming is enabled without MSAA
        // if (!_hasCompatibleDepth)
        {
            var depthDesc = cameraData.cameraTargetDescriptor;
            depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
            depthDesc.depthStencilFormat = cameraData.cameraTargetDescriptor.depthStencilFormat;
            depthDesc.msaaSamples = 1;
            resourceData.dBufferDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc,
                TextDecalDBufferRenderPass.DBufferDepthName, true);

            // Copy the current depth data into the new attachment
            Render(renderGraph, resourceData.dBufferDepth, resourceData.cameraDepthTexture, resourceData, cameraData,
                false, passName);
        }
    }

#if !UNITY_6000_4_OR_NEWER
    [Obsolete(TextDecalRendererFeature.CompatibilityScriptingAPIObsolete, false)]
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera) return;
#if UNITY_2023_3_OR_NEWER && UNITY_EDITOR
        // Render Graph Compatibility mode
#else
        base.Execute(context, ref renderingData);
#endif
    }
#endif
}
}
#endif