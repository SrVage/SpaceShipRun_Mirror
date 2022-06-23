Shader "Unlit/Star"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            appdata vert (appdata v)
            {
                v.vertex = UnityObjectToClipPos(v.vertex);
                return v;
            }

            fixed4 frag (appdata i) : COLOR
            {
                return i.color;
            }
            ENDCG
        }
    }
}
