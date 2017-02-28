// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// -------------------------------------------------------------------
//  Mobile Forward Pass, code based on generated surface shader.
// -------------------------------------------------------------------

#ifndef WFA_MOBILE_FORWARD_INCLUDED
#define WFA_MOBILE_FORWARD_INCLUDED

#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"

#define UNITY_PASS_FORWARDBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

#include "WFAMobileSurf.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

// vertex-to-fragment interpolation data
// no lightmaps:
#ifndef LIGHTMAP_ON
struct v2f_surf {
	float4 pos : SV_POSITION;
	float2 pack0 : TEXCOORD0; // _MainTex
	half3 worldNormal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
	half3 custompack0 : TEXCOORD3; // distance
	#if UNITY_SHOULD_SAMPLE_SH
	half3 sh : TEXCOORD4; // SH
	#endif
	SHADOW_COORDS(5)
	UNITY_FOG_COORDS(6)
	#if SHADER_TARGET >= 30
	float4 lmap : TEXCOORD7;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif
// with lightmaps:
#ifdef LIGHTMAP_ON
struct v2f_surf {
	float4 pos : SV_POSITION;
	float2 pack0 : TEXCOORD0; // _MainTex
	half3 worldNormal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
	half3 custompack0 : TEXCOORD3; // distance
	float4 lmap : TEXCOORD4;
	SHADOW_COORDS(5)
	UNITY_FOG_COORDS(6)
	#ifdef DIRLIGHTMAP_COMBINED
	fixed3 tSpace0 : TEXCOORD7;
	fixed3 tSpace1 : TEXCOORD8;
	fixed3 tSpace2 : TEXCOORD9;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif
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
	o.pos = UnityObjectToClipPos(v.vertex);
	o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
	#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
	fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
	fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
	fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
	#endif
	#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
	o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
	o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
	o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
	#endif
	o.worldPos = worldPos;
	o.worldNormal = worldNormal;
	#ifdef DYNAMICLIGHTMAP_ON
	o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
	#ifdef LIGHTMAP_ON
	o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#endif

	// SH/ambient and vertex lights
	#ifndef LIGHTMAP_ON
	#if UNITY_SHOULD_SAMPLE_SH
		o.sh = 0;
		// Approximated illumination from non-important point lights
		#ifdef VERTEXLIGHT_ON
		o.sh += Shade4PointLights (
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, worldPos, worldNormal);
		#endif
		o.sh = ShadeSHPerVertex (worldNormal, o.sh);
	#endif
	#endif // LIGHTMAP_OFF

	TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
	UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
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
	o.Normal = IN.worldNormal;
	normalWorldVertex = IN.worldNormal;

	// call surface function
	surf (surfIN, o);

	// alpha test
	#ifdef _ALPHATEST_ON
		clip (o.Alpha - _Cutoff);
	#endif

	// compute lighting & shadowing factor
	UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
	fixed4 c = 0;

	// Setup lighting environment
	UnityGI gi;
	UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
	gi.indirect.diffuse = 0;
	gi.indirect.specular = 0;
	#if !defined(LIGHTMAP_ON)
		gi.light.color = _LightColor0.rgb;
		gi.light.dir = lightDir;
		gi.light.ndotl = LambertTerm (o.Normal, gi.light.dir);
	#endif
	// Call GI (lightmaps/SH/reflections) lighting function
	UnityGIInput giInput;
	UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
	giInput.light = gi.light;
	giInput.worldPos = worldPos;
	giInput.atten = atten;
	#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
	giInput.lightmapUV = IN.lmap;
	#else
	giInput.lightmapUV = 0.0;
	#endif
	#if UNITY_SHOULD_SAMPLE_SH
	giInput.ambient = IN.sh;
	#else
	giInput.ambient.rgb = 0.0;
	#endif
	giInput.probeHDR[0] = unity_SpecCube0_HDR;
	giInput.probeHDR[1] = unity_SpecCube1_HDR;
	#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
	giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
	#endif
	#if UNITY_SPECCUBE_BOX_PROJECTION
	giInput.boxMax[0] = unity_SpecCube0_BoxMax;
	giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
	giInput.boxMax[1] = unity_SpecCube1_BoxMax;
	giInput.boxMin[1] = unity_SpecCube1_BoxMin;
	giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
	#endif
	LightingLambert_GI(o, giInput, gi);

	// realtime lighting: call lighting function
	c += LightingLambert (o, gi);
	UNITY_APPLY_FOG(IN.fogCoord, c); // apply fog
	return c;
}

#endif // WFA_MOBILE_FORWARD_INCLUDED