using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectRotate : MonoBehaviour {

    public float rotationSpeed = 10f;

	
	// Update is called once per frame
	void Update () {
        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
    }
}
