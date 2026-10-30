// Hole 마스크: 색/깊이는 쓰지 않고 Stencil 값(1)만 기록한다. 원형 디스크는 UV 거리로 clip.
// 바닥(GroundStencil)이 Stencil != 1 인 곳에만 그려지므로 마스크 영역은 "바닥이 사라진" 것처럼 보인다.
Shader "HoleIt/HoleMask"
{
    Properties { }
    SubShader
    {
        Tags { "Queue" = "Geometry-10" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        ColorMask 0
        ZWrite Off
        ZTest Always
        Cull Off

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(0.5 - length(i.uv - 0.5));
                return 0;
            }
            ENDCG
        }
    }
}
