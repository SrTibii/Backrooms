#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#include "DecalFunctions.hlsl"

#ifdef DECAL_RECONSTRUCT_NORMAL
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
#endif

#if defined(DECAL_LOAD_NORMAL)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#endif

struct vertex_t
{
    UNITY_VERTEX_INPUT_INSTANCE_ID
    float4 position : POSITION;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float4 color : COLOR;
    float4 texcoord0 : TEXCOORD0;

    #if defined(DECAL_SCREEN_SPACE)
    #if defined(LIGHTMAP_ON)
            float2 staticLightmapUV: TEXCOORD1;
    #endif
    #if defined(DYNAMICLIGHTMAP_ON)
            float2 dynamicLightmapUV: TEXCOORD2;
    #endif
    #if !defined(LIGHTMAP_ON)
    float3 sh: TEXCOORD3;
    #endif
    #endif

    #if defined(USE_APV_PROBE_OCCLUSION)
        float4 probeOcclusion : TEXCOORD4;
    #endif

    float4 vert_data : TEXCOORD5; // decal vert_data
    float4 uv_data : TEXCOORD6; // decal uv_data
    float4 extra_data : TEXCOORD7; // decal extra_data
};

struct pixel_t
{
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
    float4 position : SV_POSITION;
    float4 faceColor : COLOR;
    float4 outlineColor : COLOR1;
    float4 texcoord0 : TEXCOORD0;
    float4 param : TEXCOORD1;
    float3 normalWS : NORMAL;
    float3 tangentOS : TANGENT;
    #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
        DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 2);
    #endif
    #if defined(DYNAMICLIGHTMAP_ON) && defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV)
        float2 dynamicLightmapUV : TEXCOORD3;
    #endif
    float4 vert_data : TEXCOORD4; // vertex left bottom position (x, y, z), vertex width (w)
    float4 uv_data : TEXCOORD5; // UV left bottom position (x, y), UV width (z), size ratio (w)
    float4 extra_data : TEXCOORD6;
};

bool IsGammaSpace()
{
    #ifdef UNITY_COLORSPACE_GAMMA
    return true;
    #else
    return false;
    #endif
}

// This piecewise approximation has a precision better than 0.5 / 255 in gamma space over the [0..255] range
// i.e. abs(l2g_exact(g2l_approx(value)) - value) < 0.5 / 255
// It is much more precise than GammaToLinearSpace but remains relatively cheap
half3 UIGammaToLinear(half3 value)
{
    half3 low = 0.0849710 * value - 0.000163029;
    half3 high = value * (value * (value * 0.265885 + 0.736584) - 0.00980184) + 0.00319697;

    // We should be 0.5 away from any actual gamma value stored in an 8 bit channel
    const half3 split = (half3)0.0725490; // Equals 18.5 / 255
    return (value < split) ? low : high;
}

pixel_t VertShader(vertex_t input)
{
    pixel_t output = (pixel_t)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float bold = step(input.texcoord0.w, 0);

    float4 vert = input.position;
    vert.x += _VertexOffsetX;
    vert.y += _VertexOffsetY;

    const float4 vPosition = GetVertexPositionInputs(vert.xyz).positionCS;

    float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
    weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

    float4 color = input.color;
    // if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
    if (!IsGammaSpace())
    {
        color.rgb = UIGammaToLinear(color.rgb);
    }
    
    float4 faceColor = color * _FaceColor;

    float4 outlineColor = _OutlineColor;
    outlineColor.a *= faceColor.a;
    outlineColor.rgb *= outlineColor.a;

    VertexNormalInputs normInputs = GetVertexNormalInputs(input.normal);
    output.normalWS = normalize(normInputs.normalWS);
    output.tangentOS = input.tangent;
    output.position = vPosition;
    output.faceColor = faceColor;
    output.outlineColor = outlineColor;
    output.texcoord0 = float4(input.texcoord0.xy, 0, 0);
    output.param = float4(0.5 - weight, 0, _OutlineWidth * _ScaleRatioA * 0.5, 0);

    #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV) && defined(LIGHTMAP_ON)
        OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    #endif

    #if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
        output.dynamicLightmapUV.xy = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    #if defined(VARYINGS_NEED_SH) && !defined(LIGHTMAP_ON)
        output.vertexSH = float3(SampleSHVertex(float3(output.normalWS)));
    #endif

    // decal
    output.extra_data = input.extra_data;
    output.vert_data = input.vert_data;
    output.uv_data = input.uv_data;

    return output;
}

void CalculateDecalUV_float(float3 normalWS, float3 tangentOS, float3 positionCS, float4 meshData, float4 uvData, float4 quaternion,
                            float maxZ, out float3 world_pos, out float3 decalUV)
{
    const float2 uv = (positionCS.xy / _ScaledScreenParams.xy);
    #if UNITY_REVERSED_Z
    float depth = SampleSceneDepth(uv);
    #else
    // Adjust Z to match NDC for OpenGL ([-1, 1])
    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
    #endif
    world_pos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
    const float3 local_pos = TransformWorldToObject(world_pos);
    clip(maxZ - local_pos.z);
    CalculateDecal_float(normalWS, tangentOS, local_pos, meshData, uvData, quaternion, decalUV);
}

float ComputeAngleFade(float3 normalWS, float3 decalNormal, float angleFadeStart, float angleFadeEnd)
{
    float fade = 1;
    if (angleFadeEnd < 0.0f) // if angle fade is enabled
    {
        half dotAngle = dot(normalWS, decalNormal);
        fade = saturate(angleFadeStart + angleFadeEnd * (dotAngle * (dotAngle - 2.0)));
    }
    return fade;
}

float4 DecalPixelShader(inout pixel_t input, out float3 world_pos, out float angleFade)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    DecalLayerClip_float(input.position.xyz);
    
    float3 decalUV;
    float3 decalNormal = input.normalWS;
    CalculateDecalUV_float(input.normalWS, input.tangentOS, input.position.xyz, input.vert_data, input.uv_data, input.extra_data,
        input.extra_data.z, world_pos, decalUV);
    angleFade = 1;
    // const float3 to_camera = normalize(GetCameraPosition(unity_StereoEyeIndex) - world_pos);
    // if (dot(input.normalWS, to_camera) < 0)
    // {
    //     // return float4(0, 0, 0, 0);
    // }

    float d = tex2D(_MainTex, decalUV.xy).a;
    const float2 step = float2(ddx(decalUV.x), ddy(decalUV.y));

    const float s = rsqrt(abs(step.x * step.y - ddy(decalUV.x) * ddx(decalUV.y)));
    float scale = s / _TextureWidth * _GradientScale * (_Sharpness + 1);
    scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
    float4 faceColor = input.faceColor * saturate((d - input.param.x) * scale + 0.5);

    #if defined(DECAL_RECONSTRUCT_NORMAL)
        #if defined(_TEXT_DECAL_NORMAL_BLEND_HIGH)
            input.normalWS = ReconstructNormalTap9(input.position.xy);
        #elif defined(_TEXT_DECAL_NORMAL_BLEND_MEDIUM)
            input.normalWS = ReconstructNormalTap5(input.position.xy);
        #else
            input.normalWS = ReconstructNormalDerivative(input.position.xy);
        #endif
    #elif defined(DECAL_LOAD_NORMAL)
        input.normalWS = half3(LoadSceneNormals(input.position.xy));
    #endif
    
    #if OUTLINE_ON
    float4 outlineColor = lerp(input.faceColor, input.outlineColor, sqrt(min(1.0, input.param.z * scale * 2)));
    faceColor = lerp(outlineColor, input.faceColor, saturate((d - input.param.x - input.param.z) * scale + 0.5));
    faceColor *= saturate((d - input.param.x + input.param.z) * scale + 0.5);
    #endif

    angleFade = 1.0;
    #if ANGLE_FADE
    angleFade = ComputeAngleFade(input.normalWS, decalNormal, input.extra_data.x, input.extra_data.y);
    #endif

    faceColor.a *= angleFade;
    clip(faceColor.a - 0.001);

    #if AFFECT_NORMAL
    const float3 dxy = float3(.5 / _TextureWidth, .5 / _TextureHeight, 0);
    float3 n = GetSurfaceNormal(decalUV.xy, input.param.w, dxy, decalNormal, input.tangentOS);
    input.normalWS = lerp(input.normalWS.xyz, n.xyz, _NormalBlend).xyz;
    #endif

    return faceColor;
}
