using UnityEngine;
using System.Collections;

public class Follow : MonoBehaviour {

	public GameObject target;

	Vector3 delta;

	void Start() {

		delta = transform.position - target.transform.position;

	}

	// Update is called once per frame
	void Update() {

		Vector3 targetPosition = target.transform.position;
		Vector3 newPosition = transform.position;

		//targetPosition.y = newPosition.y;

		newPosition = Vector3.Lerp(newPosition, targetPosition, 0.75f);
		transform.position = newPosition + delta;

	}
}
