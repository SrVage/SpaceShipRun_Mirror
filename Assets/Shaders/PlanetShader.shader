Shader "Unlit/PlanetShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Atmosphere("Atmosphere", Range(0,1)) = 0
        _AtmosphereColor("Atmosphere Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        
                                Pass
        {
             Stencil 
        {
            Ref 5
            Comp NotEqual
        }
            //Cull Front
            Tags {"LightMode" = "SRPDefaultUnlit2" "RenderType"="Transparent" "Queue" = "Transparent-1" }
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float scalar:FLOAT0;
                float distance:FLOAT1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Atmosphere;
            fixed4 _AtmosphereColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex*(1+_Atmosphere));
                o.scalar = dot(_WorldSpaceCameraPos - mul(unity_ObjectToWorld,v.vertex), UnityObjectToWorldNormal(v.normal));
                o.distance = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld,v.vertex));
                return o;
            }

            fixed4 frag (v2f o) : SV_Target
            {
                fixed4 col;
                col.rgb = _AtmosphereColor*o.scalar/10;
                col.a =1000*_AtmosphereColor.a*o.scalar/pow(o.distance,2);
                return col;
            }
            ENDCG
            }
        
        Pass
        {
            Stencil 
        {
            Ref 5
            Comp Always
            Pass Replace
        }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
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
