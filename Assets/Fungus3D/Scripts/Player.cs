using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Fungus;

namespace Fungus3D {

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]

    // TODO: Subclass Player from Persona, moving NavMesh goal over to Persona
    // i.e. give Personae the power to walk. Yes, we have a Messiah complex ;-)

    public class Player : MonoBehaviour {



        #region Variables

        bool alive = true;

        Flowchart currentFlowchart = null;
        GameObject currentPersona = null;

        GameObject targetObject;
        Vector3 goal;
        bool goalSet = false;

        Animator animator;
        NavMeshAgent navMeshAgent;

        Vector2 smoothDeltaPosition = Vector2.zero;
        Vector2 velocity = Vector2.zero;

        #endregion



        #region Get/Set

        bool Walking { get { return navMeshAgent.velocity.sqrMagnitude > 0.01f; } }
        bool Dead { get { return !alive; } }

        #endregion



        #region Event Delegates

        public delegate void PlayerMovedDelegate(float distance);
        public static event PlayerMovedDelegate PlayerMoved;

        public delegate void PlayerDiedDelegate(GameObject player);
        public static event PlayerDiedDelegate PlayerDied;

        public delegate void TurnTowardsDelegate(GameObject target);
        public static event TurnTowardsDelegate TurnTowards;

        public delegate void PlayerReachedTargetDelegate();
        public static event PlayerReachedTargetDelegate PlayerReachedTarget;

        public delegate void PlayerStartedDialogueWithDelegate(GameObject player, List<GameObject> personae);
        public static event PlayerStartedDialogueWithDelegate PlayerStartedDialogueWith;

        public delegate void PlayerStoppedDialogueWithDelegate(GameObject player, List<GameObject> personae);
        public static event PlayerStoppedDialogueWithDelegate PlayerStoppedDialogueWith;

        #endregion



        #region Event Listeners

        void OnEnable()
        {
            Persona.ClickedOnPersona += ClickedOnPersona;
            Persona.PersonaIsInCurrentFlowchart += PersonaIsInCurrentFlowchart;
            Persona.GoToPersona += GoToPersona;
            Ground.GoToPosition += GoToPosition;
        }


        void OnDisable()
        {
            Persona.ClickedOnPersona -= ClickedOnPersona;
            Persona.PersonaIsInCurrentFlowchart -= PersonaIsInCurrentFlowchart;
            Persona.GoToPersona -= GoToPersona;
            Ground.GoToPosition -= GoToPosition;
        }

        #endregion



        #region Init

        void Start() {

            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

        	// Don’t update position automatically
            navMeshAgent.updatePosition = false;

        }

        #endregion



        #region Animation

        void Update() {

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Die();
            }

            if (!Dead)
            {
                UpdatePosition();
            }

        }

        void UpdatePosition() {

            Vector3 worldDeltaPosition = navMeshAgent.nextPosition - transform.position;

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

            bool shouldMove = velocity.magnitude > 0.5f && navMeshAgent.remainingDistance > navMeshAgent.radius;

            if (shouldMove)
            {
                animator.SetFloat("Forward", 1.0f);
            }
            else
            {
                animator.SetFloat("Forward", 0.0f);
            }

        	// move head      
            //GetComponent<LookAt>().lookAtTargetPosition = navMeshAgent.steeringTarget + transform.forward;

            // Pull agent towards character
            if (worldDeltaPosition.magnitude > navMeshAgent.radius)
            {
                navMeshAgent.nextPosition = transform.position + 0.9f * worldDeltaPosition;
            }
        }

        void OnAnimatorMove() {

            if (Dead)
            {
                return;
            }

        	// Update position to agent position
            //transform.position = navMeshAgent.nextPosition;

            // Update position based on animation movement using navigation surface height
            Vector3 position = animator.rootPosition;
            position.y = navMeshAgent.nextPosition.y;
            transform.position = position;

            if (Walking)
            {
                Vector3 deltaPosition = position - goal;
                BroadcastDistance(deltaPosition.sqrMagnitude);
            }
        }

        #endregion



        #region Interaction

        void ClickedOnPersona(GameObject persona) {
            // fire the click of the player GameObject
            OnClick(persona);
        }

        void OnMouseDown() {

        	OnClick(null);

        }

        public void OnClick(GameObject clickedObject) {

            if (Dead)
            {
                return;
            }
          
        	// if we're not talking to anyone
        	if (currentFlowchart == null || currentPersona == null) {      
        		// are we walking?
        		if (Walking) {
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

            if (Dead)
            {
                return;
            }

            // if we're interacting with a persona
            if (other.gameObject.tag == "Persona" && other.gameObject == targetObject) {

                // make sure we're not already talking with someone else
                if (currentPersona != null && currentPersona != other.gameObject) {
                    return;
                }

                // tell the Persona to turn towards us, the Player
                //other.GetComponent<Persona>().TurnTowards(this.gameObject);
                if (TurnTowards != null)
                {
                    TurnTowards(this.gameObject);
                }

                // start talking
                StartFlowchart(other.gameObject);

        	}

        }


        void OnTriggerStay(Collider other) {

            if (Dead)
            {
                return;
            }

        	// if we're interacting with another character
        	if (Walking && other.gameObject.tag == "Persona" && other.gameObject == targetObject) {
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

            if (Dead)
            {
                return;
            }

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


        #region Collision

        // TODO: Collisions don't fire (!?)

        void OnCollisionEnter(Collision collision) {

            // if this is the bullet
            if (collision.gameObject.tag == "Bullet")
            {
                // get the first contact point
                ContactPoint contact = collision.contacts[0];
                // we hit a specific rigidbody and we'll use the impact normal for calculating force
                Die(contact.thisCollider.attachedRigidbody, contact.normal);
                // destroy bullet
                Destroy(collision.gameObject);
            }

        }

        void OnCollisionStay(Collision impact) {

//            print("OnCollisionStay " + impact.gameObject.name);

        }

        void OnCollisionExit(Collision impact) {

//            print("OnCollisionStay " + impact.gameObject.name);

        }

        #endregion



        #region NavMesh

        public void GoToPosition(Vector3 position) {

            if (Dead)
            {
                return;
            }

        	targetObject = null;
        	goal = position;
            goalSet = true;
        	navMeshAgent.destination = goal;

        }


        public void GoToPersona(GameObject other) {

            if (Dead)
            {
                return;
            }

        	targetObject = other;

        	Vector3 position = other.transform.position;
        	position.y = 0.01f;
        	goal = position;
            goalSet = true;
        	navMeshAgent.destination = goal;

        }

        void StopWalking() {

            if (Dead)
            {
                return;
            }

        	// go to where we already are
        	targetObject = null;
            // remember that this is the new target
        	goal = transform.position;
            navMeshAgent.ResetPath();
            // broadcast that we're at the new target
            BroadcastDistance(0.0f);
            // we no longer have a goal
            goalSet = false;

        }

        bool IsAtDestination() {
          
            if (navMeshAgent.pathPending) {
        		return true;
        	}

            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f) {
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
                PlayerStartedDialogueWith(this.gameObject, personae);
            }
        }

        void StoppedDialogue(Flowchart flowchart) {

            // get a list of all the characters in this flowchart
            List<GameObject> personae = GetCharactersInFlowchart(flowchart);
            // if there are any listeners
            if (PlayerStoppedDialogueWith != null)
            {   // tell them all the objects we've stopped discussing with
                PlayerStoppedDialogueWith(this.gameObject, personae);
            }

        }

        #endregion


        #region Die

        void Die(Rigidbody hitBodypart, Vector3 hitVector) {

            // first, convert into a ragdoll
            Die();
            // apply the force
            StartCoroutine(DeathForce(hitBodypart, hitVector * 2.0f));

        }



        IEnumerator DeathForce(Rigidbody hitBodypart, Vector3 hitVector, float duration = 0.25f)
        {
            float startTime = Time.time;

            while (Time.time - startTime < duration)
            {
                hitBodypart.AddForce(hitVector, ForceMode.VelocityChange);
                yield return new WaitForFixedUpdate();
            }
        }


        void Die() {

            alive = false;

            // if there are any listeners
            if (PlayerDied != null)
            {   // convert into a rigidbody
                PlayerDied(this.gameObject);
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
                flowchart.SendFungusMessage("Enter");
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
                currentFlowchart.SendFungusMessage("Exit");
        	}

        	// hide any possible menus of our own
        	Transform playerSayTransform = transform.FindChild("Dialogues/SayDialog");

        	if (playerSayTransform != null) {
        		playerSayTransform.gameObject.SetActive(false);
        	}

        	Transform playerMenuTransform = transform.FindChild("Dialogues/MenuDialog");

            // FIXME: This is ugly
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


        // TODO: (Maybe?) Add all characters in all blocks

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


        /// <summary>
        /// Determines whether this Persona GameObject is in the current flowchart.
        /// </summary>
        /// <returns><c>true</c> if the Persona GameObject is in current flowchart; otherwise, <c>false</c>.</returns>
        /// <param name="persona">Persona GameObject.</param>

        bool PersonaIsInCurrentFlowchart(GameObject persona) {

            // if this is ourselves, ignore this request
            if (persona == this.gameObject)
            {
                return false;
            }

            // if there's no current persona we're interacting with
            if (currentPersona == null)
            {
                return false;
            }

            Flowchart flowchart = null;

            // if there isn't even a flowchart, forget it
            if (currentFlowchart != null)
            {   // use the current flowchart for the test
                flowchart = currentFlowchart;
            } else {
                // try to extract a flowchart from the current persona
                flowchart = GetFlowchart(currentPersona);
            }

            // still no flowchart? 
            if (flowchart == null)
            {   // COMPUTER SAYS NO
                return false;
            }

            // ok, we've got a flowchart, who's in it?
            List<GameObject> characters = GetCharactersInFlowchart(currentFlowchart);
            
            // is this character in it?
            if (characters.Contains(persona)) {
                return true;
            }

            // if we're here, then the answer is no
            return false;

        }

        #endregion



        #region Tools

        /// <summary>
        /// Calculates the distance to another GameObject.
        /// </summary>
        /// <returns>The distance to the other object.</returns>
        /// <param name="other">Other GameObject.</param>

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


        /// <summary>
        /// For Debugging purposes. Creates a string containing the transform heirarchy to a specific Transform component.
        /// </summary>
        /// <returns>The complete Transform path to this specific Transform.</returns>
        /// <param name="current">The Transform we want to know the path to.</param>

        public static string GetPath(Transform current) {
            if (current.parent == null)
            {
                return "/" + current.name;
            }
        	return GetPath(current.parent) + "/" + current.name;
        }


        /// <summary>
        /// For Debugging purposes. Creates a string containing the transform heirarchy to a specific GameObject component.
        /// </summary>
        /// <returns>The complete Transform path to this specific GameObject.</returns>
        /// <param name="current">The GameObject we want to know the path to.</param>
        /// 
        public static string GetPath(GameObject obj) {
            return GetPath(obj.transform);
        }


        GameObject FindRagdoll() {

            foreach (Transform t in this.transform)
            {   // if this is the ragdoll
                if (t.gameObject.tag == "Ragdoll")
                {   // remember it
                    return t.gameObject;
                }
            }
            // couldn't find it
            return null;
        }


        GameObject FindModel() {

            foreach (Transform t in this.transform)
            {   // if this is the ragdoll
                if (t.gameObject.tag == "Model")
                {   // remember it
                    return t.gameObject;
                }
            }
            // couldn't find it
            return null;
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

}