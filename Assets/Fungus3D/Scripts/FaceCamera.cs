using UnityEngine;
using System.Collections;

namespace Fungus3D {
    
    public class FaceCamera : MonoBehaviour {

    	// Turn towards camera permanently
    	void LateUpdate() {
    		// look at the camera
    		transform.rotation = Camera.main.transform.rotation;
    	}

    }

}
