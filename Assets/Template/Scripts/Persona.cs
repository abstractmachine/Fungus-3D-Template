using UnityEngine;
using System.Collections;

public class Persona : MonoBehaviour {

	GameObject currentPlayer = null;

	void OnMouseDown() {

		// get access to player
		GameObject player = GameObject.FindGameObjectWithTag("Player");

		if (player == null) {
			Debug.LogWarning("Player doesn't exist!");
			return;
		}

		// FIXME: The player shouldn't always have to be in the flowchart
		// if we're in the current flowchart discussion
		if (player.GetComponent<Player>().IsCharacterInFlowchart(this.gameObject)) {
			player.GetComponent<Player>().OnClick(this.gameObject);
			return;
		}

		// if we're currently talking to the player
		if (currentPlayer != null) {
			currentPlayer.GetComponent<Player>().OnClick(this.gameObject);
			return;
		}

		// ok we're not part of the current discussion

		// get the ground object
		GameObject ground = GameObject.FindGameObjectWithTag("Ground");
		// tell the player to come here
		ground.GetComponent<Ground>().TouchedObject(this.gameObject);

	}


	void OnTriggerEnter(Collider other) {

		// only register intersections with the player
		if (other.gameObject.tag != "Player") {
			return;
		}

		// make sure we're not already talking to this player
		if (currentPlayer == other.gameObject) {
			return;
		}

		// make sure we're not already talking with someone else
		if (currentPlayer != null) {
			return;
		}

		// ok, register this as valid other
		currentPlayer = other.gameObject;

	}


	void OnTriggerExit(Collider other) {

		// only register intersections with the player
		if (other.gameObject.tag != "Player") {
			return;
		}

		// make sure this is the actual person we were interacting with
		if (other.gameObject == currentPlayer) {
			currentPlayer = null;
		}
      
	}
 

}
