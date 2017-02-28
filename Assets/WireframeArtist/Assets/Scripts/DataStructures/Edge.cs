using System;
using System.Collections.Generic;
using UnityEngine;

namespace WireframeArtist.Thresh {
    struct EdgeKey : IEquatable<EdgeKey> {
        public Position p0, p1;

        public EdgeKey(Position p0, Position p1) {
            this.p0 = p0;
            this.p1 = p1;
        }

        public EdgeKey(Vector3 p0, Vector3 p1) : 
            this(new Position(p0), new Position(p1)){}

        public override int GetHashCode() {
            unchecked { // Overflow is fine, just wrap
                int hash = 17;
                hash = hash * 486187739 + p0.GetHashCode() + p1.GetHashCode();
                return hash;
            }
        }

        public bool Equals(EdgeKey e) {
            return (p0.Equals(e.p0) && p1.Equals(e.p1))
                || (p1.Equals(e.p0) && p0.Equals(e.p1)); // non-directional
        }

        public override string ToString() {
            return "(" + p0.x + "," + p0.y + "," + p0.z + ")";
        }
    }

    class Edge : IEquatable<Edge> {
        public EdgeKey key;
        public List<int> triangles;
        public List<int> vertices;

        public Position p0 { get { return key.p0; } }
        public Position p1 { get { return key.p1; } }

        public Edge(EdgeKey key) {
            triangles = new List<int>();
            vertices = new List<int>();
            this.key = key;
        }

        public void AddTriangle(int tri, int v0, int v1) {
            triangles.Add(tri);
            vertices.Add(v0);
            vertices.Add(v1);
        }

        public override int GetHashCode() {
            return key.GetHashCode();
        }

        public bool Equals(Edge e) {
            return key.Equals(e.key);
        }

        public void GetVerts(int tri, out int id0, out int id1) {
            id0 = -1; id1 = -1;
            for (int i = 0; i < triangles.Count; i++) {
                if (triangles[i] != tri) continue;
                id0 = vertices[i * 2];
                id1 = vertices[i * 2+1];
                break;
            }
        }
    }
}
