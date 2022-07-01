Shader "Unlit/Asteroid"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"LightMode" = "SRPDefaultUnlit" "RenderType"="Opaque"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5
            
            #include "UnityCG.cginc"
            #include "FractalGPU.hlsl"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            struct Input
            {
				float3 worldPos;
			};

            sampler2D _MainTex;
            float4 _MainTex_ST;
            

           v2f vert (Input i, appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(i.worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
