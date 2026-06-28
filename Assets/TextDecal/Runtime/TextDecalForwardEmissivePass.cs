using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif


namespace space.chikalin.textdecal
{
public class TextDecalForwardEmissivePass : ScriptableRenderPass
{
    public const string TextDecalForwardEmissive = "TextDecalForwardEmissive";
    private readonly FilteringSettings _filteringSettings;
    private readonly List<ShaderTagId> _shaderTagIdList;

    public TextDecalForwardEmissivePass()
    {
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

        profilingSampler = new ProfilingSampler("Text Decal Forward Emissive");
        _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

        _shaderTagIdList = new List<ShaderTagId> { new(TextDecalForwardEmissive) };
    }

#if !UNITY_6000_4_OR_NEWER
    [Obsolete(TextDecalRendererFeature.CompatibilityScriptingAPIObsolete, false)]
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera) return;
        var cameraData = renderingData.cameraData;
        var sortingCriteria = cameraData.defaultOpaqueSortFlags;
        var drawingSettings =
            RenderingUtils.CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);
        var param = new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);

        var cmd = CommandBufferPool.Get();
        var rendererList = context.CreateRendererList(ref param);
        using (new ProfilingScope(cmd, profilingSampler))
        {
            cmd.DrawRendererList(rendererList);
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
#endif
    
#if UNITY_2023_3_OR_NEWER
    private class PassData
    {
        public RendererListHandle rendererList;
    }

    private RendererListParams InitRendererListParams(UniversalRenderingData renderingData,
        UniversalCameraData cameraData, UniversalLightData lightData)
    {
        var sortingCriteria = cameraData.defaultOpaqueSortFlags;
        var drawingSettings =
            RenderingUtils.CreateDrawingSettings(_shaderTagIdList, renderingData, cameraData, lightData,
                sortingCriteria);
        return new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var cameraData = frameData.Get<UniversalCameraData>();
        if (cameraData.isPreviewCamera) return;

        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler);
        var resourceData = frameData.Get<UniversalResourceData>();
        var renderingData = frameData.Get<UniversalRenderingData>();
        var lightData = frameData.Get<UniversalLightData>();

        var param = InitRendererListParams(renderingData, cameraData, lightData);
        passData.rendererList = renderGraph.CreateRendererList(param);
        builder.UseRendererList(passData.rendererList);

        builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

        builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
        {
            rgContext.cmd.DrawRendererList(data.rendererList);
        });
    }
#endif
}
}