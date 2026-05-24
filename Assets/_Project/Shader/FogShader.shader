// =============================================================================
//  AAA_VolumetricFog.shader
//  Nebbia volumetrica AAA per Unity 6 URP
// =============================================================================

Shader "Custom/FogShader"
{
    Properties
    {
        [Header(Color)]
        _FogColor      ("Fog Color",     Color)  = (0.80, 0.82, 0.87, 1)
        _AmbientColor  ("Ambient Color", Color)  = (0.45, 0.55, 0.70, 1)

        // [AGGIUNTO] Colore ed intensità emissiva della nebbia.
        // Simula luce autoprodotta (es. nebbia radioattiva, magica, volumetrica con neon).
        // EmissiveIntensity = 0 -> nessuna emissione (comportamento originale invariato).
        [Header(Emissive)]
        _EmissiveColor     ("Emissive Color",     Color)  = (1, 1, 1, 1)
        _EmissiveIntensity ("Emissive Intensity", Range(0, 10)) = 0.0

        [Header(Density and Scattering)]
        _Density        ("Density",                    Range(0, 10))     = 2.0
        _ScatterAlbedo  ("Scatter Albedo (0=absorb, 1=scatter all)", Range(0, 1)) = 0.85
        _Anisotropy     ("Anisotropy HG (neg=back, pos=forward)",   Range(-0.99, 0.99)) = 0.25
        _MultiScatter   ("Multi-Scatter Blend",        Range(0, 1))      = 0.35

        [Header(Height Distribution)]
        _HeightFalloff  ("Height Falloff (potenza)",   Range(0.1, 10))   = 2.5
        _HeightBias     ("Height Bias (0=basso denso, 1=alto denso)", Range(0, 1)) = 0.05
        _EdgeSoftness   ("Edge Softness",              Range(0.01, 0.49)) = 0.08

        [Header(Noise Shape FBM)]
        _NoiseTex3D       ("3D Noise Texture",         3D)               = "" {}
        _NoiseScaleBase   ("Base Noise Scale",         Float)            = 0.04
        _NoiseScaleDetail ("Detail Noise Scale",       Float)            = 0.15
        _NoiseContrast    ("Noise Contrast",           Range(0.5, 6))    = 2.0
        _NoiseOffset      ("Noise Offset",             Range(-1, 1))     = -0.25
        _DetailBlend      ("Detail Blend (erosione)",  Range(0, 1))      = 0.40

        [Header(Wind Animation)]
        _WindDir         ("Wind Direction (xyz)",      Vector)           = (1, 0, 0.3, 0)
        _WindSpeedBase   ("Wind Speed - Base Layer",   Float)            = 0.10
        _WindSpeedDetail ("Wind Speed - Detail Layer", Float)            = 0.22

        [Header(Opacity)]
        _OpacityScale    ("Opacity Scale",             Range(0, 1))      = 1.0

        [Header(Ray March Quality)]
        _StepCount       ("Step Count",                Range(32, 256))   = 96
        _JitterStrength  ("Jitter Strength",           Range(0, 1))      = 0.9
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
        }

        Pass
        {
            Name "VolumetricFog_AAA"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // -----------------------------------------------------------------
            //  CBUFFER
            // -----------------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _AmbientColor;
                // [AGGIUNTO] Variabili emissive nel CBUFFER
                float4 _EmissiveColor;
                float  _EmissiveIntensity;
                float  _Density;
                float  _ScatterAlbedo;
                float  _Anisotropy;
                float  _MultiScatter;
                float  _HeightFalloff;
                float  _HeightBias;
                float  _EdgeSoftness;
                float  _NoiseScaleBase;
                float  _NoiseScaleDetail;
                float  _NoiseContrast;
                float  _NoiseOffset;
                float  _DetailBlend;
                float4 _WindDir;
                float  _WindSpeedBase;
                float  _WindSpeedDetail;
                int    _StepCount;
                float  _JitterStrength;
                float  _OpacityScale;
            CBUFFER_END

            TEXTURE3D(_NoiseTex3D);
            SAMPLER(sampler_NoiseTex3D);

            // -----------------------------------------------------------------
            //  STRUCTS
            // -----------------------------------------------------------------
            struct Attributes { float4 positionOS : POSITION; };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            // -----------------------------------------------------------------
            //  UTILITY FUNCTIONS  (invariate)
            // -----------------------------------------------------------------

            float ScreenJitter(float2 pixelCoord)
            {
                uint2 ip = (uint2)pixelCoord;
                uint  n  = ip.x + ip.y * 1000u;
                n = (n ^ 61u) ^ (n >> 16u);
                n *= 9u;
                n ^= (n >> 4u);
                n *= 0x27d4eb2du;
                n ^= (n >> 15u);
                return float(n) * (1.0 / 4294967296.0);
            }

            float PhaseHG(float cosTheta, float g)
            {
                float g2    = g * g;
                float denom = pow(max(1.0 + g2 - 2.0 * g * cosTheta, 0.0001), 1.5);
                return (0.25 * INV_PI) * (1.0 - g2) / denom;
            }

            float PhaseDualLobe(float cosTheta, float g, float multiScatter)
            {
                return lerp(PhaseHG(cosTheta, g), 0.25 * INV_PI, multiScatter);
            }

            float SampleDensity(float3 posWS)
            {
                float3 windBase   = normalize(_WindDir.xyz + float3(1e-5, 0, 0)) * (_Time.y * _WindSpeedBase);
                float3 windDetail = normalize(_WindDir.zxy + float3(0, 0.3, 0))  * (_Time.y * _WindSpeedDetail);

                float nBase   = SAMPLE_TEXTURE3D_LOD(_NoiseTex3D, sampler_NoiseTex3D,
                                    posWS * _NoiseScaleBase   + windBase,   0).r;
                float nDetail = SAMPLE_TEXTURE3D_LOD(_NoiseTex3D, sampler_NoiseTex3D,
                                    posWS * _NoiseScaleDetail + windDetail, 0).r;

                float combined = nBase - nDetail * _DetailBlend;
                return saturate(combined * _NoiseContrast + _NoiseOffset);
            }

            float HeightGradient(float localY)
            {
                float t = localY + 0.5;
                t = lerp(t, 1.0 - t, _HeightBias);
                return pow(saturate(1.0 - t), _HeightFalloff);
            }

            float BoxEdgeFade(float3 posOS, float margin)
            {
                float3 d    = abs(posOS) - (0.5 - margin);
                float  edge = max(d.x, max(d.y, d.z));
                return saturate(1.0 - edge / margin);
            }

            bool RayBoxIntersect(float3 roOS, float3 rdOS, out float tNear, out float tFar)
            {
                float3 invRD = 1.0 / rdOS;
                float3 t0    = (-0.5 - roOS) * invRD;
                float3 t1    = ( 0.5 - roOS) * invRD;
                float3 tmin  = min(t0, t1);
                float3 tmax  = max(t0, t1);
                tNear = max(tmin.x, max(tmin.y, tmin.z));
                tFar  = min(tmax.x, min(tmax.y, tmax.z));
                return tFar > max(tNear, 0.0);
            }

            // -----------------------------------------------------------------
            //  VERTEX SHADER  (invariato)
            // -----------------------------------------------------------------
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                return o;
            }

            // -----------------------------------------------------------------
            //  FRAGMENT SHADER
            // -----------------------------------------------------------------
            float4 frag(Varyings i) : SV_Target
            {
                float2 screenUV    = i.screenPos.xy / i.screenPos.w;
                float  rawDepth    = SampleSceneDepth(screenUV);
                float3 sceneWS     = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float  sceneDistWS = length(sceneWS - _WorldSpaceCameraPos);

                float3 rayOriginWS    = _WorldSpaceCameraPos;
                float3 rayDirWS       = normalize(i.positionWS - rayOriginWS);
                float3 rayOriginOS    = TransformWorldToObject(rayOriginWS);
                float3 rayDirOSUnnorm = mul((float3x3)UNITY_MATRIX_I_M, rayDirWS);

                float tNear, tFar;
                if (!RayBoxIntersect(rayOriginOS, rayDirOSUnnorm, tNear, tFar)) discard;

                tNear = max(tNear, 0.0001);
                tFar  = min(tFar, sceneDistWS);
                if (tNear >= tFar) discard;

                Light  mainLight  = GetMainLight();
                float3 lightDir   = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;

                float cosTheta = dot(rayDirWS, lightDir);
                float phase    = PhaseDualLobe(cosTheta, _Anisotropy, _MultiScatter);

                float3 directLum  = lightColor * phase * _FogColor.rgb;
                float3 ambientLum = _AmbientColor.rgb * _FogColor.rgb;

                // [AGGIUNTO] Contributo emissivo: colore autoprodotto dalla nebbia,
                // indipendente dalla luce della scena. Si somma a directLum + ambientLum.
                // Con _EmissiveIntensity = 0 il termine è zero: comportamento originale invariato.
                float3 emissiveLum = _EmissiveColor.rgb * _EmissiveIntensity;

                float3 luminance  = directLum + ambientLum + emissiveLum; // [MODIFICATO] aggiunto emissiveLum

                int   steps    = clamp(_StepCount, 8, 256);
                float stepSize = (tFar - tNear) / float(steps);
                float jitter   = ScreenJitter(screenUV * _ScreenParams.xy);
                float t        = tNear + jitter * stepSize * _JitterStrength;

                float3 stepOS = rayDirOSUnnorm * stepSize;
                float3 posOS  = rayOriginOS + rayDirOSUnnorm * t;

                float3 accColor      = 0.0;
                float  transmittance = 1.0;

                [loop]
                for (int s = 0; s < 256; s++)
                {
                    if (s >= steps || t >= tFar || transmittance < 0.005) break;

                    float3 posWS = rayOriginWS + rayDirWS * t;

                    float noiseDensity = SampleDensity(posWS);
                    float heightGrad   = HeightGradient(posOS.y);
                    float edgeFade     = BoxEdgeFade(posOS, _EdgeSoftness);
                    float density      = noiseDensity * heightGrad * edgeFade * _Density;

                    if (density > 0.0001)
                    {
                        float tau       = density * stepSize;
                        float stepTrans = exp(-tau);
                        float3 stepColor = _ScatterAlbedo * luminance * (1.0 - stepTrans);
                        accColor += transmittance * stepColor;
                        transmittance *= stepTrans;
                    }

                    t     += stepSize;
                    posOS += stepOS;
                }

                float alpha = saturate(1.0 - transmittance) * _OpacityScale;
                return float4(accColor, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}