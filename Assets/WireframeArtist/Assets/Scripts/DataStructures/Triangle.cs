using UnityEngine;

namespace WireframeArtist.Thresh {
    struct Triangle{
        public int v0, v1, v2; // 3 vertices
        public Vector3 normal;

        public Triangle(int v0, int v1, int v2) {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
            normal = new Vector3();
        }

        public void CalcNormal(Vector3[] vertices) {
            var p1 = vertices[v1] - vertices[v0];
            var p2 = vertices[v2] - vertices[v0];
            normal = Vector3.Cross(p1, p2).normalized;
        }

        public int this[int i] {
            get { return i==0 ? v0 : (i==1 ? v1 : v2); }
            set {
                if (i == 0) v0 = value;
                else if (i == 1) v1 = value;
                else v2 = value;
            }
        }


    }
}
