Shader "Wireframe Artist/Projector" {
	Properties {
		// Wire
		_WTex("Wire Texture", 2D) = "white" {}
		[KeywordEnum(UV0, Barycentric)] _WUV("Wire UV Set", Float) = 0.0
		_WColor("Wire Color", Color) = (0,0,1,1)
		_WTransparency("Wire Transparency", Range(0.0,1.0)) = 1.0
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
		[HideInInspector] _Fold("__fld", Float) = 1.0
		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _Limits("__lmt", Float) = 1.0
	}
	CGINCLUDE
		#define WFA_PROJECTOR
	ENDCG
	SubShader {
		Tags{ "Queue" = "Transparent"}
		LOD 300

		Pass{
			ZWrite Off
			Offset -1, -1
			Blend [_SrcBlend] [_DstBlend]

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _GLOW_ON
			#pragma shader_feature _AA_ON
			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR 
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma multi_compile_fog

			#pragma vertex vert
			#pragma fragment frag

			#include "WFAProjector.cginc"

			ENDCG
		}
	}
	SubShader {
		Tags{ "Queue" = "Transparent"}
		LOD 100

		Pass{
			ZWrite Off
			Offset -1, -1
			Blend [_SrcBlend] [_DstBlend]

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _WUV_UV0 _WUV_BARYCENTRIC
			#pragma shader_feature _CHANNEL_UV3 _CHANNEL_COLOR 
			#pragma shader_feature _ _MODE_SCREEN _MODE_WORLD
			#pragma shader_feature _WSTYLE_DEFAULT _WSTYLE_SMOOTH

			#pragma multi_compile_fog

			#pragma vertex vert
			#pragma fragment frag

			#include "WFAProjector.cginc"

			ENDCG
		}
	}
	CustomEditor "WFAShaderGUI"
}
