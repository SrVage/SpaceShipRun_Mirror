Shader "Unlit/PlanetNoiseShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Height ("Height", Range(-1,1)) = 0
        _Seed ("Seed", Range(0,1000)) = 0
        _WaterColor ("Water Color", Color) = (1,1,1,1)
        _DeepWaterColor ("Deep Water Color", Color) = (1,1,1,1)
        _GroundColor ("Ground Color", Color) = (1,1,1,1)
        _MountainColor ("Mountain Color", Color) = (1,1,1,1)
        //Circle parameters
        _WideOfCircle("Wide of Circle", Range(0,5)) = 1
        _InsideWideOfCircle("Inside Wide of Circle", Range(0,5)) = 1
        _CircleColor ("Circle Color", Color) = (1,1,1,1)
        //Atmosphere
        _AtmosphereColor ("Atmosphere Color", Color) = (1,1,1,1)
        _Atmosphere("Atmosphere", Range(0,1)) = 0

    }
    SubShader
    {
        Tags {"RenderType"="Transparent"}
        LOD 100

        Pass //отрисовка планеты
        {
            CGPROGRAM
            #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 normal : NORMAL;
                float height: FLOAT;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Seed;
            fixed _Height;
            fixed4 _WaterColor;
            fixed4 _DeepWaterColor;
            fixed4 _GroundColor;
            fixed4 _MountainColor;

            float4 LightingBasicDiffuse (v2f input, fixed4 color)
            {
                float difLight = max(0, dot (input.normal, _WorldSpaceLightPos0));
                float4 col;
                col.rgb = color.rgb*_LightColor0.xyz * difLight;
                col.a = color.a;
                return col;
            }

            float hash(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float2 p, float size)
            {
                float result = 0;
                p *= size;
                float2 i = floor(p + _Seed);
                float2 f = frac(p + _Seed / 739);
                float2 e = float2(0, 1);
                float z0 = hash((i + e.xx) % size);
                float z1 = hash((i + e.yx) % size);
                float z2 = hash((i + e.xy) % size);
                float z3 = hash((i + e.yy) % size);
                float2 u = smoothstep(0, 1, f);
                result = lerp(z0, z1, u.x) + (z2 - z0) * u.y * (1.0 - u.x) + (z3 - z1) * u.x * u.y;
                return result;
            }

            
            v2f vert (appdata v)
            {
                v2f o;
                fixed height = noise(v.uv, 5) * 0.75 + noise(v.uv, 30) * 0.125 + noise(v.uv, 50) * 0.125;
                fixed param = (height-0.5)*_Height;
                o.height = param;
                o.vertex = UnityObjectToClipPos(v.vertex+param*v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col;
                if (i.height<0)
                    col = lerp(_WaterColor, _DeepWaterColor, 5*abs(i.height));
                else if (i.height<0.15)
                    col=_GroundColor;
                else
                    col = _MountainColor;
                col = LightingBasicDiffuse(i, col);
                return col;
            }
            ENDCG
        }
        
        
                Pass
        {
            Cull Front
            Tags {"RenderType"="Transparent" "Queue" = "Geometry" }
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma exclude_renderers d3d11
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
                float light:FLOAT1;
                float distance:FLOAT2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Atmosphere;
            fixed4 _AtmosphereColor;
            fixed _Seed;

            float hash(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float2 p, float size)
            {
                float result = 0;
                p *= size;
                float2 i = floor(p + _Seed);
                float2 f = frac(p + _Seed / 739);
                float2 e = float2(0, 1);
                float z0 = hash((i + e.xx) % size);
                float z1 = hash((i + e.yx) % size);
                float z2 = hash((i + e.xy) % size);
                float z3 = hash((i + e.yy) % size);
                float2 u = smoothstep(0, 1, f);
                result = lerp(z0, z1, u.x) + (z2 - z0) * u.y * (1.0 - u.x) + (z3 - z1) * u.x * u.y;
                return result;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex*(1+_Atmosphere));
                o.scalar = dot(_WorldSpaceCameraPos - mul(unity_ObjectToWorld,v.vertex), UnityObjectToWorldNormal(v.normal));
                o.distance = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld,v.vertex));
                o.light = noise(v.uv+_CosTime.x, 5) * 0.75 + noise(v.uv+_CosTime.x, 30) *0.125 + noise(v.uv, 50) * 0.125;
                return o;
            }

            fixed4 frag (v2f o) : SV_Target
            {
                fixed4 col;
                col.rgb = _AtmosphereColor*o.scalar/10+o.light;
                col.a =1000*_AtmosphereColor.a*o.scalar/pow(o.distance,2);
                return col;
            }
            ENDCG
            }
        
        
        Pass //расчет внутренней окружности для стенсил-буффера
        {
            
            Stencil 
        {
            Ref 10
            Comp Always
            Pass Replace
        }
            Blend SrcAlpha OneMinusSrcAlpha
            Tags {"RenderType"="Transparent" "Queue" = "Geometry-1" }
            CGPROGRAM
            #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            
            fixed _InsideWideOfCircle;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(float3(v.vertex.x, 0, v.vertex.z)*_InsideWideOfCircle);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0,0,0,0);
                return col;
            }
            ENDCG
        }
       

        Pass //отрисовка кольца
        {
             Stencil 
        {
            Ref 10
            Comp NotEqual
        }
             Blend SrcAlpha OneMinusSrcAlpha
             Tags {"RenderType"="Transparent" "Queue" = "Geometry" }
            CGPROGRAM
#pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 normal : NORMAL;
                float coord : FLOAT;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _WideOfCircle;
            fixed4 _CircleColor;
            fixed _Seed;

            float4 LightingBasicDiffuse (v2f input, fixed4 color)
            {
                float difLight = max(0, dot (input.normal, _WorldSpaceLightPos0));
                float4 col;
                col.rgb = color.rgb*_LightColor0.xyz * (difLight+0.2);
                return col;
            }

            float hash(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float2 p, float size)
            {
                float result = 0;
                p *= size;
                float2 i = floor(p + _Seed);
                float2 f = frac(p + _Seed / 739);
                float2 e = float2(0, 1);
                float z0 = hash((i + e.xx) % size);
                float z1 = hash((i + e.yx) % size);
                float z2 = hash((i + e.xy) % size);
                float z3 = hash((i + e.yy) % size);
                float2 u = smoothstep(0, 1, f);
                result = lerp(z0, z1, u.x) + (z2 - z0) * u.y * (1.0 - u.x) + (z3 - z1) * u.x * u.y;
                return result;
            }

            
            v2f vert (appdata v)
            {
                v2f o;
                o.coord = noise(v.uv+_CosTime.x, 5) * 0.75 + noise(v.uv+_CosTime.x, 30) *0.125 + noise(v.uv, 50) * 0.125;
                o.vertex = UnityObjectToClipPos(float3(v.vertex.x, 0, v.vertex.z)*_WideOfCircle);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _CircleColor*i.coord;
                col = LightingBasicDiffuse(i, col);
                col.a = _CircleColor.a;
                return col;
            }
            ENDCG
        }
    }
}
