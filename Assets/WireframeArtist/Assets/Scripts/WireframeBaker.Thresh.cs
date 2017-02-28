using System.Collections.Generic;
using UnityEngine;
using WireframeArtist.Thresh;

namespace WireframeArtist {
    public static partial class WireframeBaker {

        static Dictionary<Position, Point> points, sharpPoints;
        static Dictionary<EdgeKey, Edge> edges;
        static Triangle[] triangles;
        static List<Vector3> idMarks;

        static Point[] pnts;
        static Edge[] edgs;
        static int[] ids;

        static Mesh ConvertThresh(Mesh mesh) {
            var vertIDs = mesh.triangles;
            #if UNITY_EDITOR
                if (vertIDs.Length >= 65534) throw new TooMuchVerticesException(mesh);
            #endif

            points = new Dictionary<Position, Point>();
            sharpPoints = new Dictionary<Position, Point>();
            edges = new Dictionary<EdgeKey, Edge>();
            pnts = new Point[3];
            edgs = new Edge[3];
            ids = new int[3];

            var vertices = mesh.vertices;
            var newVerts = new Vector3[vertIDs.Length];
            var newIds = new int[vertIDs.Length];
            triangles = new Triangle[vertIDs.Length / 3];
            idMarks = new List<Vector3>(new Vector3[vertIDs.Length]);

            for (int i = 0, triID = 0; i < vertIDs.Length; i += 3, triID++) {
                ids[0] = vertIDs[i];
                ids[1] = vertIDs[i + 1];
                ids[2] = vertIDs[i + 2];
                newVerts[i] = vertices[ids[0]];
                newVerts[i + 1] = vertices[ids[1]];
                newVerts[i + 2] = vertices[ids[2]];
                newIds[i] = i;
                newIds[i + 1] = i + 1;
                newIds[i + 2] = i + 2;

                var triangle = new Triangle(i, i + 1, i + 2);
                triangle.CalcNormal(newVerts);
                triangles[triID] = triangle;

                for (int j = 0; j < 3; j++) {
                    pnts[j] = GetPoint(newVerts[i + j]);
                    pnts[j].AddTriangle(triID);
                }

                for (int j = 0; j < 3; j++) {
                    int id0 = i + j;
                    int id1 = i + ((j + 1) % 3);
                    edgs[j] = GetEdge(newVerts[id0], newVerts[id1]);
                    edgs[j].AddTriangle(triID, id0, id1);
                }
            }

            // mark edges if sharp enough
            foreach (var e in edges.Values) {
                if (e.triangles.Count == 1) {
                    AddEdge2Points(e);
                    Mark(e, e.triangles[0]);
                    continue;
                }
                for (int i = 0; i < e.triangles.Count; i++) {
                    var trii = e.triangles[i];
                    for (int j = i + 1; j < e.triangles.Count; j++) {
                        var trij = e.triangles[j];
                        if (!IsSharp(trii, trij)) continue;
                        AddEdge2Points(e);
                        Mark(e, trij);
                        Mark(e, trii);
                    }
                }
            }
            
            // mark corners
            foreach (var pp in sharpPoints) { //for every sharp point
                var p = pp.Value;
                var pos = pp.Key;
                for (int i = 0; i < p.triangles.Count; i++) { //for every triangle at point
                    var triangle = triangles[p.triangles[i]];
                    for (int j = 0; j < 3; j++) { // for every vertex at triangle
                        var id = triangle[j];
                        if (!pos.Equals(new Position(newVerts[id]))) continue; // only consider vertices at point
                        var m = idMarks[id];
                        if (m.x == 1 || m.y == 1 || m.z == 1) break; // already marked

                        var m1 = idMarks[triangle[(j + 1) % 3]];
                        var m2 = idMarks[triangle[(j + 2) % 3]];
                        if (m1.x == 0 && m2.x == 0) m.x = 1;
                        else if (m1.y == 0 && m2.y == 0) m.y = 1;
                        else m.z = 1;

                        idMarks[id] = m;
                        break;
                    }
                }
            }

            mesh.vertices = newVerts;
            SetTriangles(newIds, mesh);

            mesh.normals = SplitArray(mesh.normals, vertIDs);
            mesh.boneWeights = SplitArray(mesh.boneWeights, vertIDs);
            mesh.tangents = SplitArray(mesh.tangents, vertIDs);
            mesh.uv = SplitArray(mesh.uv, vertIDs);
            mesh.uv2 = SplitArray(mesh.uv2, vertIDs);
            mesh.uv3 = SplitArray(mesh.uv3, vertIDs);

            if (channel == Channel.Color) {
                var colors = new Color32[idMarks.Count];
                for (int i = 0; i < idMarks.Count; i++) {
                    var m = (Vector3.one - idMarks[i]) * 255f;
                    colors[i] = new Color32((byte)m.x, (byte)m.y, (byte)m.z, 0);
                }
                mesh.colors32 = colors;
            } else {
                mesh.colors32 = SplitArray(mesh.colors32, vertIDs);
            }

            if (channel == Channel.UV3) {
                var uvs = new Vector2[idMarks.Count];
                for (int i = 0; i < idMarks.Count; i++) {
                    var m = Vector3.one - idMarks[i];
                    var idx = m.x + m.y * 2 + m.z * 4;
                    uvs[i] = new Vector2(-1, idx);
                }
                mesh.uv4 = uvs;
            } else {
                mesh.uv4 = SplitArray(mesh.uv4, vertIDs);
            }

            return mesh;
        }

        static bool IsSharp(int trii, int trij) {
            float dot = Vector3.Dot(triangles[trii].normal, triangles[trij].normal);
            dot = Mathf.Clamp(dot, -0.99999f, 0.99999f);
            float acos = Mathf.Acos(dot);
            return acos > thresholdAngle;
        }

        static void Mark(Edge e, int tri) {
            int id0, id1;
            e.GetVerts(tri, out id0, out id1);
            for (int j = 0; j < 3; j++) {
                var m0 = idMarks[id0];
                var m1 = idMarks[id1];
                if (m0[j] == 1 && m1[j] == 1) break;
                if (m0[j] != 0 || m1[j] != 0) continue;
                m0[j] = 1;
                m1[j] = 1;
                idMarks[id0] = m0;
                idMarks[id1] = m1;
                break;
            }
        }

        static void AddEdge2Points(Edge e) {
            if (!sharpPoints.ContainsKey(e.p0)) sharpPoints.Add(e.p0, points[e.p0]);
            if (!sharpPoints.ContainsKey(e.p1)) sharpPoints.Add(e.p1, points[e.p1]);
        }

        static Point GetPoint(Vector3 v) {
            var key = new Position(v);
            Point point;
            if (!points.TryGetValue(key, out point)) {
                point = new Point(key);
                points.Add(key, point);
            }
            return point;
        }

        static Edge GetEdge(Vector3 v0, Vector3 v1) {
            EdgeKey key = new EdgeKey(v0, v1);
            Edge edge;
            if (!edges.TryGetValue(key, out edge)) {
                edge = new Edge(key);
                edges.Add(key, edge);
            }
            return edge;
        }

    }
}
