using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WireframeArtist;

namespace WireframeArtist.MaterialGUI {
    /// 
    /// Static helper class handling property settings like layout, label and tooltip.
    /// 
    public static class Prop {
        public static Property _Mode = new Property(Layout.EnumField(typeof(WFABlendMode)),
            "Rendering Mode", "This allows you to choose whether the object uses transparency, and if so, which type of blending mode to use.");

        public static DrawField _ModeStd = Layout.EnumField(typeof(WFABlendModeStd));
        public static DrawField _ModeProj = Layout.EnumField(typeof(WFABlendModeProj));

        public static Property _Cutoff = new Property(Layout.slider,
            "Alpha Cutoff", "Threshold for alpha cutoff.");

        // For projectors
        public static Property _PMode = new Property(Layout.EnumField(typeof(ProjectorMode)), _Mode.label.text, _Mode.label.tooltip);

        #region Wireframe

        public static Property _WColor = new Property(Layout.colorField, "Color", "Wire color.");
        public static Property _WTex = new Property(Layout.TextureExtraField(_WColor),
            "Albedo", "Wire texture and color.");

        public static Property _WUV = new Property(Layout.defaultUI,//Layout.EnumField(typeof(WireTextureUV)),
            "UV Set", "Wire UV set.");

        public static Property _WOpacity = new Property(Layout.defaultUI,
            "Opacity", "Wire opacity.");

        public static Property _WTransparency = new Property(Layout.defaultUI, 
            "Transparency", "Wire transparency.");

        public static Property _WEmission = new Property(Layout.Slider(0,2), 
            "Emission", "Wire emission.");

        public static Property _WThickness = new Property(Layout.Slider(.33f), 
            "Thickness", "Wire thickness.");

        public static Property _WGloss = new Property(Layout.defaultUI, 
            "Smoothness", "Wire smoothness.");

        public static Property _WMetal = new Property(Layout.defaultUI, 
            "Metallic", "Wire metallic property.");

        public static Property _WParam = new Property(Layout.Slider(.4f), 
            "Smoothing", "Tunable style parameter.");

        public static Property _WMode = new Property(Layout.EnumField(typeof(WireMode)), 
            "Distance Scale", "Wire distance metric.");

        public static DrawField _WModeSmooth = Layout.EnumField(typeof(WireModeSmooth));

        public static Property _WStyle = new Property(Layout.defaultUI, 
            "Wire Style", "Wire style.");

        public static Property _WMask = new Property(Layout.textureField,
            "Mask", "Mask (G). Hide parts of the wireframe.");

        public static Property _WInvert = new Property(Layout.toggle,
            "Invert Wireframe", "Wireframe gets surface properties and surface gets wireframe properties.");

        public static Property _WLight = new Property(Layout.defaultUI, 
            "Lighting Mode", "Alters the lighting for the wireframe.");

        public static Property _Channel = new Property(Layout.defaultUI, 
            "Data Channel", "Channel containing baked barycentric coordinates.");

        public static Property _AASmooth = new Property(Layout.Slider(4), 
            "Anti-aliasing", "Anti-alias smoothing. Set to 0 to turn off AA completely.");

        #endregion

        #region Surface

        public static Property _Color = new Property(Layout.colorField, "Surface Color", "");
        public static Property _MainTex = new Property(Layout.TextureExtraField(_Color),
            "Albedo", "Albedo (RGB) and Transparency (A).");

        public static Property _Metallic = new Property(Layout.defaultUI,"Metallic", "");
        public static Property _MetallicGlossMap = new Property(Layout.TextureExtraField(_Metallic),
            "Metallic", "Metallic (R) and Smoothness (A).");

        public static Property _Glossiness = new Property(Layout.defaultUI,
            "Smoothness", "Smoothness value.");

        public static Property _GlossMapScale = new Property(Layout.defaultUI,
            "Smoothness", "Smoothness scale factor.");

        public static Property _SmoothnessTextureChannel = new Property(Layout.defaultUI,
            "Source", "Smoothness texture and channel.");

        public static Property _GlossyReflections = new Property(Layout.defaultUI,
            "Reflections", "Glossy Reflections.");

        public static Property _BumpScale = new Property(Layout.defaultUI, "Normal Scale", "");
        public static Property _BumpMap = new Property(Layout.TextureExtraField(_BumpScale),
            "Normal Map", "Normal Map.");

        public static Property _OcclusionStrength = new Property(Layout.defaultUI, "Occlusion Strength", "");
        public static Property _OcclusionMap = new Property(Layout.TextureExtraField(_OcclusionStrength),
            "Occlusion", "Occlusion (G).");

        public static Property _EmissionColor = new Property(Layout.defaultUI, "Emission", "");
        public static Property _EmissionMap = new Property(Layout.defaultUI, 
            "Emission", "Emission (RGB)");

        #endregion

        #region Glow

        public static Property _Glow = new Property(Layout.defaultUI,
            "Enable", "Enable or disable glow.");

        public static Property _GColor = new Property(Layout.colorField, 
            "Color", "Glow color.");

        public static Property _GEmission = new Property(Layout.Slider(0, 2), 
            "Emission", "Glow emission.");

        public static Property _GDist = new Property(Layout.slider, 
            "Distance", "Glow distance from the edge.");

        public static Property _GPower = new Property(Layout.slider, 
            "Power", "Glow power.");

        #endregion

        #region Fade

        public static Property _Fade= new Property(Layout.defaultUI,
            "Enable", "Enable or disable fading.");

        public static Property _FMode = new Property(Layout.EnumField(typeof(FadeMode)), 
            "Fade To", "Fade the wireframe into __, if far away.");

        public static Property _FColor = new Property(Layout.defaultUI, 
            "Color", "Fade the wireframe into this color.");

        public static Property _FDist = new Property(Layout.Slider(.2f), 
            "Distance", "Fade distance from camera.");

        public static Property _FPow = new Property(Layout.Slider(0, 10), 
            "Transition Speed", "Fade transition speed.");

        #endregion

        #region Prefs

        public static Property _ZWrite = new Property(Layout.toggle,
            "ZWrite", "Controls whether pixels from this material are written to the depth buffer. This option resets when changing the render mode.");

        public static Property _Limits = new Property(Layout.toggle,
            "Use Sliders Limits", "Enable or disable property sliders where appropriate. This enables/disables the range limits for some float values.");

        public static Property _Cull = new Property(Layout.EnumField(typeof(CullMode)), 
            "Cull Mode", "Backface culling mode.");

        #endregion

        public static List<Property> allProperties = new List<Property>();
        public static List<string> propNames = new List<string>();

        static Prop() {
            foreach (var field in typeof(Prop).GetFields()) {
                if (field.FieldType != typeof(Property)) continue;
                var value = field.GetValue(null);
                allProperties.Add((Property)value);
                propNames.Add(field.Name);
            }
        }

        public delegate MaterialProperty PropertyFinder(string name, MaterialProperty[] properties);
        public static PropertyFinder propFinder;

        public static void Initialize(MaterialProperty[] properties) {
            // Find all properties
            for (int i = 0; i < allProperties.Count; i++) {
                allProperties[i].Set(propFinder(propNames[i], properties));
            }
        }
    }

    public class Property{
        public MaterialProperty matProp;
        public GUIContent label;
        public DrawField draw;

        public bool active { get { return matProp != null; } set { if (matProp != null && !value) matProp = null; } }

        public float _float { get { return matProp.floatValue; } set { matProp.floatValue = value; } }
        public int _int { get { return (int)_float; } set { matProp.floatValue = value; } }
        public Color _color { get { return matProp.colorValue; } set { matProp.colorValue = value; } }
        public Texture _texture { get { return matProp.textureValue; } set { matProp.textureValue = value; } }
        public bool _bool { get { return _float > .5f; } set { matProp.floatValue = value ? 1 : 0; } }

        public Property(DrawField draw, string label, string tooltip) {
            this.draw = draw;
            this.label = new GUIContent(label, tooltip);
        }

        public bool Draw(int indentation = 0, DrawField altDraw=null) {
            if (!active) return false;
            if (indentation > 0) EditorGUI.indentLevel += indentation;
            var changed = altDraw==null ? draw(this) : altDraw(this);
            if (indentation > 0) EditorGUI.indentLevel -= indentation;
            return changed;
        }

        public void Set(MaterialProperty prop) {
            matProp = prop;
        }

    }
}

