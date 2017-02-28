using System;
using System.Collections.Generic;
using UnityEngine;

namespace WireframeArtist.Thresh {
    class Point : IEquatable<Point> {
        public List<int> triangles; // always uniques
        public Position position;

        public Point(Position pos) {
            position = pos;
            triangles = new List<int>();
        }

        public void AddTriangle(int tri) {
            triangles.Add(tri);
        }

        public Vector3 vector { get { return position.vector; } }

        public override int GetHashCode() {
            return position.GetHashCode();
        }

        public bool Equals(Point p) {
            return p.position.Equals(position);
        }
    }

    struct Position : IEquatable<Position> {
        public long x, y, z;

        const int scaleFactor = 100000;

        public Vector3 vector {
            get {
                return new Vector3(x, y, z) / scaleFactor;
            }
        }

        public Position(Vector3 pos) {
            x = (long)(pos.x * scaleFactor);
            y = (long)(pos.y * scaleFactor);
            z = (long)(pos.z * scaleFactor);
        }

        public override int GetHashCode() {
            unchecked { // Overflow is fine, just wrap
                int hash = 17;
                hash = hash * 486187739 + x.GetHashCode();
                hash = hash * 486187739 + y.GetHashCode();
                hash = hash * 486187739 + z.GetHashCode();
                return hash;
            }
        }

        public bool Equals(Position p) {
            return x == p.x && y == p.y && z == p.z;
        }
    }
}
