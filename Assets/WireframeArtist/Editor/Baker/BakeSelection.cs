using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WireframeArtist.Baker {
    ///	
    /// Updates and filters selected objects
    ///
    public static class BakeSelection {

        public static List<Object> selection = new List<Object>();

        public static int count {get{return selection.Count;}}

        public static void Update(){
        	selection.Clear();
        	UpdateAssets();
            UpdateSceneObjects();
        }

        static void UpdateAssets(){
            var assets = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);

            foreach (var asset in assets) {
                var type = asset.GetType();
        	    if (type == typeof(GameObject)) {
        	        var go = (GameObject)asset;
                    // Only add objects with mesh filters or skinned mesh renderers
                    if (go.GetComponentsInChildren<MeshFilter>(true).Length > 0
    	        		|| go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0) {
                            selection.Add(go);
                    }

        	    } else if (type == typeof(Mesh)) {
                    selection.Add(asset);
        	    }
        	}
        }

        static void UpdateSceneObjects(){
            var gameObjects = Selection.GetFiltered(typeof(GameObject), SelectionMode.ExcludePrefab);

        	// Only add objects with mesh filters or skinned mesh renderers
        	foreach (GameObject go in gameObjects) {
                if (go.GetComponentsInChildren<MeshFilter>().Length == 0
                    && go.GetComponentsInChildren<SkinnedMeshRenderer>().Length == 0) continue;
                selection.Add(go);
        	}
        }

    }
}
