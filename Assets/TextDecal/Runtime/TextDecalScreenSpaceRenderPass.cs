using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using static space.chikalin.textdecal.TextDecalRendererFeature;

#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace space.chikalin.textdecal
{
public class TextDecalScreenSpaceRenderPass : ScriptableRenderPass
{
    private static string ShaderTagId = "TextDecalScreenSpace";

    private List<ShaderTagId> m_ShaderTagIdList;

    private FilteringSettings m_FilteringSettings;

    private readonly DecalScreenSpaceSettings _settings;
    
    private static GlobalKeyword? _DecalNormalBlendLow;
    private static GlobalKeyword? _DecalNormalBlendMedium;
    private static GlobalKeyword? _DecalNormalBlendHigh;

    private static GlobalKeyword DecalNormalBlendLow =>
        _DecalNormalBlendLow ??= GlobalKeyword.Create("_TEXT_DECAL_NORMAL_BLEND_LOW");

    private static GlobalKeyword DecalNormalBlendMedium =>
        _DecalNormalBlendMedium ??= GlobalKeyword.Create("_TEXT_DECAL_NORMAL_BLEND_MEDIUM");

    private static GlobalKeyword DecalNormalBlendHigh =>
        _DecalNormalBlendHigh ??= GlobalKeyword.Create("_TEXT_DECAL_NORMAL_BLEND_HIGH");


    public TextDecalScreenSpaceRenderPass(DecalScreenSpaceSettings settings)
    {
        _settings = settings;
        renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        ConfigureInput(ScriptableRenderPassInput.Depth);
        profilingSampler = new ProfilingSampler("Text Decal Draw Screen Space");
        m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
        m_ShaderTagIdList = new List<ShaderTagId> { new(ShaderTagId) };
    }


#if UNITY_2023_3_OR_NEWER

    private class TextDecalPassData
    {
        public RendererListHandle rendererList;
        public UniversalCameraData cameraData;

        public DecalScreenSpaceSettings settings;
        // public TextureHandle colorTarget;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var cameraData = frameData.Get<UniversalCameraData>();
        if (cameraData.isPreviewCamera) return;
        
        var resourceData = frameData.Get<UniversalResourceData>();

        var cameraDepthTexture = resourceData.cameraDepthTexture;

        using var builder =
            renderGraph.AddRasterRenderPass<TextDecalPassData>(passName, out var passData, profilingSampler);
        var renderingData = frameData.Get<UniversalRenderingData>();
        var lightData = frameData.Get<UniversalLightData>();

        passData.cameraData = cameraData;
        // passData.colorTarget = resourceData.cameraColor;
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);


        var param = InitRendererListParams(renderingData, passData.cameraData, lightData);
        passData.rendererList = renderGraph.CreateRendererList(param);
        passData.settings = _settings; 
        builder.UseRendererList(passData.rendererList);

        if (cameraDepthTexture.IsValid())
            builder.UseTexture(cameraDepthTexture, AccessFlags.Read);

        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc((TextDecalPassData data, RasterGraphContext rgContext) =>
        {
            // RenderingUtils.SetScaleBiasRt(rgContext.cmd, in data.cameraData, data.colorTarget);
            ExecutePass(rgContext.cmd, data);
        });
    }

    private RendererListParams InitRendererListParams(UniversalRenderingData renderingData,
        UniversalCameraData cameraData, UniversalLightData lightData)
    {
        var sortingCriteria = SortingCriteria.None;
        var drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData,
            cameraData, lightData, sortingCriteria);
        return new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
    }

    private static void ExecutePass(RasterCommandBuffer cmd, TextDecalPassData passData)
    {
        NormalReconstruction.SetupProperties(cmd, passData.cameraData);

        cmd.SetKeyword(DecalNormalBlendLow, passData.settings.normalBlend == DecalNormalBlend.Low);
        cmd.SetKeyword(DecalNormalBlendMedium, passData.settings.normalBlend == DecalNormalBlend.Medium);
        cmd.SetKeyword(DecalNormalBlendHigh, passData.settings.normalBlend == DecalNormalBlend.High);
        
        cmd.DrawRendererList(passData.rendererList);
    }


#endif
#if !UNITY_6000_4_OR_NEWER
    [Obsolete(CompatibilityScriptingAPIObsolete, false)]
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera) return;
        var cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, profilingSampler))
        {
            var drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.None);
            var param = new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
            var rendererList = context.CreateRendererList(ref param);
            cmd.DrawRendererList(rendererList);
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
#endif
}
}