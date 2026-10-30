Shader "PocketPit/ReferenceSoft" {
Properties {
 _MainTex("Color",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)
 _ShadowColor("Shadow Tint",Color)=(.566,.566,.566,1)
 _EdgeStart("Shadow Start",Float)=-1 _EdgeEnd("Shadow End",Float)=2
 _Glossiness("Gloss Exponent",Float)=32
}
SubShader { Tags {"RenderType"="Opaque"} LOD 200
CGPROGRAM
#pragma surface surf Reference fullforwardshadows addshadow noambient noforwardadd
#pragma target 3.0
#pragma multi_compile_instancing
sampler2D _MainTex;fixed4 _Color,_ShadowColor;half _EdgeStart,_EdgeEnd,_Glossiness;
struct Input {float2 uv_MainTex;};
half4 LightingReference(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten){
 half shade=smoothstep(_EdgeStart,max(_EdgeStart+.01,_EdgeEnd),dot(s.Normal,lightDir));
 half3 diffuse=lerp(_ShadowColor.rgb,half3(1,1,1),shade);
 half3 h=normalize(lightDir+viewDir);
 half highlight=pow(saturate(dot(s.Normal,h)),max(4,_Glossiness))*0.10;
 return half4((s.Albedo*diffuse+highlight)*_LightColor0.rgb*lerp(.65h,1.h,atten),1);
}
void surf(Input IN,inout SurfaceOutput o){o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;o.Alpha=1;}
ENDCG
} FallBack "Diffuse"
}
