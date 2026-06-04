
// ...

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

// https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-shadows.html
// https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-additional-lights-fplus.html

//#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

// Assign these via script.

int _FrameCount;

// Can't use the name _AdditionalLightsCount (with plural 'Lights')
// because it would be a redefinition of an existing variable that
// I can't use because it appears to be used by Unity.

uint _AdditionalLightCount;

// ...

float4 _AmbientLighting;

#pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

float3 SampleAPV(float3 positionWS, float3 viewDirectionWS, float2 uvSS)
{
    float3 bakedGI = 0.0;

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    
    // For fog, there is no real surface normal.
    // Use something stable: up vector, or -viewDirWS for camera-facing fog.
    
    float3 normalWS = -viewDirectionWS;
    uint renderingLayer = 0xFFFFFFFF;

    EvaluateAdaptiveProbeVolume(positionWS, normalWS, viewDirectionWS, uvSS, renderingLayer, bakedGI);
    
#endif

    return bakedGI;
}

// ...

// Henyey–Greenstein anisotropic phase function.
// Would optimization via pre-compute really matter?

// Compiler may take care of it...
// Else, I'll need to pass in pre-computed values for anisotropySqr, etc.

float HenyeyGreensteinPhase(float VdotL, float anisotropy)
{
    float g = anisotropy;
    float g2 = g * g;

    float denom = 1.0 + g2 - (2.0 * g * VdotL);

    return (1.0 - g2) / (4.0 * PI * denom * sqrt(denom));
}

// ...

float GetVolumetricFogDensity(float3 positionWS)
{
    // TO-DO: noise, detail, height, etc.
    
    return 1.0;
}

struct SampleData
{    
    float scale;
    
    float2 tiling;    
    float2 offset;
    
    float2 animation;
    
    float4 SampleTexture(Texture2D tex, float2 uv)
    {
        uv *= scale;
        uv *= tiling;
        
        uv -= offset;
        
        uv -= animation * _Time.y;
        
        return SAMPLE_TEXTURE2D(tex, sampler_LinearRepeat, uv);
    }
};

// Height fog.

float GetVolumetricFogDensity(float3 positionWS, float heightDistance, float heightOffset, float heightFalloff)
{    
    // Height of this sample above the fog base.

    float heightAboveBase = (positionWS.y - heightOffset) - heightDistance;

    // Exponential falloff: full density at the base, thinning with altitude.
    // Saturate caps it at the base value below the line.

    float heightDensity = saturate(exp(-(heightAboveBase * heightFalloff)));

    return heightDensity;
}

// Volumetric fog.
// WS = world space.

void VolumetricFog_float(

    // ...

    float3 positionWS, float3 normalWS, float2 uvSS,
    float4 colour, int steps, float density, float maxDistance,

    // ...

    float anisotropy, float anisotropyBlend,

    // ...

    bool enableBlurTexture,

    Texture2D blurTexture,

    float blurTextureBlend,

    float blurTextureRemapMin,
    float blurTextureRemapMax,

    // ...

    bool enableHeightMask,

    float heightMaskBlend,

    float heightMaskDistance,
    float heightMaskOffset,

    float heightMaskFalloff,

    // ...

    bool enableHeightMaskTexture,

    Texture2D heightMaskTexture,

    float heightMaskTextureAmplitude,
    float heightMaskTextureScale,

    float2 heightMaskTextureAnimation,

    float heightMaskTexturePower,

    // ...

    float ambientLightScale,

    // ...    

    bool enableAdaptiveProbeVolumes,

    float adaptiveProbeVolumeScale,
    float adaptiveProbeVolumePower,

    // ...

    bool enableMainLightSelfShadow,
    
    float mainLightSelfShadowSteps,
    float mainLightSelfShadowDistance,

    // Final fog composite/mix with the scene.
    // If I want the final fog, use this.

    out float4 composite, 
    
    // Output lighting and transmittance separately.
    // If I want to composite them separate, use these.

    out float3 lighting, out float transmittance)
{    
    // -- SETUP.
    
    float3 cameraPositionWS = GetCameraPositionWS();
    float3 offsetToSurfaceWS = positionWS - cameraPositionWS;
    
    //float aspectRatio = _ScreenParams.x / _ScreenParams.y;
        
    // Distance between camera and surface (vertex or fragment).
    
    float distanceToSurfaceWS = length(offsetToSurfaceWS);
    
    // Direction from camera to surface = -view direction.
    
    float3 directionToSurfaceWS = offsetToSurfaceWS / distanceToSurfaceWS;
    
    // Screen pixel coordinates.
    
    float2 uvPP = uvSS * _ScreenParams.xy;
    
    // Noise.
     
    //float interleavedGradientNoise = InterleavedGradientNoise(pixelCoordinates, _FrameCount);
    float interleavedGradientNoise = InterleavedGradientNoise(uvPP, (_Time.y * 60) % 60);
    
    // Distance.
    
    // Limiting this allows for higher resolution.
    // Same number of steps, covering a smaller distance.
    
    float raymarchDistanceWS = distanceToSurfaceWS;    
    raymarchDistanceWS = min(raymarchDistanceWS, maxDistance);
    
    // Lighting.
    
    lighting = 0.0;
    
    InputData inputData = (InputData) 0;
    
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = -directionToSurfaceWS;
    
    inputData.normalizedScreenSpaceUV = uvSS;
    
    // Get main light *now* - it will not change during raymarch.
    
    Light mainLight = GetMainLight();
    
    // -- RAYMARCH.
    
    float rayStepWS = raymarchDistanceWS / steps;
    float rayStepNoiseWS = rayStepWS * interleavedGradientNoise;
    
    float densityPerStepWS = density * rayStepWS;
        
    // Transmittance: How much light can pass through to the camera.
    // Starts at 1.0 (fully clear) and decays towards 0.0 (fully blocked).
    
    transmittance = 1.0;
    
    // Loop.
        
    [loop]
    for (int i = 0; i < steps; ++i)
    {
        // Calculate distance along ray for this step/iteration.
        
        float stepRayDistanceWS = i * rayStepWS;
                
        // Add up to one full step/iteration (noise is [0.0, 1.0] of IGNoise-based offset.
        
        stepRayDistanceWS += rayStepNoiseWS;
        
        // Depth test.
                
        if (stepRayDistanceWS > raymarchDistanceWS)
        {
            break;
        }
        
        // Calculate current position along ray in world space.
        
        float3 stepPositionWS = cameraPositionWS + (directionToSurfaceWS * stepRayDistanceWS);
    
        // -- LIGHTING.
        // Setup lighting data for this position.
                    
        inputData.positionWS = stepPositionWS;
        
        // Sample density.
        // Can vary based on noise/height/etc.
        
        float stepDensity = GetVolumetricFogDensity(stepPositionWS);
        float heightMaskOffsetComposite = heightMaskOffset;
        
        if (enableHeightMask)
        {
            SampleData heightMaskTextureSampleData;
            
            if (enableHeightMaskTexture)
            {            
                heightMaskTextureSampleData.scale = heightMaskTextureScale;
                
                heightMaskTextureSampleData.tiling = _Height_Mask_Texture_ST.xy;
                heightMaskTextureSampleData.offset = _Height_Mask_Texture_ST.zw;
            
                heightMaskTextureSampleData.animation = heightMaskTextureAnimation;
            
                float heightMaskTextureSample = heightMaskTextureSampleData.SampleTexture(heightMaskTexture, stepPositionWS.xz).r;
                
                heightMaskTextureSample = pow(heightMaskTextureSample, heightMaskTexturePower);
                
                // Remap from [0.0, 1.0] to [-1.0, 1.0], and scale.
                
                heightMaskTextureSample = (heightMaskTextureSample * 2.0) - 1.0;                
                heightMaskTextureSample *= heightMaskTextureAmplitude;
                
                heightMaskOffsetComposite += heightMaskTextureSample;
            }
            
            float stepDensity_heightMask = GetVolumetricFogDensity(stepPositionWS, heightMaskDistance, heightMaskOffsetComposite, heightMaskFalloff);
            
            stepDensity = lerp(stepDensity, stepDensity_heightMask, heightMaskBlend);
        }
        
        stepDensity *= densityPerStepWS;
                
        // Calculate transmittance for this specific step (Beer-Lambert Law).
        
        float stepTransmittance = exp(-stepDensity);
        
        // Calculate scattering probability.        
        // > Amount of light captured and scattered, this step.
        
        // Physically cannot exceed 1.0, unlike raw density.
        
        float stepScattering = 1.0 - stepTransmittance;
        
        // Combined lighting for this step...
        
        float3 stepLighting = 0.0;
        
#ifdef _ENABLE_MAIN_LIGHT
        // 1. Main light.
        
        #ifdef _ENABLE_MAIN_LIGHT_SHADOWS
            inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);        
            mainLight.shadowAttenuation = MainLightRealtimeShadow(inputData.shadowCoord);
        #endif
        
        float3 mainLighting = mainLight.color * mainLight.shadowAttenuation;
        
        // Main lighting anisotropy.
        
        float mainLight_VdotL = dot(directionToSurfaceWS, mainLight.direction);
        float mainLight_phase = HenyeyGreensteinPhase(mainLight_VdotL, anisotropy);
        
        mainLight_phase = lerp(1.0, mainLight_phase, anisotropyBlend);
        
        // Add main light to this step's lighting,
        // modulated by anisotropic phase function.
        
        stepLighting += mainLighting * mainLight_phase;
#endif
        
#ifdef _ENABLE_ADDITIONAL_LIGHTS        
        // 2. Additional lights.
        
        //int lightCount = GetAdditionalLightsCount(); // Doesn't seem to work for fullscreen effects.
                
        // _AdditionalLightsCount already exists as a global URP uniform (hence the redefinition error).
        // It is bound automatically every frame, and holds the post-cull additional light count in .x.
        
        LIGHT_LOOP_BEGIN(_AdditionalLightCount)
        {
            Light additionalLight = GetAdditionalPerObjectLight(lightIndex, inputData.positionWS); // This one works for post-processing.
            //Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);

            #ifdef _ENABLE_ADDITIONAL_LIGHT_SHADOWS
                        additionalLight.shadowAttenuation = AdditionalLightRealtimeShadow(

                        lightIndex,

                        inputData.positionWS,
                        additionalLight.direction,

                        GetAdditionalLightShadowParams(lightIndex),
                        GetAdditionalLightShadowSamplingData(lightIndex));
            #endif
            
            float3 additionalLighting = additionalLight.color * (additionalLight.shadowAttenuation * additionalLight.distanceAttenuation);
                                              
            // Additional lighting anisotropy.
            
            float additionalLight_VdotL = dot(directionToSurfaceWS, additionalLight.direction);
            float additionalLight_phase = HenyeyGreensteinPhase(additionalLight_VdotL, anisotropy);
            
            additionalLight_phase = lerp(1.0, additionalLight_phase, anisotropyBlend);
            
            // Add additional light to this step's lighting,
            // modulated by anisotropic phase function.
            
            stepLighting += additionalLighting * additionalLight_phase;
        }
        LIGHT_LOOP_END
#endif
        
#ifdef _ENABLE_AMBIENT_LIGHT
        // 3. Ambient light.
        
        stepLighting += _AmbientLighting.rgb * ambientLightScale;        
        
#endif
        
        // 4. Adaptive probe volumes.
                
        if (enableAdaptiveProbeVolumes)
        {
            float3 apvSample = SampleAPV(stepPositionWS, -directionToSurfaceWS, uvSS);
            
            apvSample = pow(apvSample, adaptiveProbeVolumePower);
            stepLighting += apvSample * adaptiveProbeVolumeScale;
        }
                        
        // -- INTEGRATION.
        
        // Apply scattering.
        
        stepLighting *= stepScattering;
        
        // Accumulate light into the final buffer.
        // Crucial: multiply by 'transmittance'. 
        
        // Light deeper in the fog obscured by fog  already stepped through.
        
        lighting += stepLighting * transmittance;
        
        // -- ABSORPTION.
        
        // Update transmittance for the NEXT step/iteration/cycle.
        // Beer-Lambert law: exp(-density).
        
        transmittance *= stepTransmittance;
        
        // Early exit if transmittance is nearly zero (opaque).
        
        if (transmittance < 0.001)
        {
            transmittance = 0.0f; break;
        }
    }
    
    // Tint accumulated light.
    
    lighting *= colour.rgb;
    
    // -- COMPOSITE (FINAL COLOUR).
    
    // Blend to blur based on scattering, attenuated by fog alpha.
    
    float4 sceneColour = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvSS);
    
    if (enableBlurTexture)
    {    
        // Blur.
    
        // [0.0, 1.0] depth = distance to surface (from camera, in units [meters]) / cameraFarPlane;
        
        //float linearDepth = distanceToSurfaceWS / _ProjectionParams.z;
        //float scattering = smoothstep(remapMin, remapMax, linearDepth);

        // UPDATE: Blur by fog 'density' (inverse transmittance).

        float blur = smoothstep(blurTextureRemapMin, blurTextureRemapMax, 1.0 - transmittance);
    
        blur *= blurTextureBlend;
        
        float4 sceneColour_blur = SAMPLE_TEXTURE2D(blurTexture, sampler_LinearClamp, uvSS);
        sceneColour = lerp(sceneColour, sceneColour_blur, blur * colour.a);
    }
    
    // Scene colour multiplied by remaining transmittance (whatever wasn't blocked).
    // Fog light is then added on top.
    
    composite.rgb = (sceneColour.rgb * transmittance) + lighting;
    composite.rgb = lerp(sceneColour.rgb, composite.rgb, colour.a);

    composite.a = sceneColour.a;
}