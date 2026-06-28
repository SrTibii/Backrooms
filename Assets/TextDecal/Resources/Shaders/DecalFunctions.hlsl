#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"

float invLerp(const float from, const float to, const float value)
{
    return (value - from) / (to - from);
}

float remap(const float origFrom, const float origTo, const float targetFrom, const float targetTo, const float value)
{
    const float rel = invLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}

float3 rotate(float3 v, float3 normalWS, float3 tangentOS)
{
    const float3 normalOS  = normalize(TransformWorldToObjectNormal(normalWS));
    tangentOS = normalize(tangentOS);
    const float3 bitangentOS = cross(normalOS, tangentOS);
    const float3x3 rotation = float3x3(tangentOS,  bitangentOS, normalOS);
    return mul(rotation, v);
}

float3 GetSurfaceNormal(float4 h, float bias, float3 forward, float3 right)
{
    bool raisedBevel = step(1, fmod(_ShaderFlags, 2));

    h += bias + _BevelOffset;

    float bevelWidth = max(.01, _OutlineWidth + _BevelWidth);

    // Track outline
    h -= .5;
    h /= bevelWidth;
    h = saturate(h + .5);

    if (raisedBevel) h = 1.0 - abs(h * 2.0 - 1.0);
    h = lerp(h, sin(h * 3.141592 / 2.0), _BevelRoundness);
    h = min(h, 1.0 - _BevelClamp);
    h *= _Bevel * bevelWidth * _GradientScale * -2.0;
    
    forward = normalize(forward);
    right = normalize(TransformObjectToWorldNormal(right));
    const float3 bitangent = normalize(cross(forward, right));
    const float3 va = normalize(right + forward * (h.y - h.x));
    const float3 vb = normalize(bitangent + forward * (h.w - h.z));
    const float3 n = normalize(cross(va, vb));
    return n;
}


float3 GetSurfaceNormal(float2 uv, float bias, float3 delta, float3 forward, float3 right)
{
    // Read "height field"
    const float4 h = {
        tex2D(_MainTex, uv - delta.xz).a,
        tex2D(_MainTex, uv + delta.xz).a,
        tex2D(_MainTex, uv - delta.zy).a,
        tex2D(_MainTex, uv + delta.zy).a
    };

    return GetSurfaceNormal(h, bias, forward, right);
}

void CalculateDecal_float(float3 normalWS, float3 tangentOS, float3 position, float4 meshData, float4 uvData, float4 quaternion,
                          out float3 decalUV)
{
    float3 local_pos = position - meshData.xyz;
    #if TEXT_DECAL_ROTATION
        local_pos = rotate(local_pos, -normalWS, tangentOS);
    #endif
    const float width = meshData.w;
    const float height = meshData.w * uvData.w;
    local_pos.x += width / 2;
    local_pos.y += height / 2;

    const float3 decalClip = float3(
        step(0, local_pos.x) * (1 - step(width, local_pos.x)),
        step(0, local_pos.y) * (1 - step(height, local_pos.y)),
        1);
    clip(decalClip.x * decalClip.y * decalClip.z - .001);

    // remap from (0..vertex_size) to (uv.start..uv.end)
    const float2 uvSize = float2(uvData.z, uvData.z * uvData.w);
    decalUV = float3(
        remap(0, width, uvData.x, uvData.x + uvSize.x, local_pos.x),
        remap(0, height, uvData.y, uvData.y + uvSize.y, local_pos.y),
        local_pos.z);
}

void DecalLayerClip_float(float3 positionCS)
{
    #ifdef _RENDER_PASS_ENABLED
        uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, positionCS.xy).r);
    #else
        uint surfaceRenderingLayer = LoadSceneRenderingLayer(positionCS.xy);
    #endif
    #ifdef _DECAL_LAYERS
        uint projectorRenderingLayer = _DecalLayerMaskFromDecal;
        // This is simple trick to clip if there is no matching layers
        // Part (surfaceRenderingLayer & projectorRenderingLayer) will produce 0, 1, 2 ...
        // Finally we subtract with small value to remmap only zero to negative value
        clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
    #endif
}

void DecalClip_float(float4 color, float3 decal_clip, out float4 rgba)
{
    rgba = color * decal_clip.x * decal_clip.y * decal_clip.z;
}

// float3 GetCameraPosition(uint eyeIndex)
// {
//     #ifdef UNITY_SINGLE_PASS_STEREO
//         float3 cameraPos = unity_StereoEyePos[eyeIndex]; 
//     #else
//         float3 cameraPos = _WorldSpaceCameraPos;
//     #endif
//     return cameraPos;
// }

float4 SRGBToLinear2(float4 rgba)
{
    return float4(lerp(rgba.rgb / 12.92f, pow((rgba.rgb + 0.055f) / 1.055f, 2.4f), step(0.04045f, rgba.rgb)), rgba.a);
}
