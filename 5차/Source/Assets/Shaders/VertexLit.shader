Shader "HoleIt/VertexLit" {
Properties { _Color ("Tint", Color)=(1,1,1,1) }
SubShader { Tags { "RenderType"="Opaque" "Queue"="Geometry+2" } LOD 150
CGPROGRAM
#pragma surface surf Lambert addshadow fullforwardshadows vertex:vert
#pragma target 3.0
#pragma multi_compile_instancing
struct Input { fixed4 color : COLOR; };
fixed4 _Color;
void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.color=v.color; }
void surf(Input IN,inout SurfaceOutput o) { o.Albedo=IN.color.rgb*_Color.rgb; o.Alpha=1; }
ENDCG
} FallBack "Diffuse" }
