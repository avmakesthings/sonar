Shader "Wireframe Artist/Mobile" {
	Properties {
		// Wire
		_WTex("Wire Texture", 2D) = "white" {}
		[KeywordEnum(UV0, Barycentric)] _WUV("Wire UV Set", Float) = 0.0
		_WColor("Wire Color", Color) = (0,0,1,1)
		_WTransparency("Wire Transparency", Range(0.0,1.0)) = 1.0
		_WOpacity("Wire Opacity", Range(0.0,1.0)) = 1.0
		_WThickness("Wire Thickness", Float) = 0.05
		_WParam("Wire Parameter", Float) = 0
		_WMode("Wire Mode", Float) = 0.0
		[KeywordEnum(Default, Smooth)] _WStyle("Wire Style", Float) = 0.0 
		_WMask("Wire Mask", 2D) = "white" {}
		_AASmooth("AA Smoothing", Float) = 1.5

		// Glow
		[Toggle] _Glow("Glow Enable", Float) = 0.0
		_GColor("Glow Color", Color) = (0,0,0,1)
		_GDist("Glow Distance", Float) = 0.35
		_GPower("Glow Power", Float) = 0.5

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
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	CGINCLUDE
		#define WFA_UNLIT
		#define _WLIGHT_UNLIT
	ENDCG
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 150
		Cull [_Cull]

		Pass{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }
			ColorMask RGB
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _GLOW_ON
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _ _ALPHATEST_ON

			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase

			#pragma vertex vert_surf
			#pragma fragment frag_surf

			#include "WFAMobileForward.cginc"

			ENDCG
		}

		Pass{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _GLOW_ON
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _ _ALPHATEST_ON

			#pragma multi_compile_shadowcaster
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2

			#pragma vertex vert_surf
			#pragma fragment frag_surf

			#include "WFAMobileShadow.cginc"

			ENDCG
		}

		Pass{
			Name "Meta"
			Tags{ "LightMode" = "Meta" }
			Cull Off

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _ _ALPHATEST_ON

			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#pragma skip_variants INSTANCING_ON

			#pragma vertex vert_surf
			#pragma fragment frag_surf

			#include "WFAMobileMeta.cginc"

			ENDCG
		}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull [_Cull]

		Pass{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }
			ColorMask RGB
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _ _ALPHATEST_ON

			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase

			#pragma vertex vert_surf
			#pragma fragment frag_surf

			#include "WFAMobileForward.cginc"

			ENDCG
		}

		Pass{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _ _ALPHATEST_ON

			#pragma multi_compile_shadowcaster
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2

			#pragma vertex vert_surf
			#pragma fragment frag_surf

			#include "WFAMobileShadow.cginc"

			ENDCG
		}

		Pass{
			Name "Meta"
			Tags{ "LightMode" = "Meta" }
			Cull Off

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _GLOW_ON
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma shader_feature _ _ALPHATEST_ON

			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#pragma skip_variants INSTANCING_ON

			#pragma vertex vert_surf
			#pragma fragment frag_surf

			#include "WFAMobileMeta.cginc"

			ENDCG
		}
	}
	Fallback "Mobile/VertexLit"
	CustomEditor "WFAShaderGUI"
}
