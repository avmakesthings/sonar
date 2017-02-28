// -------------------------------------------------------------------
//  Forward add pass. Based on code in UnityStandardCore.cginc.
// -------------------------------------------------------------------

#ifndef WFA_FORWARD_ADD_INCLUDED
#define WFA_FORWARD_ADD_INCLUDED

#include "UnityStandardCore.cginc"
#include "WFACG.cginc"

// Unity 5.5 Compatibility
#if UNITY_VERSION >= 550
	#define ADDITIVELIGHT AdditiveLight(IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i))
#else
	#define ADDITIVELIGHT AdditiveLight(s.normalWorld, IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i))
#endif

// Wireframe data stored in fogCoord.yzw
struct VertexOutputWFA{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndLightDir[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:lightDir]
	LIGHTING_COORDS(5,6)

	half4 fogCoord                     : TEXCOORD7;

	UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputWFA vert(VertexInput v
    #if defined(_CHANNEL_COLOR)
	    ,half4 channel : COLOR
    #elif defined(_CHANNEL_UV3)
	    ,half4 channel : TEXCOORD3
    #endif
	){
	VertexOutputWFA o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputWFA, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.pos = UnityObjectToClipPos(v.vertex);
		
	o.tex = TexCoords(v);
	o.eyeVec = posWorld.xyz - _WorldSpaceCameraPos;// Don't normalize here, because we use it for fading the wireframe
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
	#ifdef _TANGENT_TO_WORLD
		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
		o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
		o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
		o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
	#else
		o.tangentToWorldAndLightDir[0].xyz = 0;
		o.tangentToWorldAndLightDir[1].xyz = 0;
		o.tangentToWorldAndLightDir[2].xyz = normalWorld;
	#endif
	//We need this for shadow receiving
	TRANSFER_VERTEX_TO_FRAGMENT(o);

	float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
	#ifndef USING_DIRECTIONAL_LIGHT
		lightDir = NormalizePerVertexNormal(lightDir);
	#endif
	o.tangentToWorldAndLightDir[0].w = lightDir.x;
	o.tangentToWorldAndLightDir[1].w = lightDir.y;
	o.tangentToWorldAndLightDir[2].w = lightDir.z;

    #if defined(_CHANNEL_COLOR) || defined(_CHANNEL_UV3)
		half3 distance = WFAVert(channel);
		o.fogCoord.yzw = distance;
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
		length(i.eyeVec),
		#endif
		albedo, alpha, emission, metallic, SMOOTHNESS, wire);

	#if defined(_ALPHATEST_ON)
		clip(alpha - _Cutoff); 
	#endif
	
	i.tex = Parallax(i.tex, IN_VIEWDIR4PARALLAX_FWDADD(i));
	FragmentCommonData s = (FragmentCommonData)0;
	s.diffColor = DiffuseAndSpecularFromMetallic (albedo, metallic, /*out*/ s.specColor, /*out*/ s.oneMinusReflectivity);
	s.SMOOTHNESS = SMOOTHNESS;
	s.normalWorld = PerPixelWorldNormal(i.tex, i.tangentToWorldAndLightDir);
	s.eyeVec = normalize(i.eyeVec);
	s.posWorld = half3(0,0,0);
	s.diffColor = PreMultiplyAlpha(s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);

	#if defined(_WLIGHT_OVERLAY) && defined(_NORMALMAP)
		// Don't bump an overlay wireframe
		s.normalWorld = lerp(normalize(i.tangentToWorldAndLightDir[2].xyz), s.normalWorld, wire);
	#endif

	UnityLight light = ADDITIVELIGHT;

	UnityIndirect noIndirect = ZeroIndirect ();

	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.SMOOTHNESS, s.normalWorld, -s.eyeVec, light, noIndirect);
	
	#ifdef _WLIGHT_UNLIT
		c = lerp(half4(0.0,0.0,0.0,0.0), c, wire);
	#endif

	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
	return OutputForward (c, s.alpha);
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
		i0.tangentToWorldAndLightDir[2].xyz *= -1; 
		i1.tangentToWorldAndLightDir[2].xyz *= -1;
		i2.tangentToWorldAndLightDir[2].xyz *= -1;

		// Emit triangle with different winding order
		stream.Append(i2);
		stream.Append(i0);
		stream.Append(i1);
	#endif
}

#endif //WFA_DX11


#endif // WFA_FORWARD_ADD_INCLUDED