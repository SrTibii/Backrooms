Shader "Text Decal/Text Decal SDF URP Lit"
{

    Properties
    {
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0

        _WeightNormal ("Weight Normal", float) = 0
        _WeightBold ("Weight Bold", float) = .5

        _ShaderFlags ("Flags", float) = 0
        _ScaleRatioA ("Scale RatioA", float) = 1
        _ScaleRatioB ("Scale RatioB", float) = 1
        _ScaleRatioC ("Scale RatioC", float) = 1

        _MainTex ("Font Atlas", 2D) = "white" {}
        _TextureWidth ("Texture Width", float) = 512
        _TextureHeight ("Texture Height", float) = 512
        _GradientScale ("Gradient Scale", float) = 5
        _ScaleX ("Scale X", float) = 1
        _ScaleY ("Scale Y", float) = 1
        _PerspectiveFilter ("Perspective Correction", Range(0, 1)) = 0.875
        _Sharpness ("Sharpness", Range(-1,1)) = 0

        _VertexOffsetX ("Vertex OffsetX", float) = 0
        _VertexOffsetY ("Vertex OffsetY", float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _CullMode ("Cull Mode", Float) = 0
        _ColorMask ("Color Mask", Float) = 15

        //        _Specular("Specular", Color) = (0,0,0,1)
        _Smoothness("Smoothness", Range(0,1)) = 0
        _Metallic("Metallic", Range(0,1)) = 0
        _Emission("Emission", Color) = (0,0,0,1)

        _NormalBlend ("Normal Blend", Range(0,1)) = .5
        _Bevel ("Bevel", Range(-1,1)) = 0
        _BevelOffset ("Bevel Offset", Range(-0.5,0.5)) = 0
        _BevelWidth ("Bevel Width", Range(-.5,0.5)) = 0
        _BevelClamp ("Bevel Clamp", Range(0,1)) = 0
        _BevelRoundness ("Bevel Roundness", Range(0,1)) = 0

        _DecalLayerMaskFromDecal("Decal Layer", Float) = 1
        [HideInInspector]_DrawOrder("Draw Order", Range(-50, 50)) = 0
//        _UIVertexColorAlwaysGammaSpace("Vertex color in Gamma Space", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "TextDecalScreenSpacePass"
            Tags
            {
                "LightMode" = "TextDecalScreenSpace"
            }

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            // Pragmas
            #pragma target 4.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma editor_sync_compilation
            #pragma vertex VertShader
            #pragma fragment PixShader

            // Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile _TEXT_DECAL_NORMAL_BLEND_LOW _TEXT_DECAL_NORMAL_BLEND_MEDIUM _TEXT_DECAL_NORMAL_BLEND_HIGH
            #pragma multi_compile _ _DECAL_LAYERS
			#if UNITY_VERSION >= 60010000
				#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#else
				#pragma multi_compile _ _FORWARD_PLUS
			#endif
            
            // Baked GI
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ USE_APV_PROBE_OCCLUSION

            // VR
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON //STEREO_MULTIVIEW_ON

            // Text Mesh Pro
            #pragma shader_feature __ OUTLINE_ON
            #pragma shader_feature __ ANGLE_FADE
            #pragma shader_feature __ AFFECT_NORMAL
            #pragma shader_feature __ TEXT_DECAL_ROTATION
            
            #define AFFECT_BASE_COLOR
            #if defined(AFFECT_NORMAL) || defined(ANGLE_FADE)
                #define DECAL_RECONSTRUCT_NORMAL
            #endif
            
            #define VARYINGS_NEED_STATIC_LIGHTMAP_UV
            #define VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV
            #define REQUIRE_DEPTH_TEXTURE
            #define DECAL_SCREEN_SPACE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            CBUFFER_START(UnityPerMaterial)
            #include "Properties.hlsl"
            float3 _Emission;
            CBUFFER_END

            #include "DecalShader.hlsl"
            #include "DecalLighting.hlsl"

            half4 PixShader(pixel_t input) : SV_Target
            {
                float3 world_pos;
                float angleFade;
                const float4 face_color = DecalPixelShader(input, world_pos, angleFade);
                input.faceColor = face_color;
                DecalSurfaceData surfaceData;
                CalculateLighting(input, world_pos, angleFade, surfaceData);
                return surfaceData.baseColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "TextDecalDBufferPass"
            Tags
            {
                "LightMode" = "TextDecalDBuffer"
            }

            Cull Back
            ZWrite Off
            
            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            ColorMask RGBA
            ColorMask RGBA 1
            ColorMask RGBA 2
            ColorMask RGBA 3

            HLSLPROGRAM
            // Pragmas
            #pragma target 4.0
            #pragma exclude_renderers gles3 glcore
            #pragma multi_compile_instancing
            #pragma editor_sync_compilation
            #pragma vertex VertShader
            #pragma fragment PixShader

            // Keywords
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ _DECAL_LAYERS

            // VR
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON //STEREO_MULTIVIEW_ON

            // Text Mesh Pro
            #pragma shader_feature __ OUTLINE_ON
            #pragma shader_feature __ AFFECT_NORMAL
            #pragma shader_feature __ ANGLE_FADE
            #pragma shader_feature __ TEXT_DECAL_ROTATION

            #if defined(ANGLE_FADE)
                #define DECAL_LOAD_NORMAL
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            #include "Properties.hlsl"
            float3 _Emission;
            CBUFFER_END
            #include "DecalShader.hlsl"
            #include "DecalLighting.hlsl"

            void PixShader(pixel_t input, OUTPUT_DBUFFER(outDBuffer))
            {
                float3 world_pos;
                float angleFade;
                const float4 face_color = DecalPixelShader(input, world_pos, angleFade);
                input.faceColor = face_color;
                DecalSurfaceData surfaceData;
                CalculateLighting(input, world_pos, angleFade, surfaceData);
                ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
            }
            ENDHLSL
        }

        Pass
        {
            Name "TextDecalForwardEmissivePass"
            Tags
            {
                "LightMode" = "TextDecalForwardEmissive"
            }

            Cull Back
            Blend 0 SrcAlpha One
            ZWrite Off

            HLSLPROGRAM
            // Pragmas
            #pragma target 4.0
            #pragma exclude_renderers gles3 glcore
            #pragma multi_compile_instancing
            #pragma editor_sync_compilation

            // Keywords
            #pragma multi_compile _ _DECAL_LAYERS
            #pragma multi_compile _TEXT_DECAL_NORMAL_BLEND_LOW _TEXT_DECAL_NORMAL_BLEND_MEDIUM _TEXT_DECAL_NORMAL_BLEND_HIGH

            // VR
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON //STEREO_MULTIVIEW_ON

            // Text Mesh Pro
            #pragma shader_feature __ OUTLINE_ON
            #pragma shader_feature __ AFFECT_NORMAL
            #pragma shader_feature __ ANGLE_FADE
            #pragma shader_feature __ TEXT_DECAL_ROTATION
            
            #if defined(ANGLE_FADE)
                #define DECAL_RECONSTRUCT_NORMAL
            #endif
            
            #pragma vertex VertShader
            #pragma fragment PixShader

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            #include "Properties.hlsl"
            float3 _Emission;
            CBUFFER_END
            #include "DecalShader.hlsl"

            void PixShader(pixel_t input, out half4 outEmissive : SV_Target0)
            {
                float3 world_pos;
                float3 normalWS;
                float angleFade;
                const float4 face_color = DecalPixelShader(input, world_pos, angleFade);
                outEmissive.rgb = _Emission.rgb;// * GetCurrentExposureMultiplier();
                outEmissive.a = face_color.a;
            }
            ENDHLSL
        }
    }

    CustomEditor "space.chikalin.textdecal.Editor.TextDecalShaderEditor"
}