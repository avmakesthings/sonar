// -------------------------------------------------------------------
//  Helper functions and macros used in many wireframe shaders
// -------------------------------------------------------------------

#ifndef WFA_CG_INCLUDED
#define WFA_CG_INCLUDED

// -------------------------------------------------------------------
//  Macros

#ifdef _GLOW_ON
	// Lerp 3 times with factors w.x, w.y and w.z
	#define LERP3(c0, c1, w) lerp(c0, lerp(c0, lerp(c0, c1, w.x), w.y), w.z)
#endif

// FWIDTH_DIST: used for screen space and anti-aliasing (AA)
#ifdef _WSTYLE_DEFAULT
	#if defined(_MODE_SCREEN) || defined(_AA_ON)
		#if (SHADER_TARGET < 30) || defined(SHADER_API_MOBILE)
			#define FWIDTH_DIST clamp(fwidth(distance), 0.0, 0.05)
		#else
			#define FWIDTH_DIST fwidth(distance)
		#endif
	#else
		#define FWIDTH_DIST half3(0, 0, 0)
	#endif
#else
	#define FWIDTH_DIST half3(0, 0, 0)
#endif


#ifdef WFA_DX11
	// Distance from point p0 to line p1-p2
	#define DISTTOLINE(p0,p1,p2) length(cross(p0 - p1, p0 - p2)) / length(p2 - p1)
#endif


// Unity 5.5 Compatibility
#if UNITY_VERSION >= 550
	#define SMOOTHNESS smoothness
#else
	#define SMOOTHNESS oneMinusRoughness
	#define UNITY_REQUIRE_FRAG_WORLDPOS (UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME)
#endif

#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	#define WFA_IN_WORLDPOS(i) i.posWorld
#else
	#define WFA_IN_WORLDPOS(i) half3(0,0,0)
#endif

// -------------------------------------------------------------------
//  Material property declarations

// Wire
sampler2D _WTex;
half4 _WColor;
half _WTransparency;
half _WThickness;
#if !defined(_WLIGHT_UNLIT) && !defined(WFA_PROJECTOR)
	half _WEmission;
	half _WGloss;
	half _WMetal;
#endif
half _WParam;
sampler2D _WMask;
half _WInvert;
#ifdef _AA_ON
	half _AASmooth;
#endif

// Glow
#ifdef _GLOW_ON
	half4 _GColor;
	half _GEmission;
	half _GDist;
	half _GPower;
#endif

// Fade
#ifdef _FADE_ON
	half _FDist;
	half _FPow;
	half _FMode;
#endif

// -------------------------------------------------------------------
//  Vertex functions

// Decode data stored in COLOR or TEXCOORD3
inline half3 WFAVert(half4 channel) {
	#ifdef _CHANNEL_COLOR
		#ifdef _MODE_WORLD  
			half3 bary = step(1e-5, channel.rgb);	
			half3 mantissa = channel.rgb + 254.0 / 255.0;
			half exponent = channel.a*255.0 - 128.0;
			return bary*mantissa*exp2(exponent);
		#else // barycentric
			return channel.rgb;
		#endif
	#else //_CHANNEL_UV3
		#ifdef _MODE_WORLD  
			half2 uv_step = step(1e-5, channel);
			half z = uv_step.x*uv_step.y;
			half x = uv_step.x - z;
			half y = uv_step.y - z;
			return half3(x, y, z)*channel.xyx;
		#else // barycentric
			uint idx = uint(abs(channel.y));
			return half3(idx % 2, (idx / 2) % 2, idx / 4);
		#endif
	#endif
}

// -------------------------------------------------------------------
//  Fragment functions

// Calculate wire threshold. 0: wire, 1: no wire, (0-1): wire edge for anti-aliasing
inline half WFAWireTheshhold(half3 distance, half3 thickness, half3 fwidth_dist) {
	half3 df; // distance field
	half wire; // threshold

	#if defined(_WSTYLE_DEFAULT)
		df = distance - thickness;

		#ifdef _AA_ON
			df /= _AASmooth * fwidth_dist + 1e-6;
			wire = min(df.x, min(df.y, df.z));
			wire = smoothstep(0.0, 1.0, wire + 0.5);
		#else
			wire = min(df.x, min(df.y, df.z));
			wire = step(0.0, wire);
		#endif // _AA_ON
	#else
		df = distance / (thickness*2.0  + 1e-6);
		df += _WParam;
		wire = df.x*df.y*df.z - 0.5;

		#ifdef _AA_ON
			wire /= _AASmooth * fwidth(wire) + 1e-6;
			wire = smoothstep(0.0, 1.0, wire + 0.5);
		#else
			wire = step(0.0, wire);
		#endif // _AA_ON
	#endif
	
	return wire;
}

// Handles wire thickness, wire threshold and mask
inline void WFAWire(half3 distance, half2 uv, out half mask, out half3 thickness, out half wire) {
	thickness = half3(_WThickness, _WThickness, _WThickness);
	half3 fwidth_dist = FWIDTH_DIST;
	#if defined(_MODE_SCREEN) && defined(_WSTYLE_DEFAULT)
		thickness *= fwidth_dist * 10.0;
	#endif

	wire = WFAWireTheshhold(distance, thickness, fwidth_dist);

	#ifdef WFA_PROJECTOR
		mask = 1.0;
	#else
		mask = tex2D(_WMask, uv).g;
	#endif
	wire = _WInvert - (_WInvert*2.0-1.0)*lerp(1.0, wire, mask);
}

#if !defined(WFA_SHADOW_INCLUDED) 
// Get surface properties
inline void WFASurface(half4 uv, out half4 col, inout half3 emission, inout half metallic, inout half smoothness) {
	#if defined(WFA_PROJECTOR) || defined(WFA_UNLIT)
		col = half4(0.0, 0.0, 0.0, 0.0);
		emission = half3(0.0, 0.0, 0.0);
		metallic = 0.0;
		smoothness = 0.0;
	#else
		col = half4(Albedo(uv), Alpha(uv.xy));
		emission = Emission(uv.xy);
		half2 metallicGloss = MetallicGloss(uv.xy);
		metallic = metallicGloss.x;
		smoothness = metallicGloss.y;
	#endif
}
#endif

#ifdef _FADE_ON
// Fade out the wire depending on the camera distance
inline void WFAFade(half camDist, inout half wire, out half fade) {
	fade = saturate(exp2(-pow(_FDist*max(0, camDist), _FPow)));
	wire = lerp(_FMode, wire, fade);
}
#endif

#ifdef _GLOW_ON
// Calculate the glow coming from the wire edges
inline void WFAGlow(half3 distance, half3 thickness, half mask, half fade, inout half4 col, inout half3 emission) {
	
	half3 df; // glow distance field
	#ifdef _WSTYLE_DEFAULT
		df = max(0, distance - thickness*0.95);
	#else
		df = distance / (thickness*2.0 + 1e-6) + _WParam;
	#endif
	df /= _GDist + 1e-6;
	df = smoothstep(0.0, 1.0, sqrt(df));

	// color
	half4 glowCol = _GColor*mask;
	half blend = glowCol.a*_GPower*(1.0-_WInvert);
	#ifdef _FADE_ON
		blend *= fade;
	#endif 
	//BLEND
	glowCol.rgb = lerp(col.rgb, glowCol.rgb, blend);
	glowCol.a = lerp(col.a, 1.0, blend);
	// LERP
	col = LERP3(glowCol, col, df);
	
	// emission
	#ifndef _WLIGHT_UNLIT
		half3 glowEmi = _GColor.rgb*_GEmission;
		#ifdef WFA_PASS_META
			// The Meta pass seems to only look at fragments at vertex points
			emission += glowEmi;
		#elif defined(_EMISSION)
			emission = lerp(emission, LERP3(glowEmi, emission, df), blend);
		#endif
	#endif
}
#endif

// Get the wireframe albedo and alpha
inline half4 WFAWireColor(half3 distance, half3 thickness, half2 uv) {
	#ifdef _WUV_BARYCENTRIC
		#ifdef _WSTYLE_DEFAULT
			half3 df = distance / thickness;
			half u = min(df.x, min(df.y, df.z));
		#else
			half3 df = distance / (_WThickness*1.5) + _WParam;
			half u = df.x*df.y*df.z;
		#endif
		// Set _WTex to clamp mode for best results
		half4 texcol = tex2D(_WTex, half2(saturate(u), 0.5)); 
		half4 wireCol = _WColor*texcol;
	#else // UV0
		half4 wireCol = _WColor*tex2D(_WTex, uv);
	#endif
	return wireCol;
}

#ifndef WFA_PROJECTOR
// Blend the wireframe properties with the surface properties, then lerp according to the wire threshold
inline void WFABlendLerp(half4 surfCol, half3 surfEmi, half4 wireCol, half wire, inout half4 col, 
	inout half3 emission, inout half metallic, inout half smoothness) {
	half blend = wireCol.a;

	// BLEND
	wireCol.rgb = lerp(surfCol.rgb, wireCol.rgb, blend); // alpha blend wire to surface
	half transparency = lerp(surfCol.a*_WTransparency, _WTransparency, blend);

	// WIRELERP
	col.rgb = lerp(wireCol.rgb, col.rgb, wire);
	col.a = lerp(transparency, col.a, wire);

	#ifndef _WLIGHT_UNLIT
		// BLEND
		half wireMetal = lerp(metallic, _WMetal, blend);
		half wireSmooth = lerp(smoothness, _WGloss, blend);

		// WIRELERP
		metallic = lerp(wireMetal, metallic, wire);
		#ifdef _AA_ON
			// Just lerping the smoothness looks strange with anti-aliasing
			// this code tones down the AA when wire smoothness > surface smoothness
			half smoothnesslerp = lerp(0.1, 0.9, wireSmooth > smoothness); 
			smoothnesslerp = step(smoothnesslerp, wire);
		#else
			half smoothnesslerp = wire;
		#endif
		smoothness = lerp(wireSmooth, smoothness, smoothnesslerp);

		#ifdef WFA_PASS_META
			// The Meta pass seems to only look at fragments at vertex points
			emission += _WEmission * wireCol.rgb * transparency * blend;
		#elif defined(_EMISSION)
			// BLEND
			half3 wireEmi = lerp(surfEmi, _WEmission*wireCol.rgb, blend);
			// WIRELERP
			emission = lerp(emission, lerp(wireEmi, emission, wire), col.a);
		#endif
	#endif
}
#endif

#if !defined(WFA_SHADOW_INCLUDED)
// The root fragment function
inline void WFAFrag(half3 distance, half4 uv,
	#ifdef _FADE_ON
		half camDist,
	#endif
	out half3 albedo, out half alpha, out half3 emission, out half metallic, out half smoothness, out half wire) {
	
	half4 col;
	WFASurface(uv, col, emission, metallic, smoothness);
	half3 surfEmi = emission;
	half4 surfCol = col;

	half mask, fade;
	half3 thickness;
	WFAWire(distance, uv.xy, mask, thickness, wire);

	#ifdef _FADE_ON
		WFAFade(camDist, wire, fade);
	#endif

	#ifdef _GLOW_ON
		WFAGlow(distance, thickness, mask, fade, col, emission);
	#endif

	half4 wireCol = WFAWireColor(distance, thickness, uv.xy);

	#ifdef WFA_PROJECTOR
		albedo = lerp(wireCol.rgb, col.rgb, wire);
		alpha = lerp(_WTransparency, col.a, wire);
	#else
		WFABlendLerp(surfCol, surfEmi, wireCol, wire, col, emission, metallic, smoothness);
		albedo = col.rgb;
		alpha = col.a;
	#endif
}
#endif

#if defined(WFA_SHADOW_INCLUDED)
// Fragment code for the shadow pass, which only cares about alpha
inline half WFAlpha(half3 distance, half2 uv
	#ifdef _FADE_ON
		,half camDist
	#endif
	){

	half mask, wire;
	half3 thickness;
	WFAWire(distance, uv, mask, thickness, wire);
	#ifdef _FADE_ON
		half fade;
		WFAFade(camDist, wire, fade);
	#endif // _FADE_ON
	half surfAlpha = tex2D(_MainTex, uv).a * _Color.a;
	half4 wireAlpha = WFAWireColor(distance, thickness, uv.xy).a;

	half transparency = lerp(surfAlpha*_WTransparency, _WTransparency, wireAlpha*mask);
	return lerp(transparency, surfAlpha, wire);
}
#endif


// -------------------------------------------------------------------
//  Geometry functions (shader model 5.0)

#ifdef WFA_DX11
// Calculate barycentric distances
inline void WFAgeom(float3 p0, float3 p1, float3 p2, out float3 d0, out float3 d1, out float3 d2) {
	float4 d;

	#if defined(_MODE_DEFAULT) || defined(_MODE_WORLD)
		d = float4(0.0,
			DISTTOLINE(p2, p0, p1),
			DISTTOLINE(p0, p1, p2),
			DISTTOLINE(p1, p0, p2));
		#ifdef _MODE_DEFAULT
			d /= min(d.y, min(d.z, d.w));
		#endif
	#else
		d = float4(0.0, 1.0, 1.0, 1.0); //barycentric or screen space
	#endif

	d0 = d.xzx;
	d1 = d.xxw;
	d2 = d.yxx;
}
#endif //WFA_DX11

#endif // WFA_CG_INCLUDED