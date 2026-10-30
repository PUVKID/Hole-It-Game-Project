// Hole 내부 원통: 안쪽 면(Cull Front)을 어두운 그라데이션으로 그려 깊이감을 준다.
// 바닥이 마스크로 지워진 영역에서만 보이고, 그 외 영역은 바닥의 깊이에 가려진다.
Shader "HoleIt/HolePit"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.12, 0.10, 0.16, 1)
        _DeepColor ("Deep Color", Color) = (0, 0, 0, 1)
        _Depth ("Fade Depth", Float) = 4
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+1" "RenderType" = "Opaque" }
        Cull Front
        ZWrite On
        ZTest LEqual
        Stencil { Ref 1 ReadMask 1 WriteMask 0 Comp Equal Pass Keep }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _DeepColor;
            float _Depth;

            struct v2f { float4 pos : SV_POSITION; float wy : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wy = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float k = saturate(-i.wy / max(0.01, _Depth));
                return lerp(_TopColor, _DeepColor, k);
            }
            ENDCG
        }
    }
}
