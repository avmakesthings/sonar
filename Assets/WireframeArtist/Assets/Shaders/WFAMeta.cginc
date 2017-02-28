// -------------------------------------------------------------------
//  Meta pass. Based on code in UnityStandardMeta.cginc.
// -------------------------------------------------------------------

#ifndef WFA_META_INCLUDED
#define WFA_META_INCLUDED

#include "UnityStandardMeta.cginc"
#include "WFACG.cginc"

struct VertexOutputWFA{
	float4 uv		: TEXCOORD0;
	float4 pos		: SV_POSITION;
	half3 distance 	: TEXCOORD1; // wireframe data
};

VertexOutputWFA vert (VertexInput v
    #if defined(_CHANNEL_COLOR)
	    ,half4 channel : COLOR
    #elif defined(_CHANNEL_UV3)
	    ,half4 channel : TEXCOORD3
    #endif
	){
	VertexOutputWFA o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputWFA, o);

	o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
	o.uv = TexCoords(v);

    #if defined(_CHANNEL_COLOR) || defined(_CHANNEL_UV3)
        o.distance = WFAVert(channel);
    #endif

	return o;
}

float4 frag(VertexOutputWFA i) : SV_Target{
	half alpha, metallic, SMOOTHNESS, oneMinusReflectivity, wire;
	half3 emission, albedo, specColor;
	WFAFrag(i.distance, i.uv, albedo, alpha, emission, metallic, SMOOTHNESS, wire);

	half3 diffColor = DiffuseAndSpecularFromMetallic (albedo, metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	UnityMetaInput o;
	UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

	o.Albedo = UnityLightmappingAlbedo (diffColor, specColor, SMOOTHNESS);

	#ifdef _WLIGHT_UNLIT
		o.Albedo = lerp(albedo, o.Albedo, wire);
		o.Emission = half3(0.0,0.0,0.0);
	#else
		o.Emission = emission;
	#endif

	return float4(UnityMetaFragment(o).rgb, alpha);
}


#ifdef WFA_DX11

#ifdef WFA_TWOSIDED
	[maxvertexcount(6)]
#else
	[maxvertexcount(3)]
#endif
void geom(triangle VertexOutputWFA i[3], inout TriangleStream<VertexOutputWFA> stream) {
	VertexOutputWFA i0, i1, i2; i0 = i[0]; i1 = i[1]; i2 = i[2];
	
	WFAgeom(i0.pos, i1.pos, i2.pos,
		/*out*/i0.distance, /*out*/i1.distance, /*out*/ i2.distance);
	
	stream.Append(i0);
	stream.Append(i1);
	stream.Append(i2);
	
	// Two-sided rendering
	#ifdef WFA_TWOSIDED
		// Emit triangle with different winding order
		stream.Append(i0);
		stream.Append(i2);
		stream.Append(i1);
	#endif
}

#endif //WFA_DX11


#endif // WFA_META_INCLUDED