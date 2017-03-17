using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialObjectTarget : MonoBehaviour {


	// Ask Alex about this ... should I make it a class bleergh ???
//	[System.Serializable]
//	public struct targetData {
//		AudioClip audioClip;
//		MeshFilter meshFilter;
//		Material material;
//
//	}


//	public List<targetData> targetDataList = new List<targetData>();

	public List<AudioSource> audioSources = new List<AudioSource>();
	public List<MeshFilter> targetMeshes = new List<MeshFilter> ();
	public List<Material> highlightMaterials = new List<Material> ();

	private bool active; //the active state of the target objects
	private Material[] objectMaterials; //the current materials of each mesh


	void Awake () {
		active = false;
		//Get the current materials from the meshes in target meshes so can toggle back later
		int targetMeshesSize = targetMeshes.Count;
		objectMaterials = new Material[targetMeshesSize];
		for (int i=0; i< targetMeshesSize; i++){
			objectMaterials[i] = getMaterialFromObject (targetMeshes [i]);
		}

	}


	//Helper function to find the transform of a Mesh that has been imported in 
	//and currently has bs transform vals
	Vector3 getActualObjectPosition(MeshFilter mfilter) {

		GameObject targetPosition = mfilter.gameObject;
		Vector3 updatedPosition = targetPosition.GetComponent<Renderer> ().bounds.center;
		return updatedPosition;
	
	}


	//Helper function to get the material from a mesh object
	Material getMaterialFromObject(MeshFilter mfilter) {

		GameObject targetMesh = mfilter.gameObject;
		MeshRenderer renderer =  targetMesh.GetComponent<MeshRenderer>();
		return renderer.material;
	
	}



	//Helper function to set a material
	void setMaterialOfObject(Material nextMaterial, MeshFilter mfilter ) {

		GameObject targetMesh = mfilter.gameObject;
		MeshRenderer renderer =  targetMesh.GetComponent<MeshRenderer>();
		renderer.material = nextMaterial;

	}



	// turns on sounds, changes mesh material etc
	private IEnumerator activateTargets()
	{
		//check to ensure each mesh as an associated sound
		if (targetMeshes.Count == audioSources.Count) {

			for (int i=0; i<targetMeshes.Count;i++) {
				//Find the length of each clip
				float clipLength = audioSources[i].clip.length;

				//Find the transform location of the target mesh
//				Vector3 playLocation = targetMeshes[i].transform.position; - using existing transform position
				Vector3 playLocation = getActualObjectPosition(targetMeshes[i]); //using updated position
				Debug.Log(playLocation);

				//Play the audio at the target mesh location
				AudioSource.PlayClipAtPoint(audioSources[i].clip, playLocation);

				//Change the material of the target mesh
				setMaterialOfObject(highlightMaterials[i],targetMeshes[i]);

				yield return new WaitForSeconds(clipLength); // wait for the duration of the clip

				//Change back the material of the target mesh
				setMaterialOfObject(objectMaterials[i],targetMeshes[i]);

			}

			//Reset the active state
			active = false;


		} else {
			throw new Exception("Ensure you have the same number of things you idiot ");
		}
	}



	// called by trigger when a collision with player has happened
	public bool toggle () {
//		// if a collision occures check to see whether target is active or not
		if (active){
			//do nothing
		}else{
			//activate active
			active = true;
			StartCoroutine ("activateTargets");
		}
		//return the active state
		return active;
	
	}

}
