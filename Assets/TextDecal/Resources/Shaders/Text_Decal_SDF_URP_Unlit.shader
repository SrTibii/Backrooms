Shader "Text Decal/Text Decal SDF URP Unlit"
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

        _NormalBlend ("Normal Blend", Range(0,1)) = .5
        _Bevel ("Bevel", Range(-1,1)) = 0
        _BevelOffset ("Bevel Offset", Range(-0.5,0.5)) = 0
        _BevelWidth ("Bevel Width", Range(-.5,0.5)) = 0
        _BevelClamp ("Bevel Clamp", Range(0,1)) = 0
        _BevelRoundness ("Bevel Roundness", Range(0,1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _CullMode ("Cull Mode", Float) = 0
        _ColorMask ("Color Mask", Float) = 15

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
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _DECAL_LAYERS

            #pragma vertex VertShader
            #pragma fragment PixShader

            #define DECAL_SCREEN_SPACE
            #define REQUIRE_DEPTH_TEXTURE

            // VR
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON //STEREO_MULTIVIEW_ON

            // Text Mesh Pro
            #pragma shader_feature __ OUTLINE_ON
            #pragma shader_feature __ AFFECT_NORMAL
            #pragma shader_feature __ ANGLE_FADE
            #pragma shader_feature __ TEXT_DECAL_ROTATION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            #include "Properties.hlsl"
            CBUFFER_END
            #include "DecalShader.hlsl"

            float4 PixShader(pixel_t input) : SV_Target
            {
                float3 world_pos;
                float angleFade;
                return DecalPixelShader(input, world_pos, angleFade);
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
            #pragma vertex VertShader
            #pragma fragment PixShader
            #pragma multi_compile_instancing
            #pragma editor_sync_compilation

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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            #include "Properties.hlsl"
            CBUFFER_END
            #include "DecalShader.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            void PixShader(pixel_t input, OUTPUT_DBUFFER(outDBuffer))
            {
                float3 world_pos;
                float angleFade;
                const float4 faceColor = DecalPixelShader(input, world_pos, angleFade);
                DecalSurfaceData surfaceData = (DecalSurfaceData)0;
                surfaceData.baseColor = faceColor.rgba;
                surfaceData.normalWS = half4(input.normalWS.xyz, _NormalBlend);
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

            // VR
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON //STEREO_MULTIVIEW_ON

            // Text Mesh Pro
            #pragma shader_feature __ OUTLINE_ON
            #pragma shader_feature __ AFFECT_NORMAL
            #pragma shader_feature __ ANGLE_FADE
            #pragma shader_feature __ TEXT_DECAL_ROTATION
            
            #pragma vertex VertShader
            #pragma fragment PixShader

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            #include "Properties.hlsl"
            CBUFFER_END
            #include "DecalShader.hlsl"

            void PixShader(pixel_t input, out half4 outEmissive : SV_Target0)
            {
                float3 world_pos;
                float3 normalWS;
                float angleFade;
                const float4 face_color = DecalPixelShader(input, world_pos, angleFade);
                outEmissive.rgb = face_color.rgb;// * GetCurrentExposureMultiplier();
                outEmissive.a = face_color.a;
            }
            ENDHLSL
        }
    }

    CustomEditor "space.chikalin.textdecal.Editor.TextDecalShaderEditor"
}