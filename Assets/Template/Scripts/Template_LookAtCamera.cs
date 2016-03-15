using UnityEngine;
using System.Collections;

public class Template_LookAtCamera : MonoBehaviour {

	// Turn towards camera permanently
	void Update() {
		// look at the camera
		transform.rotation = Camera.main.transform.rotation;
	}
}
