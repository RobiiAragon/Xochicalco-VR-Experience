Shader "URP/PortalClipping"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Albedo (RGB)", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        
        [Header(Portal Clipping)]
        _SliceNormal("Slice Normal", Vector) = (0,0,0,0)
        _SliceCentre ("Slice Centre", Vector) = (0,0,0,0)
        _SliceOffsetDst("Slice Offset Distance", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                float3 _SliceNormal;
                float3 _SliceCentre;
                float _SliceOffsetDst;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                #if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #endif
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Portal clipping logic
                float3 adjustedCentre = _SliceCentre + _SliceNormal * _SliceOffsetDst;
                float3 offsetToSliceCentre = adjustedCentre - input.positionWS;
                clip(dot(offsetToSliceCentre, _SliceNormal));
                
                // Sample base texture
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Simple lighting calculation
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 normalWS = normalize(input.normalWS);
                
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * NdotL;
                
                // Add ambient
                lighting += SampleSH(normalWS);
                
                // Apply shadow attenuation
                lighting *= mainLight.shadowAttenuation;
                
                half3 color = albedo.rgb * lighting;
                
                return half4(color, albedo.a);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float3 _SliceNormal;
                float3 _SliceCentre;
                float _SliceOffsetDst;
            CBUFFER_END

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                // Apply clipping in shadow pass too
                float3 adjustedCentre = _SliceCentre + _SliceNormal * _SliceOffsetDst;
                float3 offsetToSliceCentre = adjustedCentre - input.positionWS;
                clip(dot(offsetToSliceCentre, _SliceNormal));
                
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}