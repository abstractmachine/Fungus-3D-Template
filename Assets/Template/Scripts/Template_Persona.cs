using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Template_Persona : MonoBehaviour {

    #region Variables

    GameObject currentPlayer = null;

    #endregion


    #region Listeners

    void OnEnable()
    {
        Template_Player.PlayerStartedDialogueWith += PlayerStartedDialogueWith;
        Template_Player.PlayerStoppedDialogueWith += PlayerStoppedDialogueWith;
    }


    void OnDisable()
    {
        Template_Player.PlayerStartedDialogueWith -= PlayerStartedDialogueWith;
        Template_Player.PlayerStoppedDialogueWith += PlayerStoppedDialogueWith;
    }

    void PlayerStartedDialogueWith(List<GameObject> personae) {
        
        // go through list of personae in this flowchart
        foreach (GameObject persona in personae)
        {   // if we're in this flowchart
            if (persona == this.gameObject)
            {   // do something
                break;
            }
        }

    }

    void PlayerStoppedDialogueWith(List<GameObject> personae) {
        
        // go through list of personae in this flowchart
        foreach (GameObject persona in personae)
        {   // if we're in this flowchart
            if (persona == this.gameObject)
            {   // do something
                break;
            }
        }

    }

    #endregion


    #region Interaction

	void OnMouseDown() {

		// get access to player
		GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");

        if (playerGameObject == null) {
			Debug.LogWarning("Player doesn't exist!");
			return;
		}

		// FIXME: The player shouldn't always have to be in the flowchart
		// if we're in the current flowchart discussion
        Template_Player templatePlayer = playerGameObject.GetComponent<Template_Player>();
        if (templatePlayer.IsCharacterInFlowchart(this.gameObject)) {
            templatePlayer.OnClick(this.gameObject);
            return;
		}

		// if we're currently talking to the player
		if (currentPlayer != null) {
			currentPlayer.GetComponent<Template_Player>().OnClick(this.gameObject);
			return;
		}

		// ok we're not part of the current discussion

		// get the ground object
		GameObject ground = GameObject.FindGameObjectWithTag("Ground");
		// tell the player to come here
		ground.GetComponent<Template_Ground>().TouchedObject(this.gameObject);

	}

    #endregion



    #region Collisions

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

    #endregion


    #region Turn

    public void TurnTowards(GameObject target) {

        StartCoroutine(Turn(target));

    }


    IEnumerator Turn(GameObject target) {

        // which way do we have to turn?
        float angleDelta = CalculateAngleDelta(this.gameObject, target);

        // if we need to turn to the left
        if (angleDelta < -10)
        {
            // start turning left
            GetComponent<Animator>().SetBool("TurnLeft", true);
            // wait for us to get close enough
            while (angleDelta < -15)
            {
                // update angle delta
                angleDelta = CalculateAngleDelta(this.gameObject, target);
                // calculate speed
                float speed = 1.0f + Mathf.Abs(angleDelta * 0.005f);
                // speed up the faster we are from the target angle
                GetComponent<Animator>().speed = speed;
                // wait for the next frame
                yield return new WaitForEndOfFrame();
            }
            // stop turning left
            GetComponent<Animator>().SetBool("TurnLeft", false);
            // set speed back to normal (1)
            GetComponent<Animator>().speed = 1.0f;
        }
        // if we need to turn to the right
        else if (angleDelta > 10)
        {
            // start turning right
            GetComponent<Animator>().SetBool("TurnRight", true);
            // wait for us to get close enough
            while (angleDelta > 15)
            {
                // update angle delta
                angleDelta = CalculateAngleDelta(this.gameObject, target);
                // calculate speed
                float speed = 1.0f + Mathf.Abs(angleDelta * 0.005f);
                // speed up the faster we are from the target angle
                GetComponent<Animator>().speed = speed;
                // wait for the next frame
                yield return new WaitForEndOfFrame();
            }
            // stop turning right
            GetComponent<Animator>().SetBool("TurnRight", false);
            // set speed back to normal (1)
            GetComponent<Animator>().speed = 1.0f;
        }
        // else we're already close enough
        else {
            yield return null;
        }


    }

    #endregion


    #region Tools

    float CalculateAngleDelta(GameObject currentObject, GameObject targetObject) {

        // get the delta of these two positions
        Vector3 deltaVector = (targetObject.transform.position - currentObject.transform.position).normalized;
        // create a rotation looking in that direction
        Quaternion lookRotation = Quaternion.LookRotation(deltaVector);
        // get a "forward vector" for each rotation
        Vector3 currentForward = currentObject.transform.rotation * Vector3.forward;
        Vector3 targetForward = lookRotation * Vector3.forward;
        // get a numeric angle for each vector, on the X-Z plane (relative to world forward)
        float angleA = Mathf.Atan2(currentForward.x, currentForward.z) * Mathf.Rad2Deg;
        float angleB = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg;
        // get the signed difference in these angles
        float angleDifference = Mathf.DeltaAngle( angleA, angleB );

        return angleDifference;

    }

    #endregion
 

}
