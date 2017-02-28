// -------------------------------------------------------------------
//  Mobile surface shader
// -------------------------------------------------------------------

#ifndef WFA_MOBILE_SURF_INCLUDED
#define WFA_MOBILE_SURF_INCLUDED

#include "WFACG.cginc"

struct appdata_t {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD0;
	float2 texcoord1 : TEXCOORD1;
	float2 texcoord2 : TEXCOORD2;
	#if defined(_CHANNEL_COLOR)
		half4 channel : COLOR;
	#elif defined(_CHANNEL_UV3)
		half4 channel : TEXCOORD3;
	#endif
};

struct Input {
	float2 uv_MainTex;
	half3 distance : TEXCOORD1;
};

sampler2D _MainTex;
fixed4 _Color;

void vert(inout appdata_t v, out Input o){
	UNITY_INITIALIZE_OUTPUT(Input, o);

	#if defined(_CHANNEL_COLOR) || defined(_CHANNEL_UV3)
		o.distance = WFAVert(v.channel);
	#endif
}

void surf (Input i, inout SurfaceOutput o) {
	fixed4 col = tex2D(_MainTex, i.uv_MainTex)*_Color;
	fixed4 surfCol = col;

	half mask, fade, wire;
	half3 thickness;
	WFAWire(i.distance, i.uv_MainTex, mask, thickness, wire);

	#ifdef _GLOW_ON
		half3 emission = half3(0, 0, 0);
		WFAGlow(i.distance, thickness, mask, fade, col, emission);
	#endif

	half4 wireCol = WFAWireColor(i.distance, thickness, i.uv_MainTex);
					
	// BLEND
	wireCol.rgb = lerp(surfCol.rgb, wireCol.rgb, wireCol.a); // alpha blend wire to surface
	half transparency = lerp(surfCol.a*_WTransparency, _WTransparency, wireCol.a);

	// WIRELERP
	col.rgb = lerp(wireCol.rgb, col.rgb, wire);
	col.a = lerp(transparency, col.a, wire);

	o.Albedo = col.rgb;
	o.Alpha = col.a;
}

#endif // WFA_MOBILE_SURF_INCLUDED