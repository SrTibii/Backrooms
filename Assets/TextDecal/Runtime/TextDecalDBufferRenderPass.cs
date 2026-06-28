using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#else
#endif

namespace space.chikalin.textdecal
{
public class TextDecalDBufferRenderPass : ScriptableRenderPass
{
    public static readonly string DBufferDepthName = "DBufferDepth";
    private const int DBufferSize = 3;

    private static readonly string[] DBufferNames =
        { "_DBufferTexture0", "_DBufferTexture1", "_DBufferTexture2", "_DBufferTexture3" };

    private static readonly string ShaderTagId = "TextDecalDBuffer";
    private readonly List<ShaderTagId> _shaderTagIdList;
    private readonly FilteringSettings _filteringSettings;

    public TextDecalDBufferRenderPass(RenderPassEvent atEvent)
    {
        renderPassEvent = atEvent;

        ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        profilingSampler = new ProfilingSampler("Text Decal Draw DBuffer");
        _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
        _shaderTagIdList = new List<ShaderTagId> { new(ShaderTagId) };
        
#if !UNITY_6000_4_OR_NEWER
        _dBufferDepthTargetID = new RenderTargetIdentifier(DBufferDepthName);
        _dBufferTargetIDs = new RenderTargetIdentifier[DBufferSize];
        _dBufferHandles = new RTHandle[DBufferSize];
#endif
    }

#if UNITY_2023_3_OR_NEWER

    private static readonly int s_SSAOTextureID = Shader.PropertyToID("_ScreenSpaceOcclusionTexture");

    private class TextDecalPassData
    {
        public RendererListHandle rendererList;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var cameraData = frameData.Get<UniversalCameraData>();
        if (cameraData.isPreviewCamera) return;

        var renderingData = frameData.Get<UniversalRenderingData>();
        var resourceData = frameData.Get<UniversalResourceData>();
        var lightData = frameData.Get<UniversalLightData>();
        
        
        var cameraDepthTexture = resourceData.cameraDepthTexture;
        var cameraNormalsTexture = resourceData.cameraNormalsTexture;
        

        using var builder =
            renderGraph.AddRasterRenderPass<TextDecalPassData>(passName, out var passData, profilingSampler);

        var dBufferHandles = resourceData.dBuffer;

        if (!dBufferHandles[0].IsValid())
        {
            var desc = cameraData.cameraTargetDescriptor;
            desc.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? GraphicsFormat.R8G8B8A8_SRGB
                : GraphicsFormat.R8G8B8A8_UNorm;
            desc.depthStencilFormat = GraphicsFormat.None;
            desc.msaaSamples = 1;
            dBufferHandles[0] =
                CreateRenderGraphTexture(renderGraph, desc, DBufferNames[0], true, new Color(0, 0, 0, 1));
        }

        // base
        {
            builder.SetRenderAttachment(dBufferHandles[0], 0, flags: AccessFlags.Write);
        }

        if (dBufferHandles.Length >= 2 && dBufferHandles[1].IsValid())
        {
            builder.SetRenderAttachment(dBufferHandles[1], 1, flags: AccessFlags.Write);
        }
        
        if (dBufferHandles.Length >= 3 && dBufferHandles[2].IsValid())
        {
            builder.SetRenderAttachment(dBufferHandles[2], 2, flags: AccessFlags.Write);
        }

        // if (_settings.surfaceData is AlbedoNormal
        //     or AlbedoNormalMAOS)
        // {
        //     if (!dBufferHandles[1].IsValid())
        //     {
        //         var desc = cameraData.cameraTargetDescriptor;
        //         desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
        //         desc.depthStencilFormat = GraphicsFormat.None;
        //         desc.msaaSamples = 1;
        //         dBufferHandles[1] = CreateRenderGraphTexture(renderGraph, desc, DBufferNames[1], true,
        //             new Color(0.5f, 0.5f, 0.5f, 1));
        //     }
        //
        //     builder.SetRenderAttachment(dBufferHandles[1], 1, AccessFlags.Write);
        // }

        // if (_settings.surfaceData is AlbedoNormalMAOS)
        // {
        //     if (!dBufferHandles[2].IsValid())
        //     {
        //         var desc = cameraData.cameraTargetDescriptor;
        //         desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
        //         desc.depthStencilFormat = GraphicsFormat.None;
        //         desc.msaaSamples = 1;
        //         dBufferHandles[2] =
        //             CreateRenderGraphTexture(renderGraph, desc, DBufferNames[1], true, new Color(0, 0, 0, 1));
        //     }
        //
        //     builder.SetRenderAttachment(dBufferHandles[2], 2, AccessFlags.Write);
        // }

        builder.SetRenderAttachmentDepth(
            resourceData.dBufferDepth.IsValid() ? resourceData.dBufferDepth : resourceData.activeDepthTexture,
            AccessFlags.Read);

        if (cameraDepthTexture.IsValid())
            builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
        if (cameraNormalsTexture.IsValid())
            builder.UseTexture(cameraNormalsTexture, AccessFlags.Read);
        if (resourceData.renderingLayersTexture.IsValid())
            builder.UseTexture(resourceData.renderingLayersTexture, AccessFlags.Read);

        if (resourceData.ssaoTexture.IsValid())
            builder.UseGlobalTexture(s_SSAOTextureID);

        var param = InitRendererListParams(renderingData, cameraData, lightData);
        passData.rendererList = renderGraph.CreateRendererList(param);
        builder.UseRendererList(passData.rendererList);

        for (var i = 0; i < DBufferSize; ++i)
        {
            if (dBufferHandles[i].IsValid())
                builder.SetGlobalTextureAfterPass(dBufferHandles[i], Shader.PropertyToID(DBufferNames[i]));
        }

        builder.AllowPassCulling(false);
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc((TextDecalPassData data, RasterGraphContext rgContext) =>
        {
            rgContext.cmd.DrawRendererList(data.rendererList);
        });

        resourceData.dBuffer = dBufferHandles;
    }

    private RendererListParams InitRendererListParams(UniversalRenderingData renderingData,
        UniversalCameraData cameraData, UniversalLightData lightData)
    {
        var sortingCriteria = cameraData.defaultOpaqueSortFlags;
        var drawingSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIdList, renderingData,
            cameraData, lightData, sortingCriteria);
        return new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);
    }

    private static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, RenderTextureDescriptor desc,
        string name, bool clear, Color color,
        FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
    {
        TextureDesc rgDesc = new TextureDesc(desc.width, desc.height);
        rgDesc.dimension = desc.dimension;
        rgDesc.clearBuffer = clear;
        rgDesc.clearColor = color;
        rgDesc.bindTextureMS = desc.bindMS;
        rgDesc.format = (desc.depthStencilFormat != GraphicsFormat.None)
            ? desc.depthStencilFormat
            : desc.graphicsFormat;
        rgDesc.slices = desc.volumeDepth;
        rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
        rgDesc.name = name;
        rgDesc.enableRandomWrite = desc.enableRandomWrite;
        rgDesc.filterMode = filterMode;
        rgDesc.wrapMode = wrapMode;
        rgDesc.useDynamicScale = desc.useDynamicScale;
        rgDesc.useDynamicScaleExplicit = desc.useDynamicScaleExplicit;

        return renderGraph.CreateTexture(rgDesc);
    }
#endif

    #region Render Graph Compatibility section
#if !UNITY_6000_4_OR_NEWER
    public RTHandle _dBufferDepthHandle;
    private readonly RTHandle[] _dBufferHandles;
    private readonly RenderTargetIdentifier _dBufferDepthTargetID;
    private readonly RenderTargetIdentifier[] _dBufferTargetIDs;

    public void Setup(CameraData cameraData)
    {
        _dBufferDepthHandle ??= RTHandles.Alloc(_dBufferDepthTargetID, DBufferDepthName);
        
        AddDBufferHandle(0);
        AddDBufferHandle(1);
        AddDBufferHandle(2);
    }

    private void AddDBufferHandle(int i)
    {
        if (_dBufferHandles[i] != null) return;
        _dBufferTargetIDs[i] = new RenderTargetIdentifier(DBufferNames[i]);
        _dBufferHandles[i] = RTHandles.Alloc(_dBufferTargetIDs[i], DBufferNames[i]);
    }

    [Obsolete(TextDecalRendererFeature.CompatibilityScriptingAPIObsolete, false)]
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera) return;
#pragma warning disable CS0618
        ConfigureTarget(_dBufferHandles, _dBufferDepthHandle);
#pragma warning restore CS0618
    }

    [Obsolete(TextDecalRendererFeature.CompatibilityScriptingAPIObsolete, false)]
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera) return;
        var cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, profilingSampler))
        {

            var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);
            var param = new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);
            var rendererList = context.CreateRendererList(ref param);
            cmd.DrawRendererList(rendererList);
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
#endif
    
    public void Dispose()
    {
#if !UNITY_6000_4_OR_NEWER
        _dBufferDepthHandle?.Release();
        foreach (var handle in _dBufferHandles)
        {
            handle?.Release();
        }
#endif
    }

    #endregion
}
}