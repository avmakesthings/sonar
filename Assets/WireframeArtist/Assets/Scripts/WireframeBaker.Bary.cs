using System.Collections.Generic;
using UnityEngine;

namespace WireframeArtist {
    public static partial class WireframeBaker {

        static Mesh ConvertBary(Mesh mesh) {
            var idMarks = new List<int>(new int[mesh.vertexCount]);
            var toSplit = new List<int>();
            var ids = mesh.triangles;
            var vertCount = mesh.vertexCount;
            int[] marking;
            int[] tri = new int[3];

            for (int i = 0; i < ids.Length; i += 3) {
                tri[0] = ids[i];
                tri[1] = ids[i + 1];
                tri[2] = ids[i + 2];

                int case_ = idMarks[tri[0]] + idMarks[tri[1]] * 4
                            + idMarks[tri[2]] * 16;
                marking = markLUT[case_];

                for (int j = 0; j < 3; j++) {
                    int m = marking[j];
                    if (m == 0) continue;
                    if (m > 0) {
                        idMarks[tri[j]] = m;
                    } else {
                        ids[i + j] = vertCount + toSplit.Count;
                        toSplit.Add(tri[j]);
                        idMarks.Add(-m);
                    }
                }
            }

            // split some vertices if necessary
            if (toSplit.Count > 0) {
                var vertices = new List<Vector3>(mesh.vertices);
                for (int i = 0; i < toSplit.Count; i++) {
                    vertices.Add(vertices[toSplit[i]]);
                }
                #if UNITY_EDITOR
                    if (vertices.Count >= 65534) throw new TooMuchVerticesException(mesh);
                #endif
                
                mesh.SetVertices(vertices);
                SetTriangles(ids, mesh);
                mesh.normals = AddSplits(mesh.normals, toSplit, vertCount);
                mesh.boneWeights = AddSplits(mesh.boneWeights, toSplit, vertCount);
                mesh.tangents = AddSplits(mesh.tangents, toSplit, vertCount);
                mesh.uv = AddSplits(mesh.uv, toSplit, vertCount);
                mesh.uv2 = AddSplits(mesh.uv2, toSplit, vertCount);
                mesh.uv3 = AddSplits(mesh.uv3, toSplit, vertCount);
                if (channel != Channel.Color) mesh.colors32 = AddSplits(mesh.colors32, toSplit, vertCount);
                if (channel != Channel.UV3) mesh.uv4 = AddSplits(mesh.uv4, toSplit, vertCount);
            } else {
                SetTriangles(ids, mesh);
            }

            // bake into channel
            if (channel == Channel.Color) { 
                var colors = new Color32[idMarks.Count];
                for (int i = 0; i < idMarks.Count; i++) {
                    colors[i] = colorsLUT[idMarks[i]];
                }
                mesh.colors32 = colors;
            }else if(channel == Channel.UV3) {
                var uvs = new Vector2[idMarks.Count];
                for (int i = 0; i < idMarks.Count; i++) {
                    uvs[i] = uvLUT[idMarks[i]];
                }
                mesh.uv4 = uvs;
            }

            return mesh;
        }

        static T[] AddSplits<T>(T[] array, List<int> toSplit, int offset) {
            if (array.Length == 0) return null;
            for (int i = 0; i < toSplit.Count; i++) {
                array[offset + i] = array[toSplit[i]];
            }
            return array;
        }

        static readonly Vector2[] uvLUT = new Vector2[4]{
            new Vector2(-1,7),
            new Vector2(-1,1),
            new Vector2(-1,2),
            new Vector2(-1,4)
        };

        static readonly Color32[] colorsLUT = new Color32[4]{
            new Color32(255, 255, 255, 0),
            new Color32(255, 0, 0, 0),
            new Color32(0,255, 0, 0),
            new Color32(0, 0,255, 0)
        };

        static readonly int[][] markLUT = new int[64][]{
                new int[3]{1,2,3},//0,0,0
                new int[3]{0,2,3},//1,0,0
                new int[3]{0,1,3},//2,0,0
                new int[3]{0,1,2},//3,0,0

                new int[3]{2,0,3},//0,1,0
                new int[3]{0,-2,3},//1,1,0
                new int[3]{0,0,3},//2,1,0
                new int[3]{0,0,2},//3,1,0

                new int[3]{1,0,3},//0,2,0
                new int[3]{0,0,3},//1,2,0
                new int[3]{0,-1,3},//2,2,0
                new int[3]{0,0,1},//3,2,0

                new int[3]{1,0,2},//0,3,0
                new int[3]{0,0,2},//1,3,0
                new int[3]{0,0,1},//2,3,0
                new int[3]{-1,0,2},//3,3,0

                new int[3]{ 3,2,0},//0,0,1
                new int[3]{0,3,-2},//1,0,1
                new int[3]{0,3,0},//2,0,1
                new int[3]{0,2,0},//3,0,1

                new int[3]{ 3,0,-2},//0,1,1
                new int[3]{0,-2,-3},//1,1,1
                new int[3]{0,0,-3},//2,1,1
                new int[3]{0,0,-2},//3,1,1

                new int[3]{ 3,0,0},//0,2,1
                new int[3]{0,0,-3},//1,2,1
                new int[3]{0,-3,0},//2,2,1
                new int[3]{0,0,0},//3,2,1

                new int[3]{2,0,0},//0,3,1
                new int[3]{0,0,-2},//1,3,1
                new int[3]{0,0,0},//2,3,1
                new int[3]{0,-2,0},//3,3,1

                new int[3]{1,3,0},//0,0,2
                new int[3]{0,3,0},//1,0,2
                new int[3]{0,1,-3},//2,0,2
                new int[3]{0,1,0},//3,0,2

                new int[3]{ 3,0,0},//0,1,2
                new int[3]{0,-3,0},//1,1,2
                new int[3]{0,0,-3},//2,1,2
                new int[3]{0,0,0},//3,1,2

                new int[3]{1,0,-3},//0,2,2
                new int[3]{0,0,-3},//1,2,2
                new int[3]{0,-1,-3},//2,2,2
                new int[3]{0,0,-1},//3,2,2

                new int[3]{1,0,0},//0,3,2
                new int[3]{0,0,0},//1,3,2
                new int[3]{0,0,-1},//2,3,2
                new int[3]{0,-1,0},//3,3,2

                new int[3]{1,2,0},//0,0,3
                new int[3]{0,2,0},//1,0,3
                new int[3]{0,1,0},//2,0,3
                new int[3]{0,1,-2},//3,0,3

                new int[3]{2,0,0},//0,1,3
                new int[3]{0,-2,0},//1,1,3
                new int[3]{0,0,0},//2,1,3
                new int[3]{0,0,-2},//3,1,3

                new int[3]{1,0,0},//0,2,3
                new int[3]{0,0,0},//1,2,3
                new int[3]{0,-1,3},//2,2,3
                new int[3]{0,0,-1},//3,2,3

                new int[3]{1,-2,0},//0,3,3
                new int[3]{0,0,-2},//1,3,3
                new int[3]{0,0,-1},//2,3,3
                new int[3]{0,-1,-2},//3,3,3
            };
    }
}