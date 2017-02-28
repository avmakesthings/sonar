using UnityEngine;

namespace WireframeArtist {
    public static partial class WireframeBaker {
        static Mesh ConvertDist(Mesh mesh, bool isWorldSpace, float scale=1f) {
            var ids = mesh.triangles;
            #if UNITY_EDITOR
                if (ids.Length >= 65534) throw new TooMuchVerticesException(mesh);
            #endif
            var verts = mesh.vertices;
            var newVerts = new Vector3[ids.Length];
            var newIds = new int[ids.Length];
            var dists = new float[ids.Length];
            var distance = isWorldSpace ? GetWorldSpaceDistance(scale) : minNorm;

            for (int i = 0; i < ids.Length; i += 3) {
                var v0 = verts[ids[i]];
                var v1 = verts[ids[i + 1]];
                var v2 = verts[ids[i + 2]];
                newVerts[i] = v0;
                newVerts[i + 1] = v1;
                newVerts[i + 2] = v2;
                newIds[i] = i;
                newIds[i + 1] = i + 1;
                newIds[i + 2] = i + 2;

                var distTri = distance(v0, v1, v2);
                dists[i] = distTri.x;
                dists[i + 1] = distTri.y;
                dists[i + 2] = distTri.z;
            }
            mesh.vertices = newVerts;
            SetTriangles(newIds, mesh);

            mesh.normals = SplitArray(mesh.normals, ids);
            mesh.boneWeights = SplitArray(mesh.boneWeights, ids);
            mesh.tangents = SplitArray(mesh.tangents, ids);
            mesh.uv = SplitArray(mesh.uv, ids);
            mesh.uv2 = SplitArray(mesh.uv2, ids);
            mesh.uv3 = SplitArray(mesh.uv3, ids);

            if (channel == Channel.Color) {
                var colors = new Color32[ids.Length];
                byte exponent, mantissa;

                for (int i = 0; i < ids.Length; i += 3) {
                    Encode2Byte(dists[i + 1], out mantissa, out exponent);
                    colors[i] = new Color32(0, mantissa, 0, exponent);
                    Encode2Byte(dists[i + 2], out mantissa, out exponent);
                    colors[i + 1] = new Color32(0, 0, mantissa, exponent);
                    Encode2Byte(dists[i], out mantissa, out exponent);
                    colors[i + 2] = new Color32(mantissa, 0, 0, exponent);
                }
                mesh.colors32 = colors;
            } else {
                mesh.colors32 = SplitArray(mesh.colors32, ids);
            }

            if (channel ==  Channel.UV3) {
                var uvs = new Vector2[ids.Length];
                for (int i = 0; i < ids.Length; i += 3) {
                    uvs[i] = new Vector2(0, dists[i + 1]);
                    var d = dists[i + 2];
                    uvs[i + 1] = new Vector2(d, d);
                    uvs[i + 2] = new Vector2(dists[i], 0);
                }
                mesh.uv4 = uvs;
            } else {
                mesh.uv4 = SplitArray(mesh.uv4, ids);
            }
        
            return mesh;
        }

        public static void Encode2Byte(float value, out byte mantissa, out byte exponent) {
            int exp = Mathf.FloorToInt(Mathf.Log(value, 2));
            float mant = Mathf.Pow(2f, -exp) * value;
            exponent = (byte)(exp + 128);
            mantissa = (byte)(Mathf.Clamp(255f * mant - 254f, 1f, 255f));
        }

        public static T[] SplitArray<T>(T[] array, int[] ids) {
            if (array.Length == 0) return null;
            var newArr = new T[ids.Length];
            for (int i = 0; i < ids.Length; i++) {
                newArr[i] = array[ids[i]];
            }
            return newArr;
        }

        public delegate Vector3 Distance(Vector3 v0, Vector3 v1, Vector3 v2);

        public static Distance GetWorldSpaceDistance(float scale) {
            return (v0, v1, v2) => {
                v0 *= scale;v1 *= scale;v2 *= scale;
                return worldSpace(v0, v1, v2); 
            };
        }

        public static Distance worldSpace = (v0, v1, v2) => {
            return new Vector3(DistToLine(v2, v0, v1),
                            DistToLine(v0, v1, v2),
                            DistToLine(v1, v0, v2));
        };

        public static Distance minNorm = (v0, v1, v2) => {
            var d = worldSpace(v0, v1, v2);
            var m = Mathf.Min(d.x, Mathf.Min(d.y, d.z));
            return m > 1e-5f ? d / m : Vector3.zero;
        };

        static float DistToLine(Vector3 v0, Vector3 v1, Vector3 v2) {
            return Vector3.Cross(v0 - v1, v0 - v2).magnitude / (v2 - v1).magnitude;
        }
    }
}