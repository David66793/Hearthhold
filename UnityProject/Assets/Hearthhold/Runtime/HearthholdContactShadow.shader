Shader "Hearthhold/ContactShadow"
{
    Properties { _BaseColor("Shadow tint", Color) = (0.07,0.12,0.08,0.27) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "ContactShadow"
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half2 centered = (input.uv - 0.5h) * 2.0h;
                half radius = dot(centered, centered);
                half opacity = saturate((1.0h - radius) * 1.6h);
                opacity *= opacity;
                return half4(_BaseColor.rgb, _BaseColor.a * opacity);
            }
            ENDHLSL
        }
    }
}
