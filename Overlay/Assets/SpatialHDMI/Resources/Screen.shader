Shader "SpatialHDMI/Screen" {
 Properties { _MainTex("Decoded video",2D)="black" {} _VideoAspect("Video aspect",Float)=1.7777778 _FlipY("Flip vertically",Float)=0 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass {
   Name "SpatialVideo" Tags { "LightMode"="UniversalForward" }
   Cull Off ZWrite On ZTest LEqual
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST; float _VideoAspect; float _FlipY;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   Varyings vert(Attributes v) {
    Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o;
   }
   half4 frag(Varyings i):SV_Target {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 uv=i.uv-.5; float ratio=max(_VideoAspect,.01)/(16.0/9.0);
    if (ratio>1) uv.y*=ratio; else uv.x/=ratio;
    uv+=.5;
    if (any(uv<0)||any(uv>1)) return half4(0,0,0,1);
    uv.y=lerp(uv.y,1-uv.y,_FlipY);
    half3 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
    return half4(color,1); // Opaque alpha keeps passthrough out of the screen.
   }
   ENDHLSL
  }
 }
 Fallback Off
}
