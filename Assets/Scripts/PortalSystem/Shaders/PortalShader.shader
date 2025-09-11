Shader "URP/Portal"
{
    Properties
    {
        _MainTex ("Portal Texture", 2D) = "white" {}
        _InactiveColour ("Inactive Colour", Color) = (0, 0.5, 1, 1)
        _DisplayMask ("Display Mask", Int) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 100
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "Portal"
            Tags {"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _InactiveColour;
                int _DisplayMask;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(output.positionHCS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                half4 portalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, screenUV);
                
                // Mix between portal view and inactive color based on display mask
                half4 finalColor = portalColor * _DisplayMask + _InactiveColour * (1 - _DisplayMask);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Lit"
}