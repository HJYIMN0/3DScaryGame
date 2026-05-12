// =============================================================================
//  AAA_VolumetricFog.shader
//  Nebbia volumetrica AAA per Unity 6 URP
//
//  USO: Crea un materiale con questo shader, applicalo a un cubo Unity standard.
//  Scala e posiziona il cubo per definire il volume della nebbia nel mondo.
//
//  TECNICHE:
//  - Ray-AABB intersection in object space  â†’ volume preciso, rispetta scala/rotazione
//  - FBM a due layer (base + detail erosion) â†’ forme organiche stile cloud
//  - Henyey-Greenstein phase function        â†’ scattering realistico verso/contro luce
//  - Beer-Lambert transmittance integration  â†’ fisica della luce energy-conserving
//  - Screen-space hash jitter                â†’ zero banding, zero artefatti
//  - Height gradient in object space         â†’ si controlla solo scalando il cubo
//  - Box edge softening                      â†’ nessun bordo netto del volume
//  - Multi-scatter approximation             â†’ nebbia densa sembra illuminata in modo uniforme
// =============================================================================

Shader "Custom/AAA_VolumetricFog"
{
    Properties
    {
        [Header(Color)]
        _FogColor      ("Fog Color",     Color)  = (0.80, 0.82, 0.87, 1)
        _AmbientColor  ("Ambient Color", Color)  = (0.45, 0.55, 0.70, 1)

        [Header(Density and Scattering)]
        _Density        ("Density",                    Range(0, 10))     = 2.0
        _ScatterAlbedo  ("Scatter Albedo (0=absorb, 1=scatter all)", Range(0, 1)) = 0.85
        _Anisotropy     ("Anisotropy HG (neg=back, pos=forward)",   Range(-0.99, 0.99)) = 0.25
        _MultiScatter   ("Multi-Scatter Blend",        Range(0, 1))      = 0.35

        [Header(Height Distribution)]
        _HeightFalloff  ("Height Falloff (potenza)",   Range(0.1, 10))   = 2.5
        _HeightBias     ("Height Bias (0=basso denso, 1=alto denso)", Range(0, 1)) = 0.05
        _EdgeSoftness   ("Edge Softness",              Range(0.01, 0.49)) = 0.08

        [Header(Noise Shape - FBM)]
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

            // Cull Front: renderizza solo le back-face del cubo.
            // ZTest Always: funziona sia da fuori che da dentro il volume.
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
            //  UTILITY FUNCTIONS
            // -----------------------------------------------------------------

            // Hash PCG per jitter per-pixel â€“ elimina il banding senza texture
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

            // Henyey-Greenstein phase function.
            // g > 0 â†’ forward scattering (nebbia illuminata verso la luce)
            // g < 0 â†’ backward scattering
            float PhaseHG(float cosTheta, float g)
            {
                float g2    = g * g;
                float denom = pow(max(1.0 + g2 - 2.0 * g * cosTheta, 0.0001), 1.5);
                return (0.25 * INV_PI) * (1.0 - g2) / denom;
            }

            // Dual-lobe phase: blend HG con scattering isotropico.
            // MultiScatter approssima i rimbalzi multipli di luce in nebbia densa.
            float PhaseDualLobe(float cosTheta, float g, float multiScatter)
            {
                return lerp(PhaseHG(cosTheta, g), 0.25 * INV_PI, multiScatter);
            }

            // FBM density: layer base (low-freq shape) eroso da detail (high-freq).
            // Tecnica derivata dal cloud rendering di produzione (Guerrilla, Nubis, etc.)
            float SampleDensity(float3 posWS)
            {
                // Due layer di vento con direzioni leggermente diverse per sembrare turbolento
                float3 windBase   = normalize(_WindDir.xyz + float3(1e-5, 0, 0)) * (_Time.y * _WindSpeedBase);
                float3 windDetail = normalize(_WindDir.zxy + float3(0, 0.3, 0))  * (_Time.y * _WindSpeedDetail);

                float nBase   = SAMPLE_TEXTURE3D_LOD(_NoiseTex3D, sampler_NoiseTex3D,
                                    posWS * _NoiseScaleBase   + windBase,   0).r;

                float nDetail = SAMPLE_TEXTURE3D_LOD(_NoiseTex3D, sampler_NoiseTex3D,
                                    posWS * _NoiseScaleDetail + windDetail, 0).r;

                // Il detail "erode" la forma base: crea bordi filamentosi e irregolari
                float combined = nBase - nDetail * _DetailBlend;

                // Contrasto e offset per controllare quanto Ã¨ piena/vuota la nebbia
                return saturate(combined * _NoiseContrast + _NoiseOffset);
            }

            // Gradiente altezza in object-space (posOS.y in [-0.5, 0.5]).
            // DensitÃ  massima al fondo del cubo, zero all'apice.
            // HeightBias = 0 â†’ denso in basso; HeightBias = 1 â†’ denso in alto.
            float HeightGradient(float localY)
            {
                float t = localY + 0.5;              // remap a [0,1]: 0=fondo, 1=cima
                t = lerp(t, 1.0 - t, _HeightBias);   // flip opzionale
                return pow(saturate(1.0 - t), _HeightFalloff);
            }

            // Dissolvenza ai bordi del volume per eliminare i bordi netti del cubo.
            // margin: distanza in OS units entro cui fare fade (relativa a half-extent 0.5)
            float BoxEdgeFade(float3 posOS, float margin)
            {
                float3 d    = abs(posOS) - (0.5 - margin);
                float  edge = max(d.x, max(d.y, d.z));
                return saturate(1.0 - edge / margin);
            }

            // Ray-AABB intersection in object space.
            //
            // IMPORTANTE: rdOS deve essere NON-normalizzato (= mul(UNITY_MATRIX_I_M, dir_WS_norm)).
            // In questo modo i valori t corrispondono a distanze nel world-space,
            // permettendo di confrontare tFar con la distanza della geometria opaca.
            bool RayBoxIntersect(float3 roOS, float3 rdOS, out float tNear, out float tFar)
            {
                float3 invRD = 1.0 / rdOS;   // IEEE 754: inf corretto per componenti zero
                float3 t0    = (-0.5 - roOS) * invRD;
                float3 t1    = ( 0.5 - roOS) * invRD;
                float3 tmin  = min(t0, t1);
                float3 tmax  = max(t0, t1);
                tNear = max(tmin.x, max(tmin.y, tmin.z));
                tFar  = min(tmax.x, min(tmax.y, tmax.z));
                return tFar > max(tNear, 0.0);
            }

            // -----------------------------------------------------------------
            //  VERTEX SHADER
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
                // --- Depth scene opaca ---
                float2 screenUV    = i.screenPos.xy / i.screenPos.w;
                float  rawDepth    = SampleSceneDepth(screenUV);
                float3 sceneWS     = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float  sceneDistWS = length(sceneWS - _WorldSpaceCameraPos);

                // --- Setup raggio (world space, normalizzato) ---
                float3 rayOriginWS    = _WorldSpaceCameraPos;
                float3 rayDirWS       = normalize(i.positionWS - rayOriginWS);

                // Raggio in object space.
                // rayDirOSUnnorm NON normalizzato: i valori t del AABB restano in unitÃ  WS.
                float3 rayOriginOS    = TransformWorldToObject(rayOriginWS);
                float3 rayDirOSUnnorm = mul((float3x3)UNITY_MATRIX_I_M, rayDirWS);

                // --- Intersezione AABB con il volume del cubo ---
                float tNear, tFar;
                if (!RayBoxIntersect(rayOriginOS, rayDirOSUnnorm, tNear, tFar)) discard;

                tNear = max(tNear, 0.0001);
                tFar  = min(tFar, sceneDistWS);   // il volume non attraversa geometria opaca
                if (tNear >= tFar) discard;

                // --- Illuminazione principale ---
                Light  mainLight  = GetMainLight();
                float3 lightDir   = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;

                // Phase function: angolo tra raggio della camera e direzione luce
                float cosTheta = dot(rayDirWS, lightDir);
                float phase    = PhaseDualLobe(cosTheta, _Anisotropy, _MultiScatter);

                // Luminanza costante lungo il raggio (luce diretta + ambiente)
                float3 directLum  = lightColor * phase * _FogColor.rgb;
                float3 ambientLum = _AmbientColor.rgb * _FogColor.rgb;
                float3 luminance  = directLum + ambientLum;

                // --- Jitter: offset random del primo sample per eliminare banding ---
                int   steps    = clamp(_StepCount, 8, 256);
                float stepSize = (tFar - tNear) / float(steps);
                float jitter   = ScreenJitter(screenUV * _ScreenParams.xy);
                float t        = tNear + jitter * stepSize * _JitterStrength;

                // Precomputa incremento OS per evitare la moltiplicazione matriciale nel loop
                float3 stepOS = rayDirOSUnnorm * stepSize;
                float3 posOS  = rayOriginOS + rayDirOSUnnorm * t;

                // --- Ray march ---
                float3 accColor      = 0.0;
                float  transmittance = 1.0;

                [loop]
                for (int s = 0; s < 256; s++)
                {
                    if (s >= steps || t >= tFar || transmittance < 0.005) break;

                    float3 posWS = rayOriginWS + rayDirWS * t;

                    // DensitÃ  locale: noise Ã— gradiente altezza Ã— fade bordi Ã— scala globale
                    float noiseDensity = SampleDensity(posWS);
                    float heightGrad   = HeightGradient(posOS.y);
                    float edgeFade     = BoxEdgeFade(posOS, _EdgeSoftness);
                    float density      = noiseDensity * heightGrad * edgeFade * _Density;

                    if (density > 0.0001)
                    {
                        // Beer-Lambert: profonditÃ  ottica dello step
                        float tau       = density * stepSize;
                        float stepTrans = exp(-tau);

                        // Integrazione energy-conserving: Î”L = albedo Ã— L_in Ã— (1 âˆ’ T_step)
                        // Derivato da: âˆ«â‚€^Î”t Ïƒ_sÂ·LÂ·exp(âˆ’Ïƒ_tÂ·t)dt = albedoÂ·LÂ·(1âˆ’exp(âˆ’Ïƒ_tÂ·Î”t))
                        float3 stepColor = _ScatterAlbedo * luminance * (1.0 - stepTrans);
                        accColor += transmittance * stepColor;
                        transmittance *= stepTrans;
                    }

                    t     += stepSize;
                    posOS += stepOS;   // incremento OS precomputato (no matrix multiply in loop)
                }

                // Alpha finale = opacitÃ  accumulata (complementare alla trasmittanza residua)
                float alpha = saturate(1.0 - transmittance);
                return float4(accColor, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}