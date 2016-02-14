using UnityEngine;
using System.Collections;

public class LookAtCamera : MonoBehaviour {

	// Turn towards camera permanently
	void Update() {
		// look at the camera
		transform.rotation = Camera.main.transform.rotation;
	}
}
