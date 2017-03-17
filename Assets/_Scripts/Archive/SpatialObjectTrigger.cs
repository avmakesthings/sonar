using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialObjectTrigger : MonoBehaviour {

	public SpatialObjectTarget[] spatialTargets;

	//activate target 
	void OnTriggerEnter(Collider other) {
		print("Triggered");

		foreach (var target in spatialTargets) {
			target.toggle ();
		}


	}


}
