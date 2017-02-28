// -------------------------------------------------------------------
//  Projector vertex and fragment code
// -------------------------------------------------------------------

#ifndef WFA_PROJECTOR_INCLUDED
#define WFA_PROJECTOR_INCLUDED

#include "UnityCG.cginc"
#include "WFACG.cginc"

struct appdata{
	float4 vertex : POSITION;
	float2 uv0 : TEXCOORD0;
	#if defined(_CHANNEL_COLOR)
		half4 channel : COLOR;
	#elif defined(_CHANNEL_UV3)
		half4 channel : TEXCOORD3;
	#endif
};

struct VertexOutputWFA {
	float4 pos : SV_POSITION;
	float4 projUV : TEXCOORD0;
	float2 tex : TEXCOORD1;
	float3 distance : TEXCOORD2;
	UNITY_FOG_COORDS(3)
};

float4x4 unity_Projector;
float4 _WTex_ST;

VertexOutputWFA vert(appdata v){
	VertexOutputWFA o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputWFA, o);

	o.pos = UnityObjectToClipPos(v.vertex);
	o.projUV = mul(unity_Projector, v.vertex);
	o.tex = TRANSFORM_TEX(v.uv0, _WTex);

	#if defined(_CHANNEL_COLOR) || defined(_CHANNEL_UV3)
		o.distance = WFAVert(v.channel);
	#endif

	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

half4 frag(VertexOutputWFA i) : SV_Target {
	half alpha, metallic, SMOOTHNESS, wire;
	half3 emission, albedo;
	WFAFrag(i.distance, half4(i.tex,0,0), albedo, alpha, emission, metallic, SMOOTHNESS, wire);

	half mask = tex2Dproj(_WMask, UNITY_PROJ_COORD(i.projUV)).g;
	half4 col = half4(albedo, alpha)*mask;

	UNITY_APPLY_FOG(i.fogCoord, col);
	return col;
}

#ifdef WFA_DX11
[maxvertexcount(3)]
void geom(triangle VertexOutputWFA i[3], inout TriangleStream<VertexOutputWFA> stream) {
	VertexOutputWFA i0, i1, i2; i0 = i[0]; i1 = i[1]; i2 = i[2];
	
	WFAgeom(i0.pos, i1.pos, i2.pos, 
		/*out*/i0.distance, /*out*/i1.distance, /*out*/i2.distance);

	stream.Append(i0);
	stream.Append(i1);
	stream.Append(i2);
}
#endif //WFA_DX11

#endif //WFA_PROJECTOR_INCLUDED