#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

void InitializeInputData(pixel_t input, float3 positionWS, float3 normalWS, float3 viewDirectionWS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = viewDirectionWS;

#if defined(VARYINGS_NEED_SHADOW_COORD) && defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    // inputData.fogCoord = InitializeInputDataFog(float4(world_pos, 1.0), input.fogFactorAndVertexLight.x);
    // inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), 1);
    inputData.vertexLighting = float3(0, 0, 0);
#endif
    
#if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, float3(input.sh), normalWS);
    #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    #endif
#elif defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.sh,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.position.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, float3(input.vertexSH), normalWS);
    #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    #endif
#endif
#endif

    #if defined(DEBUG_DISPLAY)
    #if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV) && defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #elif defined(VARYINGS_NEED_SH)
    inputData.vertexSH = input.sh;
    #endif
    #if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
    #endif
    #endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.position);
}

void GetDecalSurfaceData(pixel_t input, float angleFade, out DecalSurfaceData surfaceData)
{
    ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
    surfaceData.baseColor = half4(input.faceColor.rgba);
    #if AFFECT_NORMAL
        surfaceData.normalWS = half4(input.normalWS.xyz, _NormalBlend);
    #else
        surfaceData.normalWS = half4(input.normalWS.xyz, 0.0);
    #endif
    surfaceData.occlusion = half(1.0);
    surfaceData.smoothness = half(_Smoothness);
    surfaceData.metallic = half(_Metallic);
    surfaceData.emissive = half3(_Emission);
    surfaceData.MAOSAlpha = input.faceColor.a * angleFade;
}

void GetSurface(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    surfaceData.albedo = decalSurfaceData.baseColor.rgb;
    surfaceData.metallic = saturate(decalSurfaceData.metallic);
    surfaceData.specular = 0;
    surfaceData.smoothness = saturate(decalSurfaceData.smoothness);
    surfaceData.occlusion = decalSurfaceData.occlusion;
    surfaceData.emission = decalSurfaceData.emissive;
    surfaceData.alpha = saturate(decalSurfaceData.baseColor.a);
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
}

void InitializeBakedGIData(pixel_t input, inout InputData inputData)
{
    #if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
        inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, float3(input.sh), normalWS);
        #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
            inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
        #endif
    #elif defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
            #if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                inputData.bakedGI = SAMPLE_GI(input.vertexSH,
                    GetAbsolutePositionWS(inputData.positionWS),
                    inputData.normalWS,
                    inputData.viewDirectionWS,
                    input.position.xy,
                    input.probeOcclusion,
                    inputData.shadowMask);
            #else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, float3(input.vertexSH), inputData.normalWS);
                #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
                    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
                #endif
            #endif
    #endif
}

void CalculateLighting(pixel_t input, float3 positionWS, float angleFade, out DecalSurfaceData surfaceData)
{
    surfaceData = (DecalSurfaceData)0;
    GetDecalSurfaceData(input, angleFade, surfaceData);
    
    #if defined(DECAL_SCREEN_SPACE)
        const float3 view_direction_ws = GetWorldSpaceNormalizeViewDir(positionWS);

        InputData input_data;
        InitializeInputData(input, positionWS, input.normalWS.xyz, view_direction_ws, input_data);

        SurfaceData surface = (SurfaceData)0;
        GetSurface(surfaceData, surface);
    
        float4 face_color = UniversalFragmentPBR(input_data, surface);
        face_color.rgb = MixFog(face_color.rgb, input_data.fogCoord);
        surfaceData.baseColor = face_color;
    #endif
}
