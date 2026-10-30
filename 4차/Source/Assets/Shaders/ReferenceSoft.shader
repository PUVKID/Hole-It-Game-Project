Shader "PocketPit/ReferenceSoft" {
Properties { _MainTex("Color",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
SubShader { Tags {"RenderType"="Opaque"} LOD 120
CGPROGRAM
#pragma surface surf Lambert addshadow noforwardadd
#pragma target 3.0
#pragma multi_compile_instancing
sampler2D _MainTex;fixed4 _Color;
struct Input {float2 uv_MainTex;};
void surf(Input IN,inout SurfaceOutput o){fixed3 c=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;o.Albedo=c;o.Emission=c*.12;o.Alpha=1;}
ENDCG
} FallBack "Diffuse"
}
