// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// -------------------------------------------------------------------
//  Mobile Shadow Pass, code based on generated surface shader.
// -------------------------------------------------------------------

#ifndef WFA_MOBILE_SHADOW_INCLUDED
#define WFA_MOBILE_SHADOW_INCLUDED

#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"

#define UNITY_PASS_SHADOWCASTER
#include "UnityCG.cginc"
#include "Lighting.cginc"

#include "WFAMobileSurf.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

// vertex-to-fragment interpolation data
struct v2f_surf {
  V2F_SHADOW_CASTER;
  float2 pack0 : TEXCOORD1; // _MainTex
  float3 worldPos : TEXCOORD2;
  half3 custompack0 : TEXCOORD3; // distance
  UNITY_VERTEX_INPUT_INSTANCE_ID
};
float4 _MainTex_ST;

// vertex shader
v2f_surf vert_surf (appdata_t v) {
  UNITY_SETUP_INSTANCE_ID(v);
  v2f_surf o;
  UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
  UNITY_TRANSFER_INSTANCE_ID(v,o);
  Input customInputData;
  vert (v, customInputData);
  o.custompack0.xyz = customInputData.distance;
  o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
  fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
  o.worldPos = worldPos;
  TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
  return o;
}
fixed _Cutoff;

// fragment shader
fixed4 frag_surf (v2f_surf IN) : SV_Target {
  UNITY_SETUP_INSTANCE_ID(IN);
  // prepare and unpack data
  Input surfIN;
  UNITY_INITIALIZE_OUTPUT(Input,surfIN);
  surfIN.uv_MainTex.x = 1.0;
  surfIN.distance.x = 1.0;
  surfIN.uv_MainTex = IN.pack0.xy;
  surfIN.distance = IN.custompack0.xyz;
  float3 worldPos = IN.worldPos;
  #ifndef USING_DIRECTIONAL_LIGHT
    fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
  #else
    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
  #endif
  #ifdef UNITY_COMPILER_HLSL
  SurfaceOutput o = (SurfaceOutput)0;
  #else
  SurfaceOutput o;
  #endif
  o.Albedo = 0.0;
  o.Emission = 0.0;
  o.Specular = 0.0;
  o.Alpha = 0.0;
  o.Gloss = 0.0;
  fixed3 normalWorldVertex = fixed3(0,0,1);

  // call surface function
  surf (surfIN, o);

  // alpha test
  #ifdef _ALPHATEST_ON
	clip (o.Alpha - _Cutoff);
  #endif
  SHADOW_CASTER_FRAGMENT(IN)
}

#endif // WFA_MOBILE_SHADOW_INCLUDED