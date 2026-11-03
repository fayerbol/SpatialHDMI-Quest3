Shader "SpatialHDMI/RoomLight" {
 Properties { _MainTex("Video",2D)="white" {} _Intensity("Intensity",Range(0,1))=.3 _Radius("Reach",Float)=2 _Saturation("Saturation",Range(0,2))=1 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass { CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex; float4x4 _WorldToScreen; float _Width,_Intensity,_Radius,_Saturation;
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
   struct v2f { float4 pos:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1; };
   v2f vert(appdata v) { v2f o; float4 w=mul(unity_ObjectToWorld,v.vertex); o.pos=UnityObjectToClipPos(v.vertex); o.local=mul(_WorldToScreen,w).xyz; o.normal=UnityObjectToWorldNormal(v.normal); return o; }
   fixed4 frag(v2f i):SV_Target {
     float2 uv=i.local.xy/float2(max(_Width,.01),max(_Width*.5625,.01))+.5;
     float2 outside=max(abs(uv-.5)-.5,0); float d=length(outside*float2(_Width,_Width*.5625));
     float distance3=length(float2(d,abs(i.local.z))); float fall=saturate(1-distance3/max(_Radius,.01)); fall=fall*fall*(3-2*fall);
     float2 edgeUV=clamp(uv,.02,.98); float3 c=tex2D(_MainTex,edgeUV).rgb;
     float lum=dot(c,float3(.2126,.7152,.0722)); c=lerp(lum.xxx,c,_Saturation);
     float3 normalLocal=mul((float3x3)_WorldToScreen,normalize(i.normal)); float facing=lerp(.45,1,abs(normalLocal.z));
     return float4(saturate(c), saturate(_Intensity*fall*facing));
   }
  ENDCG }
 }
}
