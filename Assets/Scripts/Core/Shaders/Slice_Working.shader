Shader "Custom/PortalClipping" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        sliceNormal ("Slice Normal", Vector) = (0,0,0,0)
        sliceCentre ("Slice Centre", Vector) = (0,0,0,0)
        sliceOffsetDst ("Slice Offset Distance", Float) = 0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float3 sliceNormal;
            float3 sliceCentre;
            float sliceOffsetDst;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Calculate distance from slice plane
                float3 offsetFromSliceCentre = i.worldPos - sliceCentre;
                float dst = dot(offsetFromSliceCentre, sliceNormal);
                
                // Discard pixels on wrong side of slice plane
                clip(dst + sliceOffsetDst);
                
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}