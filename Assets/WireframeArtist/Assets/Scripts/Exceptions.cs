#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WireframeArtist {

    /// 
    /// Classes for custom exception handling
    ///

    public class TooMuchVerticesException : WFAException {
	    public Mesh mesh;
	    public TooMuchVerticesException(Mesh mesh) {
	        this.mesh = mesh;
	        EditorUtility.DisplayDialog("Wireframe Artist Baker",
	            "The mesh " + mesh.name + " has too many triangles (>20k). Please break the mesh up into more than one part.",
	            "Got it!");
	    }
	    public TooMuchVerticesException(string message): base(message) {}
	    public TooMuchVerticesException(string message, System.Exception inner): base(message, inner) {}
	}

	public class MissingException : WFAException {
        public MissingException(string message) {
            EditorUtility.DisplayDialog("Wireframe Artist Baker",
                message,
                "Got it!");
        }
    }

    public class CancelledProgressException : WFAException { }

    public class WFAException : System.Exception {
        public WFAException(): base() { }
        public WFAException(string message): base(message) { }
        public WFAException(string message, System.Exception inner): base(message, inner) { }
    }

}
#endif