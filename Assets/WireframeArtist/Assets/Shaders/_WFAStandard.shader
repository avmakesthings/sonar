Shader "Wireframe Artist/Standard" {
	Properties {
		// Wire
		_WTex("Wire Texture", 2D) = "white" {}
		[KeywordEnum(UV0, Barycentric)] _WUV("Wire UV Set", Float) = 0.0
		_WColor("Wire Color", Color) = (0,0,1,1)
		_WTransparency("Wire Transparency", Range(0.0,1.0)) = 1.0
		_WOpacity("Wire Opacity", Range(0.0,1.0)) = 1.0
		_WEmission("Wire Emission", Float) = 0
		_WThickness("Wire Thickness", Float) = 0.05
		_WGloss("Wire Smoothness", Range(0.0,1.0)) = 0
		[Gamma] _WMetal("Wire Metallic", Range(0.0,1.0)) = 0.0
		_WParam("Wire Parameter", Float) = 0
		_WMode("Wire Mode", Float) = 0.0
		[KeywordEnum(Default, Smooth)] _WStyle("Wire Style", Float) = 0.0 
		_WMask("Wire Mask", 2D) = "white" {}
		[KeywordEnum(Surface, Overlay, Unlit)] _WLight("Wire Lighting Mode", Float) = 0.0
		_AASmooth("AA Smoothing", Float) = 1.5
		_WInvert("Wire Invert", Float) = 0.0
			
		// Glow
		[Toggle] _Glow("Glow Enable", Float) = 0.0
		_GColor("Glow Color", Color) = (0,0,0,1)
		_GEmission("Glow Emission", Float) = 0
		_GDist("Glow Distance", Float) = 0.35
		_GPower("Glow Power", Float) = 0.5

		// Fade
		[Toggle] _Fade("Glow Enable", Float) = 0.0
		_FMode("Fade Mode", Float) = 1
		_FDist("Fade Distance", Float) = 0.1
		_FPow("Fade Power", Float) = 5

		// Settings
		[KeywordEnum(UV3, Color)] _Channel("Channel", Float) = 0.0
		_Cutoff("Alpha Cutoff", Float) = 0.5
		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 4.0
		[HideInInspector] _Cull("__cull", Float) = 2.0
		[HideInInspector] _Fold("__fld", Float) = 1.0
		[HideInInspector] _Limits("__lmt", Float) = 1.0

		// Surface
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0
		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}
		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}
		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
	}
	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT MetallicSetup
		#define WFA_STANDARD
	ENDCG
	SubShader {
		Tags{ "RenderType" = "Opaque" "PerformanceChecks" = "False" }
		LOD 300
		Cull [_Cull]
		
		Pass{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite] 

			CGPROGRAM
			#pragma target 3.0
			
			#pragma shader_feature _GLOW_ON
			#pragma shader_feature _FADE_ON 
			#pragma shader_feature _AA_ON
			#pragma shader_feature _WLIGHT_OVERLAY _WLIGHT_SURFACE _WLIGHT_UNLIT
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR 
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vert
			#pragma fragment frag
			#include "WFAForwardBase.cginc"
			ENDCG
		}

		Pass{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			
			#pragma shader_feature _GLOW_ON
			#pragma shader_feature _FADE_ON 
			#pragma shader_feature _AA_ON
			#pragma shader_feature _WLIGHT_OVERLAY _WLIGHT_SURFACE _WLIGHT_UNLIT
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR 
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH
		
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP

			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog

			#pragma vertex vert
			#pragma fragment frag
			#include "WFAForwardAdd.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _FADE_ON 
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR 
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma multi_compile_shadowcaster

			#pragma vertex vert
			#pragma fragment frag

			#include "WFAShadow.cginc"

			ENDCG
		}

		Pass{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 3.0
			#pragma exclude_renderers nomrt
			
			#pragma shader_feature _GLOW_ON
			#pragma shader_feature _FADE_ON 
			#pragma shader_feature _AA_ON
			#pragma shader_feature _WLIGHT_OVERLAY _WLIGHT_SURFACE _WLIGHT_UNLIT
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR 
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH
		
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP

			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			#pragma multi_compile ___ DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON

			#pragma vertex vert
			#pragma fragment frag
			#include "WFADeferred.cginc"
			ENDCG
		}

		Pass{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature _GLOW_ON
			#pragma shader_feature _WLIGHT_OVERLAY _WLIGHT_SURFACE _WLIGHT_UNLIT
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR 
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP

			#define WFA_PASS_META
			#include "WFAMeta.cginc"
			ENDCG
		}
	}
	FallBack "Diffuse"
	CustomEditor "WFAShaderGUI"
}
