// -------------------------------------------------------------------
//  Shadow caster pass. Based on code in UnityStandardShadow.cginc.
// -------------------------------------------------------------------

#ifndef WFA_SHADOW_INCLUDED
#define WFA_SHADOW_INCLUDED

#include "UnityStandardShadow.cginc"
#include "WFACG.cginc"


struct VertexOutputWFA{
	V2F_SHADOW_CASTER_NOPOS
	float2 tex : TEXCOORD1;
	half3 distance : TEXCOORD2; // wireframe data
	#ifdef _FADE_ON
		half3 eyeVec : TEXCOORD3;
	#endif
	#ifdef WFA_DX11
		float4 _pos : SV_POSITION;
	#endif
};

void vert(VertexInput v
    #if defined(_CHANNEL_COLOR)
	    ,half4 channel : COLOR
    #elif defined(_CHANNEL_UV3)
	    ,half4 channel : TEXCOORD3
    #endif
	,out VertexOutputWFA o
	#ifndef WFA_DX11
	, out float4 opos : SV_POSITION
	#endif
	){

	UNITY_SETUP_INSTANCE_ID(v);

	o = (VertexOutputWFA)0;
	TRANSFER_SHADOW_CASTER_NOPOS(o, 
		#ifndef WFA_DX11
			opos
		#else
			o._pos
		#endif
		)
	#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
		o.tex = TRANSFORM_TEX(v.uv0, _MainTex);
	#endif

	#ifdef _FADE_ON
		o.eyeVec = mul(unity_ObjectToWorld, v.vertex) - _WorldSpaceCameraPos;
	#endif

    #if defined(_CHANNEL_COLOR) || defined(_CHANNEL_UV3)
        o.distance = WFAVert(channel);
    #endif
}


half4 frag(VertexOutputWFA i
	#if defined(UNITY_STANDARD_USE_DITHER_MASK) && !defined(WFA_DX11)
	, UNITY_VPOS_TYPE vpos : VPOS
	#endif
	) : SV_Target{
	#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
		half alpha = WFAlpha(i.distance, i.tex
			#ifdef _FADE_ON
				,length(i.eyeVec)
			#endif
			);
		#if defined(_ALPHATEST_ON)
			clip (alpha - _Cutoff);
		#endif
		#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
			#if defined(UNITY_STANDARD_USE_DITHER_MASK)
				#if defined(_ALPHAPREMULTIPLY_ON) && (UNITY_VERSION >= 550)
					half outModifiedAlpha;
					PreMultiplyAlpha(half3(0, 0, 0), alpha, SHADOW_ONEMINUSREFLECTIVITY(i.tex), outModifiedAlpha);
					alpha = outModifiedAlpha;
				#endif
				#ifdef WFA_DX11
					float4 vpos = i._pos;
				#endif
				// Use dither mask for alpha blended shadows, based on pixel position xy
				// and alpha level. Our dither texture is 4x4x16.
				half alphaRef = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25,alpha*0.9375)).a;
				clip (alphaRef - 0.01);
			#else
				clip (alpha - _Cutoff);
			#endif
		#endif
	#endif // #if defined(UNITY_STANDARD_USE_SHADOW_UVS)

	SHADOW_CASTER_FRAGMENT(i)
}	

#ifdef WFA_DX11

#ifdef WFA_TWOSIDED
	[maxvertexcount(6)]
#else
	[maxvertexcount(3)]
#endif
void geom(triangle VertexOutputWFA i[3], inout TriangleStream<VertexOutputWFA> stream) {
	VertexOutputWFA i0, i1, i2; i0 = i[0]; i1 = i[1]; i2 = i[2];
	
	WFAgeom(i0._pos, i1._pos, i2._pos,
		/*out*/i0.distance, /*out*/i1.distance, /*out*/ i2.distance);
	
	stream.Append(i0);
	stream.Append(i1);
	stream.Append(i2);

	// Two-sided rendering
	#ifdef WFA_TWOSIDED
		// Emit triangle with different winding order
		stream.Append(i2);
		stream.Append(i0);
		stream.Append(i1);
	#endif
}

#endif //WFA_DX11


#endif // WFA_SHADOW_INCLUDED