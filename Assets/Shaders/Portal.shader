Shader "Xochicalco/Portal"
{
    Properties
    {
        _MainTex ("Portal Texture", 2D) = "white" {}
        _PortalColor ("Portal Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Distortion ("Distortion", Range(0, 0.1)) = 0.02
        _EdgeGlow ("Edge Glow", Range(0, 1)) = 0.3
        _NoiseScale ("Noise Scale", Range(0, 10)) = 1.0
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float fog : TEXCOORD3;
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
            CBUFFER_END

            // Función de ruido simple
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = normalInput.normalWS;
                
                output.fog = ComputeFogFactor(output.positionHCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                
                // Agregar distorsión basada en ruido
                float time = _Time.y * _NoiseSpeed;
                float2 noiseUV = uv * _NoiseScale + time * 0.1;
                float noiseValue = smoothNoise(noiseUV);
                
                // Aplicar distorsión
                float2 distortedUV = uv + (noiseValue - 0.5) * _Distortion;
                
                // Samplear la textura del portal
                half4 portalTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                
                // Calcular el efecto de borde
                float2 centeredUV = abs(uv - 0.5) * 2.0;
                float edgeDistance = max(centeredUV.x, centeredUV.y);
                float edgeFactor = 1.0 - smoothstep(0.8, 1.0, edgeDistance);
                
                // Efecto de brillo en los bordes
                float glowFactor = pow(1.0 - edgeDistance, 2.0) * _EdgeGlow;
                
                // Combinar colores
                half4 finalColor = portalTex * _PortalColor * _Brightness;
                finalColor.rgb += glowFactor * _PortalColor.rgb;
                finalColor.a *= edgeFactor;
                
                // Aplicar fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fog);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Lit"
}