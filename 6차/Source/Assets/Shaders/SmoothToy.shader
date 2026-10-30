Shader "PocketPit/SmoothToy" {
Properties { _Color("Tint",Color)=(1,1,1,1) _Glossiness("Smoothness",Range(0,1))=.28 }
SubShader { Tags {"RenderType"="Opaque"} LOD 200
CGPROGRAM
#pragma surface surf Standard fullforwardshadows addshadow vertex:vert
#pragma target 3.0
#pragma multi_compile_instancing
struct Input { float4 color:COLOR; float2 surface; };fixed4 _Color;half _Glossiness;
void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.color=v.color;o.surface=v.texcoord1.xy;}
void surf(Input IN,inout SurfaceOutputStandard o){o.Albedo=IN.color.rgb*_Color.rgb;o.Metallic=IN.surface.x;o.Smoothness=IN.surface.y>0?IN.surface.y:_Glossiness;o.Occlusion=1;o.Alpha=1;}
ENDCG
} Fallback "Diffuse"
}
