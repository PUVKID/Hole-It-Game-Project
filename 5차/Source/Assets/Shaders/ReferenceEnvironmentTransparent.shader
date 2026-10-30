Shader "PocketPit/ReferenceEnvironmentTransparent" {
Properties { _MainTex("Color",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} LOD 120
Stencil { Ref 1 ReadMask 1 WriteMask 0 Comp NotEqual Pass Keep }
CGPROGRAM
#pragma surface surf Lambert alpha:fade noforwardadd
#pragma target 3.0
#pragma multi_compile_instancing
sampler2D _MainTex;fixed4 _Color;
struct Input {float2 uv_MainTex;};
void surf(Input IN,inout SurfaceOutput o){fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Emission=c.rgb*.12;o.Alpha=c.a;}
ENDCG
} FallBack Off
}
