using UnityEngine;

namespace WireframeArtist{
    /// 
    /// Source code for baking a wireframe into a mesh. Three different modes:
    /// WireframeBaker.Bary, WireframeBaker.Dist, WireframeBaker.Thresh
    ///
	public static partial class WireframeBaker {

        static Channel channel;
        static float thresholdAngle;

		public static Mesh Convert(Mesh mesh, BakingMode bakingMode, Channel channel, 
			float thresholdAngle=0, float sceneScale=1, float importScale=1){

            WireframeBaker.channel = channel;
            WireframeBaker.thresholdAngle = thresholdAngle * Mathf.Deg2Rad;

            #if UNITY_EDITOR
	            CheckTopology(mesh);
            #endif

            // Threshold
            if (bakingMode == BakingMode.AngleThreshold) {
			    return ConvertThresh(mesh);
			}

			// Barycentric 
			if (bakingMode == BakingMode.Barycentric || bakingMode == BakingMode.ScreenSpace) {
			    return ConvertBary(mesh);
			}

			// Distance
			if (bakingMode == BakingMode.WorldSpace) {
			    return ConvertDist(mesh, isWorldSpace: true, 
			    	scale: sceneScale / importScale);
			} else {
			    return ConvertDist(mesh, isWorldSpace: false);
			}
		}

		#if UNITY_EDITOR
		static void CheckTopology(Mesh mesh){
            for (int i = 0; i < mesh.subMeshCount; i++) {
                if(mesh.GetTopology(i) != MeshTopology.Triangles) {
                    throw new MissingException("The mesh "+ mesh.name+" doesn't have a triangle mesh topology.");
                }
            }
		}
		#endif

		// Set triangles with submesh support
		static void SetTriangles(int[] tris, Mesh mesh) {
		    int ofs = 0;
		    for (int i = 0; i < mesh.subMeshCount; i++) {
		        var subTris = mesh.GetTriangles(i);
		        System.Array.Copy(tris, ofs, subTris, 0, subTris.Length);
		        mesh.SetTriangles(subTris, i);
		        ofs += subTris.Length;
		    }
		}

	}

}