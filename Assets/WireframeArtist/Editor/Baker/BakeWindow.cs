using UnityEditor;
using UnityEngine;
using WireframeArtist;

namespace WireframeArtist.Baker {
    /// 
    /// Draws the GUI for the wireframe baker window 
    ///
    public partial class BakeWindow : EditorWindow{

        Vector2 scrollPosition;

        [MenuItem("Tools/Wireframe Artist Baker %w")]
        public static void ShowBakeWindow(){
            var window = GetWindow<BakeWindow>(true, windowTitle, true);
            window.maxSize = windowSize;
            window.minSize = windowSize;
        }

        void OnEnable() {
            BakeSelection.Update();
        }

        void OnSelectionChange(){
            BakeSelection.Update();
            Repaint();
        }

        void OnGUI() {
            // Add some padding to make the window look nice
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);
            DrawGUI();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
        }

        void DrawGUI(){
            channel = (Channel)EditorGUILayout.EnumPopup(_channel, channel);
            bakingMode = (BakingMode)EditorGUILayout.EnumPopup(_wireMode, bakingMode);
            if (bakingMode == BakingMode.WorldSpace) {
                EditorGUI.indentLevel += 2;
                sceneScale = EditorGUILayout.FloatField(_sceneScale, sceneScale);
                importScale = EditorGUILayout.FloatField(_importScale, importScale);
                EditorGUI.indentLevel -= 2;
            }else if(bakingMode == BakingMode.AngleThreshold) {
                thresholdAngle = EditorGUILayout.Slider(_thresholdAngle, thresholdAngle, 0, 180);
            }

            applyMaterial = EditorGUILayout.Toggle(_applyMaterial, applyMaterial);

            GUILayout.Space(20);
            GUILayout.FlexibleSpace();

            if(BakeSelection.count == 0) {
                EditorGUILayout.HelpBox(_noObjects, MessageType.Info);
            } else {
                // Draw objects
                GUILayout.Label(_objsToBake + BakeSelection.count);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                foreach (var obj in BakeSelection.selection) {
                    if (obj != null) EditorGUILayout.LabelField(obj.name);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(10);
            GUILayout.FlexibleSpace();

            // Button
            EditorGUILayout.BeginHorizontal();{
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(BakeSelection.count == 0);{
                    if(GUILayout.Button(buttonTxt, buttonStyle, 
                        GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y))) {
                        BakeAssetManager.Bake();
                    }
                } 
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(5);
            }
            EditorGUILayout.EndHorizontal();
        }


    }
}