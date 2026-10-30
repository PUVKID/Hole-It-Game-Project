// Hole 가장자리 림: 쿼드 위에 절차적으로 링을 그린다 (텍스처 불필요, 투명 블렌딩 1 pass).
Shader "HoleIt/HoleRim"
{
    Properties
    {
        _Color ("Rim Color", Color) = (1, 1, 1, 0.9)
        _InnerRadius ("Inner Radius (0~0.5)", Range(0, 0.5)) = 0.44
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _InnerRadius;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = length(i.uv - 0.5);
                float ring = smoothstep(_InnerRadius, _InnerRadius + 0.02, d) * (1.0 - smoothstep(0.49, 0.5, d));
                fixed4 c = _Color;
                c.a *= ring;
                return c;
            }
            ENDCG
        }
    }
}
