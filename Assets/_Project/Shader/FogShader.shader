Shader "Custom/AAA_HeightSlabFog"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.75, 0.78, 0.82, 1)

        _Density("Density", Range(0,10)) = 2

        _HeightStart("Height Start", Float) = 0

        _HeightEnd("Height End", Float) = 30

        _HeightFalloff("Height Falloff", Range(0.1,10)) = 2

        _NoiseTex3D("Noise 3D", 3D) = "" {}

        _NoiseScale("Noise Scale", Float) = 0.02

        _WindDir("Wind Dir", Vector) = (1,0,0,0)

        _WindSpeed("Wind Speed", Float) = 0.2

        _StepCount("Steps", Range(8,128)) = 64

        _MaxDistance("Max Distance", Float) = 500
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
        }

        Pass
        {
            Name "HeightSlabFog"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            float4 _FogColor;
            float _Density;
            float _HeightStart;
            float _HeightEnd;
            float _HeightFalloff;
            float _NoiseScale;
            float4 _WindDir;
            float _WindSpeed;
            float _MaxDistance;
            int _StepCount;

            TEXTURE3D(_NoiseTex3D);
            SAMPLER(sampler_NoiseTex3D);

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            float Noise(float3 p)
            {
                float3 wind = _WindDir.xyz * (_Time.y * _WindSpeed);

                float3 uvw = (p + wind) * _NoiseScale;

                return SAMPLE_TEXTURE3D_LOD(
                    _NoiseTex3D,
                    sampler_NoiseTex3D,
                    uvw,
                    0
                ).r;
            }

            float HeightMask(float y)
            {
                // slab verticale controllata
                float inRange = step(_HeightStart, y) * step(y, _HeightEnd);

                float mid = (_HeightStart + _HeightEnd) * 0.5;

                float falloff =
                    exp(-abs(y - mid) * _HeightFalloff);

                return inRange * falloff;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;

                float rawDepth = SampleSceneDepth(uv);

                float3 scenePos = ComputeWorldSpacePosition(
                    uv,
                    rawDepth,
                    UNITY_MATRIX_I_VP
                );

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(scenePos - rayOrigin);

                float maxDist = min(distance(rayOrigin, scenePos), _MaxDistance);

                float stepSize = maxDist / _StepCount;

                float t = 0;

                float3 col = 0;
                float alpha = 0;

                [loop]
                for (int i = 0; i < 128; i++)
                {
                    if (i >= _StepCount) break;
                    if (t >= maxDist || alpha > 0.95) break;

                    float3 p = rayOrigin + rayDir * t;

                    // SOLO SLAB HEIGHT (chiave AAA)
                    float hMask = HeightMask(p.y);

                    if (hMask > 0.001)
                    {
                        float n = Noise(p);

                        float density = n * hMask * _Density;

                        float extinction = density * stepSize;

                        float trans = exp(-extinction);

                        float3 fog = _FogColor.rgb * density;

                        col += fog * (1 - trans) * (1 - alpha);

                        alpha += (1 - trans);
                    }

                    t += stepSize;
                }

                return float4(col, saturate(alpha));
            }

            ENDHLSL
        }
    }
}