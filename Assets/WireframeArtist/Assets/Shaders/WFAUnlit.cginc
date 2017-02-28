// -------------------------------------------------------------------
//  Unlit pass.
// -------------------------------------------------------------------

#ifndef WFA_UNLIT_INCLUDED
#define WFA_UNLIT_INCLUDED

#include "UnityCG.cginc"
#include "WFACG.cginc"

struct appdata_t {
	float4 vertex : POSITION;
	float2 texcoord : TEXCOORD0;
	#if defined(_CHANNEL_COLOR)
		half4 channel : COLOR;
	#elif defined(_CHANNEL_UV3)
		half4 channel : TEXCOORD3;
	#endif
};

struct v2f {
	float4 pos : SV_POSITION;
	half2 texcoord : TEXCOORD0;
	half3 distance : TEXCOORD1;
	UNITY_FOG_COORDS(2)
	#ifdef _FADE_ON
		half3 eyeVec : TEXCOORD3;
	#endif

	UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _MainTex;
float4 _MainTex_ST;
fixed4 _Color;
#if defined(_ALPHATEST_ON)
	half _Cutoff;
#endif
			
v2f vert (appdata_t v){
	v2f o;
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	o.pos = UnityObjectToClipPos(v.vertex);
	o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

	#if defined(_CHANNEL_COLOR) || defined(_CHANNEL_UV3)
		o.distance = WFAVert(v.channel);
	#endif

	#ifdef _FADE_ON
		float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
		o.eyeVec = posWorld.xyz - _WorldSpaceCameraPos;
	#endif

	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}
			
fixed4 frag (v2f i) : SV_Target{
	fixed4 col = tex2D(_MainTex, i.texcoord)*_Color;
	fixed4 surfCol = col;

	half mask, fade, wire;
	half3 thickness;
	WFAWire(i.distance, i.texcoord, mask, thickness, wire);

	#ifdef _FADE_ON
		WFAFade(length(i.eyeVec), wire, fade);
	#endif

	#ifdef _GLOW_ON
		half3 emission = half3(0,0,0);
		WFAGlow(i.distance, thickness, mask, fade, col, emission);
	#endif

	half4 wireCol = WFAWireColor(i.distance, thickness, i.texcoord);
					
	// BLEND
	wireCol.rgb = lerp(surfCol.rgb, wireCol.rgb, wireCol.a); // alpha blend wire to surface
	half transparency = lerp(surfCol.a*_WTransparency, _WTransparency, wireCol.a);

	// WIRELERP
	col.rgb = lerp(wireCol.rgb, col.rgb, wire);
	col.a = lerp(transparency, col.a, wire);

	#if defined(_ALPHATEST_ON)
		clip(col.a - _Cutoff);
	#endif 

	UNITY_APPLY_FOG(i.fogCoord, col);
	return col;
}

#ifdef WFA_DX11
[maxvertexcount(3)]
void geom(triangle v2f i[3], inout TriangleStream<v2f> stream) {
	v2f i0, i1, i2; i0 = i[0]; i1 = i[1]; i2 = i[2];
	
	WFAgeom(i0.pos, i1.pos, i2.pos, 
		/*out*/i0.distance, /*out*/i1.distance, /*out*/i2.distance);
	
	stream.Append(i0);
	stream.Append(i1);
	stream.Append(i2);
}
#endif //WFA_DX11

#endif // WFA_UNLIT_INCLUDED