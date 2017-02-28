using UnityEditor;
using UnityEngine;
using WireframeArtist.MaterialGUI;

namespace WireframeArtist.Baker {
    /// 
    /// Responsible for creating, storing and updating baked assets and scene objects
    ///
    public static class BakeAssetManager{

        static Shader defaultShader;
        const string defaultShaderStr = "Wireframe Artist/Standard";

        const string rootFolderName = "Baked Assets";
    	const string rootFolder = "Assets/" + rootFolderName;
    	const string suffix = "(WFA)";

    	static string srcPath;
    	static string dstFolder;
		static string dstPath;
        static bool applyMaterial;
        static PrefabType prefabType;
        static Object curObj;

        public static void Bake(){
            // Initialize
            applyMaterial = BakeWindow.applyMaterial;
            if(applyMaterial && defaultShader==null)
                defaultShader = Shader.Find(defaultShaderStr);

            try {
                StartBakingProcess();
            } catch (WFAException) {
                CleanUp(); // cleanup but don't throw
            } catch (System.Exception) {
            	CleanUp(); throw;
            } finally {
                EditorUtility.ClearProgressBar();
            }
    	}

    	// 4 types of objects supported: Meshes, Prefabs, asset GameObjects and scene GameObjects
    	static void StartBakingProcess(){
    		ProgressBar(.01f, "Starting the baking process.");

    		// Create root folder
    		CreateFolder("Assets", rootFolderName);

    		for (int i = 0; i < BakeSelection.count; i++) {
            	curObj = BakeSelection.selection[i];

                ProgressBar((i + 1) / (float)BakeSelection.count, "Baking " + curObj.name);

                var type = curObj.GetType();
                prefabType = PrefabUtility.GetPrefabType(curObj);
                if (type == typeof(Mesh)){
            		BakeMesh((Mesh)curObj);
        		}else if(prefabType == PrefabType.Prefab){
        			BakePrefab((GameObject)curObj);
            	}else if(type == typeof(GameObject)) {
            		if(prefabType == PrefabType.ModelPrefab)
                        BakeAsset((GameObject)curObj);
            		else BakeGameObject((GameObject)curObj);
        		}
                dstFolder = ""; // Prevent cleaning up the previous folder
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

    	}

        static void BakeAsset(GameObject asset) {
            SetupPaths(asset, extension: ".prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dstPath);
            BakeGameObject(asset, prefab: prefab);
        }

        static GameObject BakeGameObject(GameObject go, GameObject prefab=null) {
            bool hasSource = true;
            var prefabParent = prefab==null ? PrefabUtility.GetPrefabParent(go) : prefab;
            if (prefabParent == null || PrefabUtility.GetPrefabType(prefabParent) != PrefabType.Prefab) {
                // scene object without source file -> make source asset
                hasSource = false;
                var originalName = go.name;
                dstFolder = CreateFolder(rootFolder, go.name);
                dstPath = dstFolder + "/" + Suffix(go.name) + ".prefab";
                if (prefabType == PrefabType.ModelPrefab) {
                    prefabParent = PrefabUtility.CreatePrefab(dstPath, go);
                    go = (GameObject)prefabParent;
                }
                go.name = originalName;
            } else {
                SetupPaths(prefabParent);
                if (prefabType != PrefabType.Prefab)
                    go = (GameObject)prefabParent;
            }

            var mfs = go.GetComponentsInChildren<MeshFilter>(true);
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var mf in mfs) {
                mf.sharedMesh = BakeSceneMesh(mf.sharedMesh);
            }
            foreach (var smr in smrs) {
                smr.sharedMesh = BakeSceneMesh(smr.sharedMesh);
                if (!applyMaterial) continue;
                smr.sharedMaterials = ApplyMaterials(smr.sharedMaterials);
            }

            if (!hasSource && prefabType != PrefabType.ModelPrefab) {
                prefabParent = PrefabUtility.CreatePrefab(dstPath, go);
                go = PrefabUtility.ConnectGameObjectToPrefab(go, (GameObject)prefabParent);
            }

            if (!applyMaterial) return go;

            var mrs = go.GetComponentsInChildren<MeshRenderer>(true);

            foreach (var mr in mrs) {
                mr.sharedMaterials = ApplyMaterials(mr.sharedMaterials);
            }

            return go;
        }

        static void BakeMesh(Mesh mesh) {
            SetupPaths(mesh);

            bool exists = false;
            var newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(dstPath);
            if (newMesh == null) newMesh = new Mesh();
            else exists = true;
            EditorUtility.CopySerialized(mesh, newMesh);
            mesh = newMesh;

            mesh = Convert(mesh);

            if (!exists) AssetDatabase.CreateAsset(mesh, dstPath);
        }

        static Mesh BakeSceneMesh(Mesh mesh) {
            if (mesh == null) throw new MissingException("The mesh is missing.");
            var meshFolder = CreateFolder(dstFolder, "Meshes");
            var meshDstPath = meshFolder + "/" + Suffix(mesh.name) + ".asset";

            bool exists = false;
            var newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshDstPath);
            if (newMesh == null) newMesh = new Mesh();
            else exists = true;
            EditorUtility.CopySerialized(mesh, newMesh);

            mesh = newMesh;
            mesh = Convert(mesh);

            if (!exists) AssetDatabase.CreateAsset(mesh, meshDstPath);

            return mesh;
        }

        static void BakePrefab(GameObject prefab) {
            SetupPaths(prefab, extension:".prefab");
            bool exists = false;
            var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dstPath);

            try {
                if (newPrefab == null) {
                    newPrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                } else {
                    exists = true;
                    newPrefab = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);
                    CopyAttributes(prefab, newPrefab); // Revert to original without losing the links
                }

                newPrefab.name = prefab.name;
                newPrefab = BakeGameObject(newPrefab);

                if (!exists) {
                    prefab = PrefabUtility.CreatePrefab(dstPath, newPrefab);
                    newPrefab = PrefabUtility.ConnectGameObjectToPrefab(newPrefab, prefab);
                }
            } catch (System.Exception) {
                throw;
            } finally {
                if(newPrefab!=null) Object.DestroyImmediate(newPrefab); // Prevent ghost objects
            }
        }

        static Material[] ApplyMaterials(Material[] materials) {
            if (materials == null || materials.Length == 0) throw new MissingException("Materials are missing.");
            for (int i = 0; i < materials.Length; i++) {
                var mat = materials[i];
                if (mat == null) throw new MissingException("Materials are missing.");

                Material newMat;
                if (AssetDatabase.GetAssetPath(mat) == "Resources/unity_builtin_extra") {
                    var matFolder = CreateFolder(dstFolder, "Materials");
                    var matDstPath = matFolder + "/" + Suffix(mat.name) + ".mat";
                    newMat = AssetDatabase.LoadAssetAtPath<Material>(matDstPath);
                    if(newMat == null) {
                        newMat = new Material(defaultShader);
                        AssetDatabase.CreateAsset(newMat, matDstPath);
                        newMat = AssetDatabase.LoadAssetAtPath<Material>(matDstPath);
                    }
                } else {
                    var matDstPath = SetupPaths(mat, isMaterial: true);
                    newMat = CopyAssetUnlessExists<Material>(srcPath, matDstPath);
                    newMat.shader = defaultShader;
                }

                var wireMode = BakeWindow.bakingMode;
                if(wireMode == BakingMode.AngleThreshold) {
                    wireMode = BakingMode.Barycentric;
                }
                newMat.SetInt("_WMode", (int)wireMode);
                newMat.SetFloat("_Channel", (int)BakeWindow.channel);
                ShaderSetup.Initialize(newMat, newMat.shader);
                ShaderSetup.MaterialChanged();

                materials[i] = newMat;
            }
            return materials;
        }

        static Mesh Convert(Mesh mesh){
            return WireframeBaker.Convert(mesh, BakeWindow.bakingMode, BakeWindow.channel, 
                BakeWindow.thresholdAngle, BakeWindow.sceneScale, BakeWindow.importScale);
        }

        static void CleanUp(){
            if(curObj.GetType() == typeof(GameObject) && prefabType != PrefabType.Prefab && prefabType != PrefabType.ModelPrefab){
                // scene object
                return; //don't delete folder
            }
    		if (AssetDatabase.IsValidFolder(dstFolder)) AssetDatabase.DeleteAsset(dstFolder);
    	}

    	static T CopyAssetUnlessExists<T>(string src, string dst) where T:Object{
    		var existingAsset = AssetDatabase.LoadAssetAtPath<T>(dst);
    		if(existingAsset != null) return existingAsset;
            return CopyAsset<T>(src, dst);
    	}

    	static T CopyAsset<T>(string src, string dst) where T:Object{
            AssetDatabase.CopyAsset(src, dst);
            T asset = AssetDatabase.LoadAssetAtPath<T>(dst);
            asset.name = Suffix(asset.name);
            return asset;
    	}

    	static string SetupPaths(Object obj, bool isMaterial=false, string extension=null){
    		srcPath = AssetDatabase.GetAssetPath(obj);
            if (srcPath.Length == 0) { // no source asset
                throw new MissingException("Can't find source asset file.");
            }

    		string[] split = srcPath.Split('/');
    		var baseName = split[split.Length - 1];

    		split = baseName.Split('.');
    		var objName = split[0];
            if (objName.EndsWith(suffix)) objName = objName.Replace(suffix, "");
    		var objExtension = (extension == null) ? "." + split[1] : extension;
            if (isMaterial) {
                var matFolder = CreateFolder(dstFolder, "Materials");
                return matFolder + "/" + Suffix(objName) + objExtension;
            }
            dstFolder = CreateFolder(rootFolder, objName);
            dstPath = dstFolder + "/" + Suffix(objName)  + objExtension;
            return dstPath;
        }

        static void CopyAttributes(GameObject src, GameObject dst) {
            var mfs = dst.GetComponentsInChildren<MeshFilter>(true);
            var smrs = dst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var mrs = dst.GetComponentsInChildren<MeshRenderer>(true);
            CopyAttributes(src, mfs, smrs, mrs);
        }

        static void CopyAttributes(GameObject src, MeshFilter[] mfs, SkinnedMeshRenderer[] smrs, MeshRenderer[] mrs) {
    	    var mfsSrc = src.GetComponentsInChildren<MeshFilter>(true);
    	    var smrsSrc = src.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    	    var mrsSrc = src.GetComponentsInChildren<MeshRenderer>(true);
    	    if (mfsSrc.Length != mfs.Length || smrsSrc.Length != smrs.Length || mrsSrc.Length != mrs.Length) {
                // this happens when the names are identical but not the data
                return;
            }
            for (int i = 0; i < mfs.Length; i++) {
                CopyAttributes(mfsSrc[i].sharedMesh, mfs[i].sharedMesh);
    	    }
    	    for (int i = 0; i < smrs.Length; i++) {
                CopyAttributes(smrsSrc[i].sharedMesh, smrs[i].sharedMesh);
    	        smrs[i].sharedMaterials = smrsSrc[i].sharedMaterials;
    	    }
    	    for (int i = 0; i < mrsSrc.Length; i++) {
    	        mrs[i].sharedMaterials = mrsSrc[i].sharedMaterials;
    	    }
    	}

    	static void CopyAttributes(Mesh srcMesh, Mesh dstMesh) {
            EditorUtility.CopySerialized(srcMesh, dstMesh);
    	}

        static string Suffix(string name) {
            return name.EndsWith(suffix) ? name : name + suffix;
        }

    	// Create folder if the folder doesn't exist
    	static string CreateFolder(string parent, string name){
    		if (!AssetDatabase.IsValidFolder(parent+"/"+name))
        		AssetDatabase.CreateFolder(parent, name);
        	return parent+"/"+name;
    	}

    	static void ProgressBar(float progress, string msg = "Baking...") {
    	    var cancelled = EditorUtility.DisplayCancelableProgressBar(
    	                "Baking Wireframe Data", msg, progress);
    	    if (cancelled) {
    	    	EditorUtility.ClearProgressBar();
    	    	throw new CancelledProgressException();
    	    }
    	}

    }
}