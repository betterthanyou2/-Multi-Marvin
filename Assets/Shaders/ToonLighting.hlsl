
#ifndef TOON_LIGHTING_INCLUDED
#define TOON_LIGHTING_INCLUDED

#if !defined(SHADERGRAPH_PREVIEW)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#endif

void ToonLighting_float(
    float3 PositionWS,
    float3 NormalWS,
    out float3 LightColor,
    out float Diffuse
)
{
#if defined(SHADERGRAPH_PREVIEW)

    // Simple fallback for Shader Graph previews
    LightColor = float3(1, 1, 1);
    Diffuse = 0.5;

#else

    // Calculate shadow coordinates
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);

    // Retrieve the main directional light
    Light mainLight = GetMainLight(shadowCoord);

    // Calculate Lambert diffuse lighting
    float3 normal = normalize(NormalWS);
    float3 lightDir = normalize(mainLight.direction);

    Diffuse = saturate(dot(normal, lightDir));

    // Apply real-time shadows
    Diffuse *= mainLight.shadowAttenuation;

    LightColor = mainLight.color;

#endif
}

#endif
