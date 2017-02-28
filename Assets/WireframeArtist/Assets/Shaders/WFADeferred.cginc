// -------------------------------------------------------------------
//  Deferred rendering pass. Based on code in UnityStandardCore.cginc.
// -------------------------------------------------------------------

#ifndef WFA_DEFERRED_INCLUDED
#define WFA_DEFERRED_INCLUDED

#include "UnityStandardCore.cginc"
#include "WFACG.cginc"

// Unity 5.5 Compatibility
#if UNITY_VERSION >= 550
	#define DUMMYLIGHT DummyLight()
#else
	#define DUMMYLIGHT DummyLight(s.normalWorld)
#endif

// Unity 5.6 Compatibility
#if UNITY_VERSION >= 560
	#define TNGNTTOWRLD tangentToWorldAndPackedData
#else
	#define TNGNTTOWRLD tangentToWorldAndParallax
#endif

// Wireframe data stored in TNGNTTOWRLD[_].w
struct VertexOutputWFA{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 TNGNTTOWRLD[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:distance]
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UVs			

	#if UNITY_REQUIRE_FRAG_WORLDPOS
		float3 posWorld					: TEXCOORD6;
	#endif

	#if UNITY_OPTIMIZE_TEXCUBELOD
		#if UNITY_SPECCUBE_BOX_PROJECTION
			half3 reflUVW				: TEXCOORD7;
		#else
			half3 reflUVW				: TEXCOORD6;
		#endif
	#endif

	UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputWFA vert(VertexInput v
    #if defined(_CHANNEL_COLOR)
	    ,half4 channel : COLOR
    #elif defined(_CHANNEL_UV3)
	    ,half4 channel : TEXCOORD3
    #endif
	){
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputWFA o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputWFA, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	#if UNITY_REQUIRE_FRAG_WORLDPOS
		o.posWorld = posWorld;
	#endif
	o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords(v);
	o.eyeVec = posWorld.xyz - _WorldSpaceCameraPos; // Don't normalize here, because we use it for fading the wireframe
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
	#ifdef _TANGENT_TO_WORLD
		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
		o.TNGNTTOWRLD[0].xyz = tangentToWorld[0];
		o.TNGNTTOWRLD[1].xyz = tangentToWorld[1];
		o.TNGNTTOWRLD[2].xyz = tangentToWorld[2];
	#else
		o.TNGNTTOWRLD[0].xyz = 0;
		o.TNGNTTOWRLD[1].xyz = 0;
		o.TNGNTTOWRLD[2].xyz = normalWorld;
	#endif

	o.ambientOrLightmapUV = 0;
	#ifdef LIGHTMAP_ON
		o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#elif UNITY_SHOULD_SAMPLE_SH
		o.ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, o.ambientOrLightmapUV.rgb);
	#endif
	#ifdef DYNAMICLIGHTMAP_ON
		o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif

	#if UNITY_OPTIMIZE_TEXCUBELOD
		o.reflUVW		= reflect(o.eyeVec, normalWorld);
	#endif

    #if defined(_CHANNEL_COLOR) || defined(_CHANNEL_UV3)
    	half3 distance = WFAVert(channel);
		o.TNGNTTOWRLD[0].w = distance.x;
		o.TNGNTTOWRLD[1].w = distance.y;
		o.TNGNTTOWRLD[2].w = distance.z;
    #endif

	return o;
}

void frag (
	VertexOutputWFA i,
	out half4 outDiffuse : SV_Target0,			// RT0: diffuse color (rgb), occlusion (a)
	out half4 outSpecSmoothness : SV_Target1,	// RT1: spec color (rgb), smoothness (a)
	out half4 outNormal : SV_Target2,			// RT2: normal (rgb), --unused, very low precision-- (a)
	out half4 outEmission : SV_Target3			// RT3: emission (rgb), --unused-- (a)
)
{
	#if (SHADER_TARGET < 30)
		outDiffuse = 1;
		outSpecSmoothness = 1;
		outNormal = 0;
		outEmission = 0;
		return;
	#endif

	half alpha, metallic, SMOOTHNESS, wire;
	half3 emission, albedo;
	half3 distance = half3(i.TNGNTTOWRLD[0].w, i.TNGNTTOWRLD[1].w, i.TNGNTTOWRLD[2].w);
	WFAFrag(distance, i.tex,
		#ifdef _FADE_ON
		length(i.eyeVec),
		#endif
		albedo, alpha, emission, metallic, SMOOTHNESS, wire);

	#if defined(_ALPHATEST_ON)
		clip(alpha - _Cutoff); 
	#endif
		
	i.tex = Parallax(i.tex, IN_VIEWDIR4PARALLAX(i));
	FragmentCommonData s = (FragmentCommonData)0;
	s.diffColor = DiffuseAndSpecularFromMetallic (albedo, metallic, /*out*/ s.specColor, /*out*/ s.oneMinusReflectivity);
	s.SMOOTHNESS = SMOOTHNESS;
	s.normalWorld = PerPixelWorldNormal(i.tex, i.TNGNTTOWRLD);
	s.eyeVec = normalize(i.eyeVec);
	s.posWorld = WFA_IN_WORLDPOS(i);
	s.diffColor = PreMultiplyAlpha(s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);

	#if UNITY_OPTIMIZE_TEXCUBELOD
		s.reflUVW		= i.reflUVW;
	#endif

	half occlusion = Occlusion(i.tex.xy);
	#ifdef _WLIGHT_OVERLAY
		// Don't bump or occlude an overlay wireframe
		#ifdef _NORMALMAP
			s.normalWorld = lerp(normalize(i.TNGNTTOWRLD[2].xyz), s.normalWorld, wire);
		#endif
		occlusion = lerp(1.0, occlusion, wire);
	#endif

	// no analytic lights in this pass
	UnityLight dummyLight = DUMMYLIGHT;
	half atten = 1;

	// only GI
	#if UNITY_ENABLE_REFLECTION_BUFFERS
		bool sampleReflectionsInDeferred = false;
	#else
		bool sampleReflectionsInDeferred = true;
	#endif

	UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

	half3 color = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.SMOOTHNESS, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
	color += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.SMOOTHNESS, s.normalWorld, -s.eyeVec, occlusion, gi);

	#ifdef _EMISSION
		color += emission;
	#endif

	#ifndef UNITY_HDR_ON
		color.rgb = exp2(-color.rgb);
		#ifdef _WLIGHT_UNLIT
			albedo = exp2(-albedo);
		#endif
	#endif

	outDiffuse = half4(s.diffColor, occlusion);
	outSpecSmoothness = half4(s.specColor, s.SMOOTHNESS);
	
	#ifdef _WLIGHT_UNLIT
		outDiffuse = lerp(half4(0.0, 0.0, 0.0, 1.0), outDiffuse, wire);
		outSpecSmoothness = lerp(half4(0.0, 0.0, 0.0, 0.0), outSpecSmoothness, wire);
		albedo = PreMultiplyAlpha(albedo, alpha, s.oneMinusReflectivity, alpha);
		outEmission = half4(lerp(albedo, color, wire), 1.0);
	#else
		outEmission = half4(color, 1);
	#endif

	outNormal = half4(s.normalWorld*0.5 + 0.5, 1);
}


#ifdef WFA_DX11

inline void setDistance(inout VertexOutputWFA i, float3 distance) {
	i.TNGNTTOWRLD[0].w = distance.x;
	i.TNGNTTOWRLD[1].w = distance.y;
	i.TNGNTTOWRLD[2].w = distance.z;
}

#ifdef WFA_TWOSIDED
[maxvertexcount(6)]
#else
[maxvertexcount(3)]
#endif
void geom(triangle VertexOutputWFA i[3], inout TriangleStream<VertexOutputWFA> stream) {
	VertexOutputWFA i0, i1, i2; i0 = i[0]; i1 = i[1]; i2 = i[2];
	float3 d0, d1, d2;

	WFAgeom(i0.pos, i1.pos, i2.pos, /*out*/d0, /*out*/d1, /*out*/ d2);

	setDistance(i0, d0);
	setDistance(i1, d1);
	setDistance(i2, d2);
	
	stream.Append(i0);
	stream.Append(i1);
	stream.Append(i2);

	// Two-sided rendering
	#ifdef WFA_TWOSIDED 
		// Invert normals
		i0.TNGNTTOWRLD[2].xyz *= -1;
		i1.TNGNTTOWRLD[2].xyz *= -1;
		i2.TNGNTTOWRLD[2].xyz *= -1;

		// Emit triangle with different winding order
		stream.Append(i2);
		stream.Append(i0);
		stream.Append(i1);
	#endif
}

#endif //WFA_DX11

#endif // WFA_DEFERRED_INCLUDED