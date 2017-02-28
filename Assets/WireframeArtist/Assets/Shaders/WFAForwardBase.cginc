// -------------------------------------------------------------------
//  Forward base pass. Based on code in UnityStandardCore.cginc.
// -------------------------------------------------------------------

#ifndef WFA_FORWARD_BASE_INCLUDED
#define WFA_FORWARD_BASE_INCLUDED

#include "UnityStandardCore.cginc"
#include "WFACG.cginc"

// Unity 5.5 Compatibility
#if UNITY_VERSION >= 550
	#define MAINLIGHT MainLight()
#else
	#define MAINLIGHT MainLight(s.normalWorld)
#endif

// Unity 5.6 Compatibility
#if UNITY_VERSION >= 560
	#define TNGNTTOWRLD tangentToWorldAndPackedData
#else
	#define TNGNTTOWRLD tangentToWorldAndParallax
#endif

// Wireframe data stored in fogCoord.yzw
struct VertexOutputWFA{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half4 eyeVec 						: TEXCOORD1;
	half4 TNGNTTOWRLD[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UV
	SHADOW_COORDS(6)

	half4 fogCoord						: TEXCOORD7;

	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
	#if UNITY_REQUIRE_FRAG_WORLDPOS
		float3 posWorld					: TEXCOORD8;
	#endif

	#if UNITY_OPTIMIZE_TEXCUBELOD
		#if UNITY_SPECCUBE_BOX_PROJECTION
			half3 reflUVW				: TEXCOORD9;
		#else
			half3 reflUVW				: TEXCOORD8;
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
		o.posWorld = posWorld.xyz;
	#endif
	o.pos = UnityObjectToClipPos(v.vertex);
		
	o.tex = TexCoords(v);
	o.eyeVec.xyz = posWorld.xyz - _WorldSpaceCameraPos;
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
	//We need this for shadow receving
	TRANSFER_SHADOW(o);

	o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

	#if UNITY_OPTIMIZE_TEXCUBELOD
		o.reflUVW 		= reflect(o.eyeVec.xyz, normalWorld);
	#endif

    #if defined(_CHANNEL_COLOR) || defined(_CHANNEL_UV3)
		half3 distance = WFAVert(channel);
		o.fogCoord.yzw = distance;
    #endif

	#ifdef WFA_PROJECTOR
		half4 projUV = mul(unity_Projector, v.vertex);
		o.TNGNTTOWRLD[0].w = projUV.x;
		o.TNGNTTOWRLD[1].w = projUV.y;
		o.TNGNTTOWRLD[2].w = projUV.z;
		o.eyeVec.w = projUV.w;
	#endif

	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}

half4 frag(VertexOutputWFA i) : SV_Target{
	half alpha, metallic, SMOOTHNESS, wire;
	half3 emission, albedo;
	half3 distance = i.fogCoord.yzw;
	WFAFrag(distance, i.tex,
		#ifdef _FADE_ON
		length(i.eyeVec.xyz),
		#endif
		albedo, alpha, emission, metallic, SMOOTHNESS, wire);

	#if defined(_ALPHATEST_ON)
		clip(alpha - _Cutoff); 
	#endif 
	
	i.tex = Parallax(i.tex, IN_VIEWDIR4PARALLAX(i));
	FragmentCommonData s = (FragmentCommonData)0;
	s.diffColor = DiffuseAndSpecularFromMetallic(albedo, metallic, /*out*/ s.specColor, /*out*/ s.oneMinusReflectivity);
	s.SMOOTHNESS = SMOOTHNESS;
	s.normalWorld = PerPixelWorldNormal(i.tex, i.TNGNTTOWRLD);
	s.eyeVec = normalize(i.eyeVec.xyz);
	s.posWorld = WFA_IN_WORLDPOS(i);
	s.diffColor = PreMultiplyAlpha (s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);

	#if UNITY_OPTIMIZE_TEXCUBELOD
		s.reflUVW = i.reflUVW;
	#endif

	half occlusion = Occlusion(i.tex.xy);
	#ifdef _WLIGHT_OVERLAY
		// Don't bump or occlude an overlay wireframe
		#ifdef _NORMALMAP
			s.normalWorld = lerp(normalize(i.TNGNTTOWRLD[2].xyz), s.normalWorld, wire);
		#endif
		occlusion = lerp(1.0, occlusion, wire);
	#endif

	UnityLight mainLight = MAINLIGHT;
	half atten = SHADOW_ATTENUATION(i);

	UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

	half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.SMOOTHNESS, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	c.rgb += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.SMOOTHNESS, s.normalWorld, -s.eyeVec, occlusion, gi);
	
	#ifdef _EMISSION
		c.rgb += emission;
	#endif

	#ifdef _WLIGHT_UNLIT
		albedo = PreMultiplyAlpha(albedo, alpha, s.oneMinusReflectivity, alpha);
		c.rgb = lerp(albedo, c.rgb, wire);
	#endif

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);
	return OutputForward(c, s.alpha);
}

#ifdef WFA_DX11

#define BARYDIST(i) i.fogCoord.yzw

#ifdef WFA_TWOSIDED
[maxvertexcount(6)]
#else
[maxvertexcount(3)]
#endif
void geom(triangle VertexOutputWFA i[3], inout TriangleStream<VertexOutputWFA> stream) {
	VertexOutputWFA i0, i1, i2; i0 = i[0]; i1 = i[1]; i2 = i[2];
	
	WFAgeom(i0.pos, i1.pos, i2.pos, 
		/*out*/BARYDIST(i0), /*out*/BARYDIST(i1), /*out*/BARYDIST(i2));
	
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


#endif // WFA_FORWARD_BASE_INCLUDED