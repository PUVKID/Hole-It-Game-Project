// HoleIt stylized purple arena ground. Subtle checker cells keep gameplay readable behind detailed props.
Shader "HoleIt/GroundStencil"
{
    Properties
    {
        _ColorA ("Tile A", Color) = (0.43, 0.15, 0.72, 1)
        _ColorB ("Tile B", Color) = (0.38, 0.12, 0.66, 1)
        _Tiles ("Tile Count", Float) = 30
        _MainTex ("Unused", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        LOD 150
        Stencil { Ref 1 Comp NotEqual }
        CGPROGRAM
        #pragma surface surf Lambert noforwardadd exclude_path:prepass nolightmap
        #pragma target 2.0
        fixed4 _ColorA;
        fixed4 _ColorB;
        float _Tiles;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 cell = IN.uv_MainTex * _Tiles;
            float2 local = frac(cell);
            float checker = fmod(floor(cell.x) + floor(cell.y), 2.0);
            fixed3 baseCol = lerp(_ColorA.rgb, _ColorB.rgb, checker);
            float edge = min(min(local.x, 1.0 - local.x), min(local.y, 1.0 - local.y));
            float fineLine = 1.0 - smoothstep(0.018, 0.055, edge);
            // Gentle, barely visible tile seam instead of high-contrast blocks.
            o.Albedo = baseCol * (1.0 - fineLine * 0.075);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Mobile/Diffuse"
}
