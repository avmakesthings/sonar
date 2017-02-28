using UnityEditor;
using UnityEngine;
using WireframeArtist;
using System.Linq;
using UnityEngine.Rendering;

namespace WireframeArtist.MaterialGUI {
    /// 
    /// Static helper class handling shader keywords and blendmodes
    /// 
    public static class ShaderSetup {
        public static bool isDX11;
        public static bool isProjector;
        public static bool isUnlit;
        public static bool isMobile;

        static Material material;
        static Object[] materials;

        public static void Initialize(MaterialEditor materialEditor) {
            materials = materialEditor.targets;
            material = (Material)materialEditor.target;
            DetectShaderType(material.shader);
        }

        public static void Initialize(Material material, Shader newShader) {
            materials = new[] { material };
            ShaderSetup.material = material;
            DetectShaderType(newShader);
        }

        static void DetectShaderType(Shader shader) {
            isProjector = shader.name.Contains("Projector");
            isDX11 = shader.name.Contains("DirectX11");
            isUnlit = shader.name.Contains("Unlit");
            isMobile = shader.name.Contains("Mobile");
        }

        public static void MaterialChanged() {
            SetBlendMode();
            SetWireMode();
            SetChannel();
            SetKeywords();
        }

        public static void SetKeywords() {
            foreach (Material mat in materials) {
                if (Mathf.Approximately(material.GetFloat("_AASmooth"), 0f))
                    material.DisableKeyword("_AA_ON");
                else material.EnableKeyword("_AA_ON");

                if (isProjector || isUnlit || isMobile) continue;

                var shouldEmissionBeEnabled = !Mathf.Approximately(material.GetFloat("_WEmission"), 0f)
                    || !Mathf.Approximately(material.GetFloat("_GEmission"), 0f)
                    || WFAShaderGUI.ShouldEmissionBeEnabled(material, material.GetColor("_EmissionColor"));
                SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

                SetKeyword(mat, "_NORMALMAP", material.GetTexture("_BumpMap"));
                SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));

                // Setup lightmap emissive flags
                MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
                if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0) {
                    flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                    if (!shouldEmissionBeEnabled)
                        flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                    material.globalIlluminationFlags = flags;
                }
            }
        }

        static void SetKeyword(Material m, string keyword, bool state) {
            if (state) m.EnableKeyword(keyword);
            else m.DisableKeyword(keyword);
        }

        public static void SetChannel() {
            foreach (Material mat in materials) {
                int mode = mat.GetInt("_Channel");
                if(mode == (int)Channel.UV3) {
                    mat.EnableKeyword("_CHANNEL_UV3");
                    mat.DisableKeyword("_CHANNEL_COLOR");
                } else if(mode == (int)Channel.Color) {
                    mat.DisableKeyword("_CHANNEL_UV3");
                    mat.EnableKeyword("_CHANNEL_COLOR");
                }
            }
        }

        #region Wire Mode

        public static void SetWireMode() {
            foreach (Material mat in materials) {
                int mode = mat.GetInt("_WMode");
                WireSetup.Setup(mat, mode);
            }
        }

        static class WireSetup {
            static readonly string[] keywordsDX11 = new[] { "_MODE_DEFAULT", "_MODE_SCREEN", "_MODE_WORLD", "_MODE_BARY" };
            static readonly string[] keywordsStd = new[] { "_MODE_WORLD", "_MODE_SCREEN", "_MODE_WORLD", "_MODE_BARY" };

            public static void Setup(Material mat, int mode) {
                string[] keywords = isDX11 ? keywordsDX11 : keywordsStd;
                var keyword = keywords[mode];
                foreach (var kw in keywords) {
                    if (kw == keyword) mat.EnableKeyword(kw);
                    else mat.DisableKeyword(kw);
                }

            }
        }

        #endregion

        #region Blend Mode

        public static void SetBlendMode() {
            foreach (Material mat in materials) {
                int mode = mat.GetInt("_Mode");
                blendCfgs[mode].Setup(mat);
            }
        }

        public static WFABlendMode GetBlendMode() {
            var bmode = Prop._Mode._int;
            var type = isDX11 ? typeof(WFABlendMode) : typeof(WFABlendModeStd);
            if (!System.Enum.IsDefined(type, bmode)) {
                Prop._Mode._int = 0; // Assumes 0 is always defined
                return 0;
            }
            return (WFABlendMode)Prop._Mode._int;
        }

        public static void SetZWrite() {
            foreach (Material mat in materials) {
                int mode = mat.GetInt("_Mode");
                material.SetInt("_ZWrite", blendCfgs[mode].zWrite);
            }
        }

        static BlendSetup[] blendCfgs = new BlendSetup[9] {
            new BlendSetup {mode=WFABlendMode.Opaque,              tag="",                 srcBlend=BlendMode.One,             dstBlend=BlendMode.Zero,            zWrite=1, keyword="",                       queue=RenderQueue.Geometry},
            new BlendSetup {mode=WFABlendMode.Cutout,              tag="TransparentCutout",srcBlend=BlendMode.One,             dstBlend=BlendMode.Zero,            zWrite=1, keyword="_ALPHATEST_ON",          queue=RenderQueue.AlphaTest},
            new BlendSetup {mode=WFABlendMode.Fade,                tag="Transparent",      srcBlend=BlendMode.SrcAlpha,        dstBlend=BlendMode.OneMinusSrcAlpha,zWrite=0, keyword="_ALPHABLEND_ON",         queue=RenderQueue.Transparent},
            new BlendSetup {mode=WFABlendMode.Transparent,         tag="Transparent",      srcBlend=BlendMode.One,             dstBlend=BlendMode.OneMinusSrcAlpha,zWrite=0, keyword="_ALPHAPREMULTIPLY_ON",   queue=RenderQueue.Transparent},
            new BlendSetup {mode=WFABlendMode.Additive,            tag="Transparent",      srcBlend=BlendMode.One,             dstBlend=BlendMode.One,             zWrite=0, keyword="_ALPHAPREMULTIPLY_ON",   queue=RenderQueue.Transparent},
            new BlendSetup {mode=WFABlendMode.SoftAdditive,        tag="Transparent",      srcBlend=BlendMode.OneMinusDstColor,dstBlend=BlendMode.One,             zWrite=0, keyword="_ALPHAPREMULTIPLY_ON",   queue=RenderQueue.Transparent},
            new BlendSetup {mode=WFABlendMode.Multiplicative,      tag="Transparent",      srcBlend=BlendMode.DstColor,        dstBlend=BlendMode.OneMinusSrcAlpha,zWrite=0, keyword="_ALPHAPREMULTIPLY_ON",   queue=RenderQueue.Transparent},
            new BlendSetup {mode=WFABlendMode.MultiplicativeDouble,tag="Transparent",      srcBlend=BlendMode.DstColor,        dstBlend=BlendMode.SrcColor,        zWrite=0, keyword="_ALPHAPREMULTIPLY_ON",   queue=RenderQueue.Transparent},
            new BlendSetup {mode=WFABlendMode.CutoutTwoSided,      tag="TransparentCutout",srcBlend=BlendMode.One,             dstBlend=BlendMode.Zero,            zWrite=1, keyword="_ALPHATEST_ON",          queue=RenderQueue.AlphaTest},
        };

        struct BlendSetup {
            public WFABlendMode mode; public string tag; public BlendMode srcBlend; public BlendMode dstBlend; public int zWrite; public RenderQueue queue;

            static readonly string[] keywords = new[] { "_ALPHATEST_ON", "_ALPHABLEND_ON", "_ALPHAPREMULTIPLY_ON" };
            string _keyword;
            public string keyword {
                get {return _keyword;}
                set {
                    if (!keywords.Contains(value) && value.Length > 0) throw new System.Exception("Keyword is not valid.");
                    _keyword = value;
                }
            }

            public void Setup(Material mat) {
                mat.SetOverrideTag("RenderType", tag);
                mat.SetInt("_SrcBlend", (int)srcBlend);
                mat.SetInt("_DstBlend", (int)dstBlend);
                if(mode==WFABlendMode.Opaque) mat.SetFloat("_WTransparency", 1f);
                if(isDX11) {
                    if (mode == WFABlendMode.CutoutTwoSided) {
                        material.EnableKeyword("WFA_TWOSIDED");
                    } else {
                        material.DisableKeyword("WFA_TWOSIDED");
                    }
                }
                mat.renderQueue = (int)queue;
                foreach (var kw in keywords) {
                    if (kw == keyword) {
                        mat.EnableKeyword(kw);
                    } else {
                        mat.DisableKeyword(kw);
                    }
                }
            }
        }

        #endregion
    }
}
