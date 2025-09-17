Shader "Xochicalco/Portal"
{
    Properties
    {
        _MainTex ("Portal Texture", 2D) = "white" {}
        _PortalColor ("Portal Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _Brightness ("Brightness", Range(0, 3)) = 1.0
        _Distortion ("Distortion", Range(0, 0.2)) = 0.02
        _EdgeGlow ("Edge Glow", Range(0, 2)) = 0.3
        _NoiseScale ("Noise Scale", Range(0, 20)) = 1.0
        _NoiseSpeed ("Noise Speed", Range(0, 10)) = 1.0
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        _PulseFrequency ("Pulse Frequency", Range(0, 5)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Portal"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float fog : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _PortalColor;
                float _Brightness;
                float _Distortion;
                float _EdgeGlow;
                float _NoiseScale;
                float _NoiseSpeed;
                float _FresnelPower;
                float _PulseFrequency;
            CBUFFER_END

            // Optimized hash function for better noise
            float hash21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }

            // Improved smooth noise with better interpolation
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); // Improved smoothstep
                
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal noise for more detail
            float fractalNoise(float2 uv, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * smoothNoise(uv);
                    uv *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = normalInput.normalWS;
                output.viewDir = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                output.fog = ComputeFogFactor(output.positionHCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.uv;
                float time = _Time.y * _NoiseSpeed;
                
                // Multi-octave noise for better distortion
                float2 noiseUV = uv * _NoiseScale;
                float noise1 = fractalNoise(noiseUV + time * 0.1, 3);
                float noise2 = fractalNoise(noiseUV * 2.17 + time * 0.15, 2);
                
                // Apply distortion with multiple noise layers
                float2 distortedUV = uv + (noise1 - 0.5) * _Distortion + (noise2 - 0.5) * _Distortion * 0.3;
                
                // Sample portal texture
                half4 portalTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                
                // Improved edge calculation
                float2 centeredUV = abs(uv - 0.5) * 2.0;
                float edgeDistance = length(centeredUV);
                float edgeFactor = 1.0 - smoothstep(0.7, 1.0, edgeDistance);
                
                // Fresnel effect
                float3 worldNormal = normalize(input.worldNormal);
                float3 viewDir = normalize(input.viewDir);
                float fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), _FresnelPower);
                
                // Pulsing effect
                float pulse = sin(time * _PulseFrequency) * 0.5 + 0.5;
                
                // Enhanced glow with fresnel and pulse
                float glowFactor = (pow(1.0 - edgeDistance, 2.0) * _EdgeGlow + fresnel * 0.5) * (1.0 + pulse * 0.3);
                
                // Combine colors with improved blending
                half4 finalColor = portalTex * _PortalColor * _Brightness;
                finalColor.rgb += glowFactor * _PortalColor.rgb * 2.0;
                finalColor.a *= edgeFactor * (0.8 + fresnel * 0.5);
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fog);
                
                return saturate(finalColor);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}