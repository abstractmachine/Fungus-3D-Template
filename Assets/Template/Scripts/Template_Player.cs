using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Fungus;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]

public class Template_Player : MonoBehaviour {

    #region Delegates

    public delegate void PlayerMovedDelegate(float distance);
    public static event PlayerMovedDelegate PlayerMoved;

    public delegate void PlayerReachedTargetDelegate();
    public static event PlayerReachedTargetDelegate PlayerReachedTarget;

    public delegate void PlayerStartedDialogueWithDelegate(List<GameObject> personae);
    public static event PlayerStartedDialogueWithDelegate PlayerStartedDialogueWith;

    public delegate void PlayerStoppedDialogueWithDelegate(List<GameObject> personae);
    public static event PlayerStoppedDialogueWithDelegate PlayerStoppedDialogueWith;

    #endregion

	#region Variables

	Flowchart currentFlowchart = null;
	GameObject currentPersona = null;

	GameObject targetObject;
	Vector3 goal;
    bool goalSet = false;

	Vector2 smoothDeltaPosition = Vector2.zero;
	Vector2 velocity = Vector2.zero;

	bool idleBoredom = false;

	#endregion


	#region getter/setter

    bool IsWalking { get { return GetComponent<NavMeshAgent>().velocity.sqrMagnitude > 0.01f; } }

	#endregion


	#region Init

	void Start() {

		// Don’t update position automatically
        GetComponent<NavMeshAgent>().updatePosition = false;

	}

	#endregion


	#region Animation

	void Update() {

		UpdateIdle();
		UpdatePosition();

	}


	void UpdateIdle() {

		if (Random.Range(0, 500) < 1) {
			idleBoredom = !idleBoredom;
            GetComponent<Animator>().SetBool("Bored", idleBoredom);
		}

	}

	void UpdatePosition() {

        Vector3 worldDeltaPosition = GetComponent<NavMeshAgent>().nextPosition - transform.position;

        // Map 'worldDeltaPosition' to local space
        float dx = Vector3.Dot (transform.right, worldDeltaPosition);
        float dy = Vector3.Dot (transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2 (dx, dy);

        // Low-pass filter the deltaMove
        float smooth = Mathf.Min(1.0f, Time.deltaTime/0.15f);
        smoothDeltaPosition = Vector2.Lerp (smoothDeltaPosition, deltaPosition, smooth);

        // Update velocity if time advances
        if (Time.deltaTime > 1e-5f)
        {
            velocity = smoothDeltaPosition / Time.deltaTime;
        }

        bool shouldMove = velocity.magnitude > 0.5f && GetComponent<NavMeshAgent>().remainingDistance > GetComponent<NavMeshAgent>().radius;

        if (shouldMove)
        {
            GetComponent<Animator>().SetFloat("Velocity", 1.0f);
        }
        else
        {
            GetComponent<Animator>().SetFloat("Velocity", 0.0f);
        }

		// move head      
        //GetComponent<LookAt>().lookAtTargetPosition = GetComponent<NavMeshAgent>().steeringTarget + transform.forward;

        // Pull agent towards character
        if (worldDeltaPosition.magnitude > GetComponent<NavMeshAgent>().radius)
        {
            GetComponent<NavMeshAgent>().nextPosition = transform.position + 0.9f * worldDeltaPosition;
        }
	}

	void OnAnimatorMove() {

		// Update position to agent position
        //transform.position = GetComponent<NavMeshAgent>().nextPosition;

        // Update position based on animation movement using navigation surface height
        Vector3 position = GetComponent<Animator>().rootPosition;
        position.y = GetComponent<NavMeshAgent>().nextPosition.y;
        transform.position = position;

        if (IsWalking)
        {
            Vector3 deltaPosition = position - goal;
            BroadcastDistance(deltaPosition.sqrMagnitude);
        }
	}

	#endregion


	#region Interaction


	void OnMouseDown() {

		OnClick(null);

	}

	public void OnClick(GameObject clickedObject) {
      
		// if we're not talking to anyone
		if (currentFlowchart == null || currentPersona == null) {      
			// are we walking?
			if (IsWalking) {
				StopWalking();            
			}         
			return;
		}

		// if we currently have a menuDialog active
		if (transform.FindChild("Dialogues/MenuDialog").gameObject.activeSelf) {
			print("menuDialog still active");
			return;
		}

		// ok, we do NOT have a menuDialog active

		// if there is no current dialogue
		if (!currentFlowchart.HasExecutingBlocks()) {   
			// if we're still in collision with a Persona
			if (currentPersona != null) {
				// try to force restart that previous dialogue
				StartFlowchart(currentPersona);
			}
			// whatever the case, leave this method
			return;
		}

		List<GameObject> charactersInFlowchart = GetCharactersInFlowchart(currentFlowchart);

		// if the clicked object isn't even in the current dialog
		if (clickedObject != null && !charactersInFlowchart.Contains(clickedObject)) {
			Debug.LogWarning("Character " + GetPath(this.gameObject.transform) + " isn't in flowchart " + currentFlowchart.name);
			return;
		}

		// go through each persona we're potentially talking to
		foreach (GameObject characterObject in charactersInFlowchart) {

			// make sure that object isn't us
//			if (characterObject.name == this.gameObject.name) {
//				continue;
//			}
			// get the path to their SayDialog
			SayDialog personaSayDialog = characterObject.GetComponentInChildren<SayDialog>();
			// if this dialog is actually something
			if (personaSayDialog != null) {
				// check to see if that dialog object is active
				if (personaSayDialog.gameObject.activeSelf) {
					// ok, push dat button!
					personaSayDialog.continueButton.onClick.Invoke();
					// all done
					return;
				}
			}
		}

	}

	#endregion


	#region Trigger


	void OnTriggerEnter(Collider other) {

        // if we're interacting with a persona
        if (other.gameObject.tag == "Persona" && other.gameObject == targetObject) {

            // make sure we're not already talking with someone else
            if (currentPersona != null && currentPersona != other.gameObject) {
                return;
            }

            // tell the Persona to turn towards us, the Player
            other.GetComponent<Template_Persona>().TurnTowards(this.gameObject);

            // start talking
            StartFlowchart(other.gameObject);

		}

	}


	void OnTriggerStay(Collider other) {

		// if we're interacting with another character
		if (IsWalking && other.gameObject.tag == "Persona" && other.gameObject == targetObject) {
			// get our distance to that character
			float distance = CalculateDistanceToObject(other.gameObject);
			// if too close
			if (distance < 2.5f) {
				// stop current movement
				StopWalking();
			}
		}

		// if we're touching the TouchTarget && we're at the end
		if (other.gameObject.tag == "TouchTarget" && IsAtDestination()) {
			// get rid of the TouchTarget
			Destroy(other.gameObject);
           //
            BroadcastDistance(0.0f);
		}

	}


	void OnTriggerExit(Collider other) {

        // ignore anyone who is not a Persona
        if (other.gameObject.tag != "Persona") {
			return;
		}
      
		// make sure this is the actual person we were interacting with
		if (other.gameObject != currentPersona) {
			return;
		}

        StopCurrentFlowchart();

		currentPersona = null;
		currentFlowchart = null;

	}

	#endregion


	#region NavMesh

	public void GoToPosition(Vector3 position) {

		targetObject = null;
		goal = position;
        goalSet = true;
		GetComponent<NavMeshAgent>().destination = goal;

	}


	public void GoToObject(GameObject other) {

		targetObject = other;

		Vector3 position = other.transform.position;
		position.y = 0.01f;
		goal = position;
        goalSet = true;
		GetComponent<NavMeshAgent>().destination = goal;

	}

	void StopWalking() {

		// go to where we already are
		targetObject = null;
        // remember that this is the new target
		goal = transform.position;
        GetComponent<NavMeshAgent>().ResetPath();
        // broadcast that we're at the new target
        BroadcastDistance(0.0f);
        // we no longer have a goal
        goalSet = false;

	}

	bool IsAtDestination() {
      
        if (GetComponent<NavMeshAgent>().pathPending) {
			return true;
		}

        if (GetComponent<NavMeshAgent>().remainingDistance <= GetComponent<NavMeshAgent>().stoppingDistance) {
            if (!GetComponent<NavMeshAgent>().hasPath || GetComponent<NavMeshAgent>().velocity.sqrMagnitude == 0f) {
				return true;
			}
		}

		return false;

	}


    void BroadcastDistance(float distance) {

        // if there are listeners
        if (goalSet && PlayerMoved != null)
        {   // broadcast movement change
            PlayerMoved(distance);
        }

        // make sure there are listeners
        if (distance == 0.0f && PlayerReachedTarget != null)
        {   // broadcast that we've reached the target
            PlayerReachedTarget();
        }

    }

	#endregion



    #region Gestures

    void StartedDialogue(Flowchart flowchart) {
        // get a list of all the characters in this flowchart
        List<GameObject> personae = GetCharactersInFlowchart(flowchart);
        // if there are any listeners
        if (PlayerStartedDialogueWith != null)
        {   // tell them all the objects we've started talking to
            PlayerStartedDialogueWith(personae);
        }
    }

    void StoppedDialogue(Flowchart flowchart) {

        // get a list of all the characters in this flowchart
        List<GameObject> personae = GetCharactersInFlowchart(flowchart);
        // if there are any listeners
        if (PlayerStoppedDialogueWith != null)
        {   // tell them all the objects we've stopped discussing with
            PlayerStoppedDialogueWith(personae);
        }

    }

    #endregion


	#region Flowchart

    /// <summary>
    /// Starts the flowchart.
    /// </summary>
    /// <param name="other">The other GameObject we're dialoguing with.</param>

	void StartFlowchart(GameObject other) {

        // find this flowchart in this character
		Flowchart flowchart = GetFlowchart(other.gameObject); 

		// if we found this persona's flowchart
		if (flowchart != null) {
            // remember who we're interacting with
			currentPersona = other;
            // remember which flowchart we're interacting with
			currentFlowchart = flowchart;
            // start this specific flowchart
            flowchart.SendFungusMessage("Start");
            // Started a dialog using this flowchart
            StartedDialogue(flowchart);
		}

	}


    /// <summary>
    /// Stops the current flowchart and informs all concerned characters.
    /// </summary>

    void StopCurrentFlowchart() {

        if (currentFlowchart != null) {
            // Stopped a dialog with a Persona
            StoppedDialogue(currentFlowchart);
            // turn off Flowchart
			currentFlowchart.GetComponent<Flowchart>().StopAllBlocks();
            currentFlowchart.GetComponent<Flowchart>().StopAllCoroutines();
            // send a stop message to the current flowchart
            currentFlowchart.SendFungusMessage("Stop");
		}

		// hide any possible menus of our own
		Transform playerSayTransform = transform.FindChild("Dialogues/SayDialog");

		if (playerSayTransform != null) {
			playerSayTransform.gameObject.SetActive(false);
		}

		Transform playerMenuTransform = transform.FindChild("Dialogues/MenuDialog");

		if (playerMenuTransform != null) {

			GameObject playerMenu = playerMenuTransform.gameObject;

			if (playerMenu != null) {
				if (ChildIsActive(playerMenu, "TimeoutSlider"))
					SetActive(playerMenu, "TimeoutSlider", false);
				if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton0"))
					SetActive(playerMenu, "ButtonGroup/OptionButton0", false);
				if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton1"))
					SetActive(playerMenu, "ButtonGroup/OptionButton1", false);
				if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton2"))
					SetActive(playerMenu, "ButtonGroup/OptionButton2", false);
				if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton3"))
					SetActive(playerMenu, "ButtonGroup/OptionButton3", false);
				if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton4"))
					SetActive(playerMenu, "ButtonGroup/OptionButton4", false);
				if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton5"))
					SetActive(playerMenu, "ButtonGroup/OptionButton5", false);
				if (playerMenu.activeInHierarchy)
					playerMenu.SetActive(false);
			}

		}

	}


	bool ChildIsActive(GameObject parentObject, string path) {
		return parentObject.transform.FindChild(path).gameObject.activeInHierarchy;
	}


	void SetActive(GameObject parentObject, string path, bool newState) {
		parentObject.transform.FindChild(path).gameObject.SetActive(newState);
	}


	Flowchart GetFlowchart(GameObject gameObject) {

        Flowchart flowchart = gameObject.GetComponentInChildren<Flowchart>();

        return flowchart;

	}


	// TODO: Add all characters in all blocks

    /// <summary>
    /// Go into the Flowchart and figure out which characters are referenced there.
    /// </summary>
    /// <returns>A list of the GameObjects of characters in the flowchart.</returns>
    /// <param name="flowchart">The Flowchart.</param>

	List<GameObject> GetCharactersInFlowchart(Flowchart flowchart) {

		List<GameObject> possiblePersonaObjects = new List<GameObject>();

		if (flowchart == null) {
			Debug.LogError("Flowchart == null");
			return possiblePersonaObjects;
		}

		// FIXME: This doesn't work when there is no executing block
		// if we have a currently executing block
		List<Block> blocks = flowchart.GetExecutingBlocks();
		// go through each executing block
		foreach (Block block in blocks) {
			// get the command list
			List<Command> commands = block.commandList;
			// go through the command list
			foreach (Command command in commands) {
				// if this is a say command
				if (command.GetType().ToString() == "Fungus.Say") {
					// force type to say
					Say sayCommand = (Say)command;
					// get the gameobject attached to this character
					GameObject persona = sayCommand.character.gameObject.transform.parent.gameObject;
					// make sure this one isn't already in the list
					if (possiblePersonaObjects.Contains(persona)) {
						continue;
					}
					// ok, add it to the list of possible people we're talking to
					possiblePersonaObjects.Add(persona);
				} // if type
			} // foreach Command
		} // foreach(Block

		// if this list doesn't contain the player
		if (!possiblePersonaObjects.Contains(this.gameObject)) {
//			print("Force-add Player");
			possiblePersonaObjects.Add(this.gameObject);
		}

		return possiblePersonaObjects;

	}


      public bool IsCharacterInFlowchart(GameObject character) {
    
          // if there's no current persona we're interacting with
          if (currentPersona == null) {
              return false;
          }
    
          Flowchart flowchart = null;
    
          // if there isn't even a flowchart, forget it
          if (currentFlowchart != null) {
              flowchart = currentFlowchart;
          } else {
              // try to get a flowchart from the current persona
              flowchart = GetFlowchart(currentPersona);
          }
          // still no flowchart? null
          if (flowchart == null) {
              return false;
          }
          // ok, we've got a flowchart, who's in it?
          List<GameObject> characters = GetCharactersInFlowchart(currentFlowchart);
    
          // is this character in it?
          if (characters.Contains(character)) {
              return true;
          }
          // if we're here, then the answer is no
          return false;
    
      }


	#endregion


	#region Tools

	float CalculateDistanceToObject(GameObject other) {
		// get their position
		Vector3 personaPosition = other.transform.position;
		// annul y
		personaPosition.y = 0f;
		// get our position
		Vector3 playerPosition = this.transform.position;
		// annul y
		playerPosition.y = 0f;
		// get the distance
		return Vector3.Magnitude(playerPosition - personaPosition);      
	}

	public static string GetPath(Transform current) {
		if (current.parent == null)
			return "/" + current.name;
		return GetPath(current.parent) + "/" + current.name;
	}

	#endregion

}











//  GameObject FindDialogues(string name) {
//
//      // get the flowchart with this person's name
//      GameObject persona = FindPersona(name);
//
//      // if we couldn't find this persona
//      if (persona == null) {
////            Debug.LogError("could find persona " + name);
//          return null;
//      }
//
//      // get that persona's SayDialog
//      GameObject dialogues = persona.transform.FindChild("Dialogues").gameObject;
//
//      return dialogues;
//
//  }


//  GameObject FindPersona(string name) {
//
//      GameObject personae = GameObject.Find("Personae");
//      if (personae == null) {
////            Debug.LogError("personae == null");
//          return null;
//      }
//      GameObject persona = personae.transform.FindChild(name).gameObject;
//      if (persona == null) {
////            Debug.LogError("persona == null");
//          return null;
//      }
//
//      return persona;
//
//  }