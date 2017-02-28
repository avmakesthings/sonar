using UnityEditor;
using UnityEngine;
using WireframeArtist;

namespace WireframeArtist.Baker {
    ///	
    /// BakerWindow properties. 
    /// Persistant settings, labels, tooltips and custom GUI styles
    ///
    public partial class BakeWindow {

        public const string windowTitle = "Wireframe Artist Baker";
        static readonly Vector2 windowSize = new Vector2(300f, 300f);

        const string _objsToBake = "Objects to bake: ";
        const string _noObjects = "There are no valid objects selected. Please select objects in the Hierarchy or Project window.";

        public static float thresholdAngle {
            get { return EditorPrefs.GetFloat("thresholdAngleWFA", 0f); }
            set { EditorPrefs.SetFloat("thresholdAngleWFA", value); }
        }
        static GUIContent _thresholdAngle = new GUIContent(
            "Threshold Angle", "(Degrees) Displays the wireframe only for edges sharper than the threshold angle.");

        public static float sceneScale {
            get { return EditorPrefs.GetFloat("sceneScaleWFA", 1f); }
            set { EditorPrefs.SetFloat("sceneScaleWFA", value); }
        }
        static GUIContent _sceneScale = new GUIContent(
            "Scene Scale", "How much are the models scaled in the scene.");

        public static float importScale {
            get { return EditorPrefs.GetFloat("importScaleWFA", 1f); }
            set { EditorPrefs.SetFloat("importScaleWFA", value); }
        }
        static GUIContent _importScale = new GUIContent(
            "Import Scale", "How much are the models scaled compared to what is in the source files.");

        public static Channel channel {
            get { return (Channel)EditorPrefs.GetInt("channelWFA", 0); }
            set { EditorPrefs.SetInt("channelWFA", (int)value); }
        }
        static GUIContent _channel = new GUIContent(
            "Data Channel", "The channel where the wireframe data should be stored.");

        public static BakingMode bakingMode {
            get { return (BakingMode)EditorPrefs.GetInt("wireModeWFA", 0); }
            set { EditorPrefs.SetInt("wireModeWFA", (int)value); }
        }
        static GUIContent _wireMode = new GUIContent(
            "Distance Scale");

        public static bool applyMaterial {
            get { return EditorPrefs.GetBool("applyMaterialWFA", true); }
            set { EditorPrefs.SetBool("applyMaterialWFA", value); }
        }
        static GUIContent _applyMaterial = new GUIContent(
            "Apply Material", "Clone all source materials and replace with wireframe materials.");


        static readonly Vector2 buttonSize = new Vector2(150f, 25f);
        static string buttonTxt {
            get {
                return BakeSelection.count < 2 ?
                    "Bake Wireframe" :
                    "Bake " + BakeSelection.count + " Wireframes";
            }
        }
        static GUIStyle _buttonStyle;
        static GUIStyle buttonStyle {
            get {
                if (_buttonStyle != null) return _buttonStyle;
                _buttonStyle = new GUIStyle("button");
                _buttonStyle.font = EditorStyles.boldFont;
                return _buttonStyle;
            }
        }
    }
}