Shader "Hearthhold/SpellAura"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,0.5)
        _Softness ("Radial softness", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZTest LEqual
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Softness;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float radius = length(input.uv - 0.5) * 2;
                float feather = 1 - smoothstep(0.05, 1, radius);
                fixed4 result = _Color;
                result.a *= lerp(1, feather, _Softness);
                return result;
            }
            ENDCG
        }
    }
}
