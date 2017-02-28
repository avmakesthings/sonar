using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WireframeArtist;
using WireframeArtist.MaterialGUI;

/// 
/// Main shader GUI class, responsible for drawing material properties.
/// 
public class WFAShaderGUI : ShaderGUI {

    MaterialEditor materialEditor;
    Material material;
    WFABlendMode bmode;
    bool firstTimeApply;

    static WFAShaderGUI() {
        Prop.propFinder = (name, props) => { return FindProperty(name, props, false); };
    }
     
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
        this.materialEditor = materialEditor;
        material = materialEditor.target as Material;

        // Init
        Prop.Initialize(properties);
        Layout.Initialize(materialEditor);
        ShaderSetup.Initialize(materialEditor);
        if(!ShaderSetup.isProjector) bmode = ShaderSetup.GetBlendMode();

        if (firstTimeApply) {
            ShaderSetup.MaterialChanged();
            firstTimeApply = false;
        }

        EditorGUI.BeginChangeCheck(); 
        DrawGUI();
        if (EditorGUI.EndChangeCheck()) {
            ShaderSetup.MaterialChanged();
        }
    }

    void DrawGUI() {

        var renderModeDrawer = ShaderSetup.isProjector ? Prop._ModeProj
            : (ShaderSetup.isDX11 && !ShaderSetup.isUnlit ? null : Prop._ModeStd);

        if (Prop._Mode.Draw(0, renderModeDrawer)) {
            bmode = ShaderSetup.GetBlendMode();
            ShaderSetup.SetZWrite(); //only set zwrite when changing the render mode, because there is a zwrite option in the GUI
            if (bmode == WFABlendMode.CutoutTwoSided)
                Prop._Cull._int = (int)CullMode.Back;
        }

        if (bmode == WFABlendMode.Cutout || bmode == WFABlendMode.CutoutTwoSided) {
            Prop._Cutoff.Draw(indentation: 2);
        }

        EditorGUILayout.Space();

        if (Layout.BeginFold("Wireframe")) DrawWireframeGUI();
        Layout.EndFold();

        if (!ShaderSetup.isProjector) {
            if (Layout.BeginFold("Surface")) DrawSurfaceGUI();
            Layout.EndFold();
        }

        if (Layout.BeginFold("Glow")) DrawGlowGUI();
        Layout.EndFold();

        if (!ShaderSetup.isProjector && !ShaderSetup.isMobile) {
            if (Layout.BeginFold("Fade")) DrawFadeGUI();
            Layout.EndFold();
        }

        if (Layout.BeginFold("Preferences")) DrawPrefGUI();
        Layout.EndFold();
    }

    void DrawWireframeGUI() {
        GUILayout.Space(-5);
        GUILayout.Label(" Lighting", EditorStyles.boldLabel);

        if (Prop._WTex.Draw() && !ShaderSetup.isProjector) {
            Prop._WOpacity._float = Prop._WColor._color.a;
        }

        if (Prop._WTex._texture != null) {
            Prop._WUV.Draw(indentation: 2);
        }

        if (Prop._WOpacity.Draw()) {
            var c = Prop._WColor._color;
            Prop._WColor._color = new Color(c.r, c.g, c.b, Prop._WOpacity._float);
        }

        if (ShaderSetup.isProjector || bmode != WFABlendMode.Opaque)
            Prop._WTransparency.Draw();

        if (Prop._WLight.active) {
            if (!ShaderSetup.isProjector && Prop._WLight._int != (int)LightMode.Unlit) {
                Prop._WEmission.Draw();
                Prop._WMetal.Draw();
                Prop._WGloss.Draw();
            }
            Prop._WLight.Draw();
        }
        
        GUILayout.Space(5);
        GUILayout.Label(" Shape", EditorStyles.boldLabel);
        Prop._WThickness.Draw();
        Prop._WStyle.Draw();
        var wireStyle = (WireStyle)Prop._WStyle._int;
        if (wireStyle != WireStyle.Default) {
            Prop._WParam.Draw(indentation: 2);
        }
        Prop._WInvert.Draw();
        Prop._WMask.Draw();

        GUILayout.Space(5);
        GUILayout.Label(" Rendering", EditorStyles.boldLabel);

        if (!ShaderSetup.isDX11) Prop._Channel.Draw();

        var wireModeDrawer = wireStyle == WireStyle.Smooth ? Prop._WModeSmooth : null;
        Prop._WMode.Draw(0, wireModeDrawer);

        if(!ShaderSetup.isMobile) Prop._AASmooth.Draw();
    }

    void DrawSurfaceGUI() {
        Prop._MainTex.Draw();

        if (!ShaderSetup.isUnlit && !ShaderSetup.isMobile) {
            bool hasGlossMap = Prop._MetallicGlossMap._texture != null;
            if (hasGlossMap) Prop._Metallic.active = false;
            Prop._MetallicGlossMap.Draw();
            if (hasGlossMap || (Prop._SmoothnessTextureChannel._int == (int)SmoothnessMapChannel.AlbedoAlpha))
                Prop._GlossMapScale.Draw(indentation: 2);
            else Prop._Glossiness.Draw(indentation: 2);

            Prop._SmoothnessTextureChannel.Draw(indentation: 3);

            Prop._BumpScale.active = Prop._BumpMap._texture != null;
            Prop._BumpMap.Draw();

            Prop._OcclusionStrength.active = Prop._OcclusionMap._texture != null;
            Prop._OcclusionMap.Draw();
            DoEmissionArea(material); // Use Unity StandardShaderGUI.cs code for this
        }
        

        EditorGUI.BeginChangeCheck();
        materialEditor.TextureScaleOffsetProperty(Prop._MainTex.matProp);
        if (EditorGUI.EndChangeCheck())
            // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake
            if(Prop._EmissionMap.active)
                Prop._EmissionMap.matProp.textureScaleAndOffset = Prop._MainTex.matProp.textureScaleAndOffset; 

        EditorGUILayout.Space();

        Prop._GlossyReflections.Draw();
    }

    void DrawGlowGUI() {
        Prop._Glow.Draw();
        EditorGUI.BeginDisabledGroup(!Prop._Glow._bool);

        Prop._GColor.Draw();
        Prop._GEmission.Draw();
        Prop._GDist.Draw();
        Prop._GPower.Draw();

        EditorGUI.EndDisabledGroup();
    }

    void DrawFadeGUI() {
        Prop._Fade.Draw();
        EditorGUI.BeginDisabledGroup(!Prop._Fade._bool);

        Prop._FMode.Draw();
        Prop._FDist.Draw();
        Prop._FPow.Draw();

        EditorGUI.EndDisabledGroup();
    }

    void DrawPrefGUI() {
        Prop._Limits.Draw();
        Prop._Cull.Draw();
        Prop._ZWrite.Draw();
    }

    #region Unity StandardShaderGUI.cs code with some modifications

    ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, 99f, 1 / 99f, 3f);
    static GUIContent emissiveWarning = new GUIContent("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader) {
        ShaderSetup.Initialize(material, newShader);

        if (ShaderSetup.isProjector) {
            material.SetFloat("_Mode", (float)WFABlendMode.Fade);
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            ShaderSetup.MaterialChanged();
            return;
        }

        // _Emission property is lost after assigning this shader to the material
        // thus transfer it before assigning the new shader
        if (material.HasProperty("_Emission")) {
            material.SetColor("_EmissionColor", material.GetColor("_Emission"));
        }

        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/")) {
            ShaderSetup.MaterialChanged();
            return;
        }

        var blendMode = WFABlendMode.Opaque;
        if (oldShader.name.Contains("/Transparent/Cutout/")) {
            blendMode = WFABlendMode.Cutout;
        } else if (oldShader.name.Contains("/Transparent/")) {
            // NOTE: legacy shaders did not provide physically based transparency
            // therefore Fade mode
            blendMode = WFABlendMode.Fade;
        }
        material.SetFloat("_Mode", (float)blendMode);

        ShaderSetup.SetZWrite();
        ShaderSetup.MaterialChanged();
    }

    void DoEmissionArea(Material material) {
        bool showHelpBox = !HasValidEmissiveKeyword(material);

        bool hadEmissionTexture = Prop._EmissionMap._texture != null;

        // Texture and HDR color controls
        materialEditor.TexturePropertyWithHDRColor(Prop._EmissionMap.label, Prop._EmissionMap.matProp, 
            Prop._EmissionColor.matProp, m_ColorPickerHDRConfig, false);

        // If texture was assigned and color was black set color to white
        float brightness = Prop._EmissionColor._color.maxColorComponent;
        if (Prop._EmissionMap._texture != null && !hadEmissionTexture && brightness <= 0f)
            Prop._EmissionColor._color = Color.white;

        // Emission for GI?
        materialEditor.LightmapEmissionProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);

        if (showHelpBox) {
            EditorGUILayout.HelpBox(emissiveWarning.text, MessageType.Warning);
        }
    }

    bool HasValidEmissiveKeyword(Material material) {
        // Material animation might be out of sync with the material keyword.
        // So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
        // (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
        bool hasEmissionKeyword = material.IsKeywordEnabled("_EMISSION");
        if (!hasEmissionKeyword && ShouldEmissionBeEnabled(material, Prop._EmissionColor._color))
            return false;
        else
            return true;
    }

    public static bool ShouldEmissionBeEnabled(Material mat, Color color) {
        var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
        return color.maxColorComponent > 0.1f / 255.0f || realtimeEmission;
    }

    #endregion

}