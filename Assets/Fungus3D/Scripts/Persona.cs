using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using System.Collections;
using System.Collections.Generic;

using Fungus;

namespace Fungus3D
{
    // all Persona must have these two components
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]

    // Note: Persona is the base class for Player
    public class Persona : MonoBehaviour
    {

        #region Variables

        // collision log verbosity
        [Tooltip("How verbose should the collision logs be")] 
        public NetworkLogLevel collisionLogLevel = NetworkLogLevel.Off;

        // are we alive?
        private bool alive = true;
        // are we a/the player?
        private bool isPlayer = false;

        // a static pointer to the current Player
        private static GameObject currentPlayer = null;

        // has the ragdoll been configured?
        private bool containsRagdoll = false;

        // movement
        private Vector2 walkVelocity = Vector2.zero;

        // targetting
        private GameObject targetObject;
        private Vector3 targetGoal;
        private bool targetGoalIsSet = false;

        // the current persona we're talking to
        private GameObject currentInterlocutor = null;
        private Flowchart currentFlowchart = null;

        // access to (required) components
        private Animator animator;
        private NavMeshAgent navMeshAgent;

        #endregion


        #region Accessors

        /// <summary>
        /// Is this Persona the Player?
        /// </summary>
        /// <value><c>true</c> if this instance is the Player; otherwise, <c>false</c>.</value>
        public bool IsPlayer { get { return isPlayer; } }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Fungus3D.Persona"/> is walking.
        /// </summary>
        /// <value><c>true</c> if walking; otherwise, <c>false</c>.</value>
        public bool Walking { get { return animator.GetFloat("Speed") > 0.1f; } }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Fungus3D.Persona"/> is dead.
        /// </summary>
        /// <value><c>true</c> if dead (not alive); otherwise, <c>false</c>.</value>
        public bool Dead { get { return !alive; } }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Fungus3D.Persona"/> is alive.
        /// </summary>
        /// <value><c>true</c> if alive; otherwise, <c>false</c>.</value>
        public bool Alive { get { return alive; } }

        /// <summary>
        /// Gets the GameObject this Persona is currently targeting.
        /// </summary>
        /// <value>The target GameObject (can be null).</value>
        public GameObject Target { get { return targetObject; } }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Fungus3D.Persona"/> reached its target.
        /// </summary>
        /// <value><c>true</c> if did reach target; otherwise, <c>false</c>.</value>
        public bool DidReachTarget { get { return (transform.position - navMeshAgent.destination).magnitude < navMeshAgent.stoppingDistance; } }

        #endregion



        #region Event Delegates

        public delegate void ClickedPersonaDelegate(GameObject persona);

        public static event ClickedPersonaDelegate ClickedPersonaListener;

        public delegate void UpdatedTargetDelegate(GameObject target);

        public static event UpdatedTargetDelegate UpdatedTargetListener;

        public delegate void PersonaDiedDelegate(GameObject actor);

        public static event PersonaDiedDelegate PersonaDiedListener;

        public delegate void MovedDelegate(float distance);

        public static event MovedDelegate MovedListener;

        public delegate void ReachedTargetDelegate();

        public static event ReachedTargetDelegate ReachedTargetListener;

        public delegate void StartedDialogueWithDelegate(GameObject player, List<GameObject> personae);

        public static event StartedDialogueWithDelegate StartedDialogueWithListener;

        public delegate void StoppedDialogueWithDelegate(GameObject player, List<GameObject> personae);

        public static event StoppedDialogueWithDelegate StoppedDialogueWithListener;

        #endregion



        #region Listeners

        void OnEnable()
        {
            Persona.StartedDialogueWithListener += PlayerStartedDialogueWith;
            Persona.StoppedDialogueWithListener += PlayerStoppedDialogueWith;
            Persona.ClickedPersonaListener += ClickedPersona;

            // only the player listens for touches
            if (tag == "Player") Ground.TouchedPositionListener += TouchedPosition;
        }


        void OnDisable()
        {
            Persona.StartedDialogueWithListener -= PlayerStartedDialogueWith;
            Persona.StoppedDialogueWithListener += PlayerStoppedDialogueWith;
            Persona.ClickedPersonaListener -= ClickedPersona;

            // only the player listens for touches
            if (tag == "Player") Ground.TouchedPositionListener -= TouchedPosition;
        }

        #endregion



        #region Init

        void Awake()
        {
            // get a reference to the NavMeshAgent component
            navMeshAgent = GetComponent<NavMeshAgent>();
            // get a reference to the animator component
            animator = GetComponent<Animator>();
        }

        void Start()
        {
            // Don’t update position automatically
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;

            // has the ragdoll been configured
            if (GetComponent<Ragdoll>() != null)
            {
                containsRagdoll = true;
            }
            // make sure all dependencies are functional
            CheckForDependencies();

            // are we the player?
            if (tag == "Player")
            {
                isPlayer = true;
                // set pointer to ourselves
                currentPlayer = this.gameObject;
            }
        }

        void CheckForDependencies()
        {
            // make sure there's a physics raycaster on one of the cameras
            PhysicsRaycaster physicsRaycaster = null;
            foreach (Camera camera in Camera.allCameras)
            {
                physicsRaycaster = camera.GetComponent<PhysicsRaycaster>();
                if (physicsRaycaster != null) break;
            }
            // if there isn't one
            if (physicsRaycaster == null)
            {   // console error
                Debug.LogError("There must be a physics raycaster on one of the cameras.");
            }
        }

        #endregion



        #region Update

        void Update()
        {
            if (!Dead)
            {
                Walk();
            }
            // if we're following someone
            if (Walking && targetObject != null)
            {
                UpdateTarget();
            }
            // if we're not the player, and we're walking
            if (!this.IsPlayer && Walking && DidReachTarget)
            {
                StopWalking();
            }
        }


        void UpdateTarget()
        {
            // check our distance
            Vector3 deltaPosition = (targetObject.transform.position - targetGoal);
            // if we've moved
            if (deltaPosition.magnitude > 0.0f)
            {   // re-target the Persona
                TargetPersona(targetObject);
                // if this is the player and there is a listener
                if (this.IsPlayer && UpdatedTargetListener != null)
                {   // tell the target to adjust to the new position
                    UpdatedTargetListener(targetObject);
                }
            }
        }

        #endregion



        #region Interaction

        /// <summary>
        /// Someone clicked on this Persona
        /// Note: This requires a physics Raycaster on a camera
        /// </summary>

        public void OnClick()
        {
            // if we're the Player
            if (this.IsPlayer)
            {
                OnClickPlayer(null);
            }

            // if we're a Persona
            if (!this.IsPlayer)
            {
                OnClickPersona();
            }
        }


        /// <summary>
        /// Handle the click on Player object
        /// </summary>
        /// <param name="clickedObject">Clicked object.</param>

        public void OnClickPlayer(GameObject clickedObject)
        {
            // dead Players can't click
            if (Dead) return;

            // if we're not talking to anyone
            if (currentFlowchart == null || currentInterlocutor == null)
            {      
                // are we walking?
                if (Walking)
                {
                    StopWalking();            
                }
                // not talking to anyone, so no need to go on
                return;
            }

            // ok, this is a dialog click
            OnClickDialog(clickedObject);

        }


        void OnClickPersona()
        {

            // FIXME: Theoretically, the player shouldn't always have to be in the flowchart
            // Persona should be able to discuss amongst themselves (cf. The Sims)

            Persona playerScript = currentPlayer.GetComponent<Persona>();

            // check to see if we're in the current flowchart discussion with the player
            if (playerScript.PersonaIsInCurrentFlowchart(this.gameObject))
            {   // tell the player we've clicked on us
                playerScript.OnClickDialog(this.gameObject);
                return;
            }

            // if we're currently interacting with the player
            if (!this.IsPlayer && currentInterlocutor != null)
            {
                print(this.gameObject);
                // force the dialog to pick up where it was (or to start over)
                playerScript.OnClickDialog(this.gameObject);
                return;
            }

            // ok we're not part of the current discussion

            // if there are any listeners
            if (ClickedPersonaListener != null)
            {   // tell the player to come here
                ClickedPersonaListener(this.gameObject);
            }

        }

        #endregion


        #region Dialog

        public void OnClickDialog(GameObject clickedObject)
        {
            // if we currently have a menuDialog active
            if (transform.FindChild("Dialogues/MenuDialog").gameObject.activeSelf)
            {   // players MUST make choice when menuDialog is active
                print("menuDialog still active");
                return;
            }

            // error catching
            if (currentFlowchart == null)
            {
                Debug.LogError("currentFlowchart == null. IsPlayer=" + IsPlayer);
                return;
            }

            // ok, we do NOT have a menuDialog active

            // if there is no current dialogue
            if (!currentFlowchart.HasExecutingBlocks())
            {   
                // if we're still in collision with a Persona
                if (currentInterlocutor != null)
                {
                    // try to force restart that previous dialogue
                    StartFlowchart(currentInterlocutor);
                }
                // whatever the case, leave this method
                return;
            }

            List<GameObject> charactersInFlowchart = GetCharactersInFlowchart(currentFlowchart);

            // if the clicked object isn't even in the current dialog
            if (clickedObject != null && !charactersInFlowchart.Contains(clickedObject))
            {
                Debug.LogWarning("Character " + GetPath(clickedObject.transform) + " isn't in flowchart " + currentFlowchart.name);
                return;
            }

            // go through each persona we're potentially talking to
            foreach (GameObject characterObject in charactersInFlowchart)
            {
                // make sure that object isn't us
                //          if (characterObject == this.gameObject) {
                //              continue;
                //          }
                // get the path to their SayDialog
                SayDialog personaSayDialog = characterObject.GetComponentInChildren<SayDialog>();
                // if this dialog is actually something
                if (personaSayDialog != null)
                {
                    // check to see if that dialog object is active
                    if (personaSayDialog.gameObject.activeSelf)
                    {
                        // ok, push dat button!
                        Button continueButton = personaSayDialog.GetComponentInChildren<Button>();
                        continueButton.onClick.Invoke();
//                        personaSayDialog.continueButton.onClick.Invoke(); // FIXME Fungus Update Bug
                        // all done
                        return;
                    }
                } // if (personaSayDialog)
            } // foreach
        }


        void PlayerStartedDialogueWith(GameObject player, List<GameObject> personae)
        {
            // go through list of personae in this flowchart
            foreach (GameObject persona in personae)
            {   // if we're in this flowchart
                if (persona == this.gameObject)
                {   // do something
                    break;
                }
            }
        }


        void PlayerStoppedDialogueWith(GameObject player, List<GameObject> personae)
        {
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


        #region Collisions Proximity

        /// <summary>
        /// Called whenever another GameObject enters the Proximity trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject</param>

        public void OnProximityEnter(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                Debug.Log(this.gameObject.name + "<Persona>().OnProximityEnter(" + other.name + ")");
            }

            // only register intersections with the player
            if (this.tag != "Player") return;

            // tell the Persona to turn towards us, the Player
            //other.GetComponent<Persona>().TurnTowards(this.gameObject);

            // find the flowchart in this character
            Flowchart flowchart = GetFlowchart(other); 

            // if we found this persona's flowchart
            if (flowchart != null)
            {   
                // check to see if there's a ProximityEnter event waiting for us
                Handler_ProximityEnter proximityEnterEvent = flowchart.GetComponent<Handler_ProximityEnter>();
                // did we find it?
                if (proximityEnterEvent != null)
                {
                    proximityEnterEvent.ExecuteBlock();
                }
            } // if (flowchart)

        }
        // OnProximityEnter


        /// <summary>
        /// Called while another GameObject stays in the Proximity trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public void OnProximityStay(GameObject other)
        {
            // if the logging level is full verbose
            if (collisionLogLevel == NetworkLogLevel.Full)
            {   // log activity
                Debug.Log(this.gameObject.name + "<Persona>().OnProximityStay(" + other.name + ")");
            }

            // only register intersections with the player
            if (this.tag != "Player") return;
        }


        /// <summary>
        /// Called whenever another GameObject exits the Proximity trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public void OnProximityExit(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                Debug.Log(this.gameObject.name + "<Persona>().OnProximityExit(" + other.name + ")");
            }

            // only register intersections with the player
            if (this.tag != "Player") return;

            // find this flowchart in this character
            Flowchart flowchart = GetFlowchart(other);

            // if we found this persona's flowchart
            if (flowchart != null)
            {   
                // check to see if there's a ProximityEnter event waiting for us
                Handler_ProximityExit proximityExitEvent = flowchart.GetComponent<Handler_ProximityExit>();
                // did we find it?
                if (proximityExitEvent != null)
                {
                    proximityExitEvent.ExecuteBlock();
                }
            } // if (flowchart

        }
        // OnProximityExit

        #endregion


        #region Collisions Interaction

        /// <summary>
        /// Called whenever another GameObject enters the Dialog trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public void OnInteractionEnter(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                Debug.Log(this.gameObject.name + "<Persona>().OnInteractionEnter(" + other.name + ")");
            }

            // depending on whether we're the player or not
            if (this.IsPlayer) OnInteractionEnterPlayer(other);
            else if (!this.IsPlayer) OnInteractionEnterPersona(other);
        }


        void OnInteractionEnterPersona(GameObject other)
        {
            // only register intersections with the player
            if (other.tag != "Player") return;

            // make sure we're not already talking to this player
            if (currentInterlocutor == other) return;

            // make sure we're not already talking with someone else
            if (currentInterlocutor != null) return;

            // ok, register this as valid other
            currentInterlocutor = other;
        }


        void OnInteractionEnterPlayer(GameObject other)
        {
            // if we're dead, ingore the rest
            if (Dead) return;

            // if we're the try to go to this Persona
            if (other.tag == "Persona" && other == targetObject)
            {
                // make sure we're not already talking with someone else
                if (currentInterlocutor != null && currentInterlocutor != other) return;

                // turn towards the other
                TurnTowards(other);
                // tell the Persona to turn towards us, the Player
                other.GetComponent<Persona>().TurnTowards(this.gameObject);
                // tell the Persona to stop walking
                other.GetComponent<Persona>().StopWalking();

                // start talking
                // StartFlowchart(other);

                StopWalking();
                // Broadcast that we're reached the target
                // ReachedTarget();

                return;

            }

            // if the other is targeting us
            if (other.tag == "Persona" && other.GetComponent<Persona>().Target == this.gameObject)
            { 
                // stop walking
                StopWalking();
                // stop wherever we were going and target them too
                //ClickedPersona(other);
                // turn towards the other
                //TurnTowards(other);
                // tell the Persona to turn towards us, the Player
                //other.GetComponent<Persona>().TurnTowards(this.gameObject);
                // tell the Persona to stop walking
                other.GetComponent<Persona>().StopWalking();
                // ok to start targeting them
                return;
            }

            // if we're touching the TouchTarget && we're at the end
            if (other.tag == "TouchTarget")
            {
                // get rid of the TouchTarget
                Destroy(other);
                // Broadcast that we're reached the target
                ReachedTarget();

                return;
            }
        }


        /// <summary>
        /// Called while another GameObject stays in the Dialog trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public void OnInteractionStay(GameObject other)
        {
            // if the logging level is full verbose
            if (collisionLogLevel == NetworkLogLevel.Full)
            {   // log activity
                print(this.gameObject.name + "<Persona>().OnInteractionStay(" + other.name + ")");
            }

            if (IsPlayer)
            {
                // if we're dead, ingore the rest
                if (Dead) return;

                // if we're targeting another Persona or this Persona is targeting us
                if (other.tag == "Persona" && (other == targetObject || other.GetComponent<Persona>().Target == this.gameObject))
                {
                    // if everyone's stopped walking
                    if (!Walking && !other.GetComponent<Persona>().Walking)
                    {
                        // make sure we're not already talking to this person
                        if (currentInterlocutor == null && currentInterlocutor != other)
                        {
                            // start talking
                            StartFlowchart(other);
                            // stop current movement
                            ReachedTarget();
                        }
                    }
                }
            } // if (IsPlayer
        }


        /// <summary>
        /// Called whenever another GameObject exits the Dialog trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public void OnInteractionExit(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                print(this.gameObject.name + "<Persona>().OnInteractionExit(" + other.name + ")");
            }

            if (this.IsPlayer) OnInteractionExitPlayer(other);
            else if (!this.IsPlayer) OnInteractionExitPersona(other);
        }

        void OnInteractionExitPersona(GameObject other)
        {
            // make sure this is the actual person we were interacting with
            if (other == currentInterlocutor)
            {   
                currentInterlocutor = null;
            }
        }


        void OnInteractionExitPlayer(GameObject other)
        {
            // if we're dead, ignore the rest
            if (Dead) return;

            // ignore anyone who is not a Persona
            if (IsPlayer && other.tag != "Persona")
            {
                return;
            }

            // make sure this is the actual person we were interacting with
            if (other != currentInterlocutor)
            {
                return;
            }

            // FIXME: When slowing down to a stop, Personae disrupt the dialog they have just initiated

            StopCurrentFlowchart();

            currentInterlocutor = null;
            currentFlowchart = null;
        }

        #endregion


        #region Collisions Physics

        // FIXME: Physicals collisions don't currently work right with discrete collider child objects (!?)

        void OnCollisionEnter(Collision impact)
        {
            // if we're the player and we've been impacted by a mortal object
            if (IsPlayer && impact.gameObject.tag == "Mortal")
            {   // start the physical impact code
                OnPhysicalEnter(impact);
            }
        }

        void OnCollisionStay(Collision impact)
        {
            // if we're the player and we've been impacted by a mortal object
            if (IsPlayer && impact.gameObject.tag == "Mortal")
            {   // start the physical impact code
                OnPhysicalStay(impact);
            }
        }

        void OnCollisionExit(Collision impact)
        {
            // if we're the player and we've been impacted by a mortal object
            if (IsPlayer && impact.gameObject.tag == "Mortal")
            {   // start the physical impact code
                OnPhysicalExit(impact);
            }
        }

        /// <summary>
        /// Called whenever another GameObject enters into Collision with this Persona
        /// </summary>
        /// <param name="impact">Collider gives info on the collision.</param>
        /// <param name="other">The other GameObject.</param>

        public void OnPhysicalEnter(Collision impact)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                print(this.gameObject.name + "<Persona>().OnCollisionEnter(" + impact + ")");
            }

            // if this is the bullet
            if (impact.gameObject.tag == "Mortal")
            {
                // get the first contact point
                ContactPoint contact = impact.contacts[0];
                // get the rigidbody
                Rigidbody contactBody = contact.thisCollider.attachedRigidbody;
                // get the direction of the bullet
                //Vector3 impactVelocity = contact.normal;
                Vector3 impactVelocity = impact.gameObject.transform.forward.normalized;
                // we hit a specific rigidbody and we'll use the impact normal for calculating force
                Die(contactBody, impactVelocity); //contact.normal);
                // destroy bullet
                Destroy(impact.gameObject);
            }

        }


        /// <summary>
        /// Called while another GameObject stays in Collision with this Persona
        /// </summary>
        /// <param name="impact">Collider gives info on the collision.</param>
        /// <param name="other">The other GameObject.</param>

        public void OnPhysicalStay(Collision impact)
        {
            // if the logging level is full verbose
            if (collisionLogLevel == NetworkLogLevel.Full)
            {   // log activity
                print(this.gameObject.name + "<Persona>().OnCollisionStay(" + impact.gameObject.name + ")");
            }
        }


        /// <summary>
        /// Called whenever another GameObject exits Collision with this Persona
        /// </summary>
        /// <param name="impact">Collider gives info on the collision.</param>
        /// <param name="other">The other GameObject.</param>

        public void OnPhysicalExit(Collision impact)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                print(this.gameObject.name + "<Persona>().OnCollisionExit(" + impact.gameObject.name + ")");
            }
        }

        #endregion



        #region Turn

        public void TurnTowards(GameObject turnTarget)
        {
            StartCoroutine(Turn(turnTarget));
        }


        IEnumerator Turn(GameObject turnTarget)
        {
            // set the walk speed to 0
            animator.SetFloat("Speed", 0.0f);

            // which way do we have to turn?
            float angleDelta = CalculateAngleDelta(this.gameObject, turnTarget);
            // get current angle
            float currentAngle = animator.GetFloat("Turn");

            while (Mathf.Abs(angleDelta) > 10.0f)
            {
                // Create the Low-pass filter for the delta
                float smoothFactor = Mathf.Min(1.0f, Time.deltaTime / 0.1f);
                // change scale of movement
                angleDelta *= 0.1f;
                // get the current angle
                currentAngle = animator.GetFloat("Turn");
                // smooth the angle
                currentAngle = Mathf.Lerp(currentAngle, angleDelta, smoothFactor);
                // if we're close enough
                if (Mathf.Abs(currentAngle) < 0.5f)
                {   // stop rotation
                    currentAngle = Mathf.Lerp(currentAngle, 0.0f, 0.5f);
                }
                // if too small
                if (Mathf.Abs(currentAngle) < 0.05) currentAngle = 0.0f;
                // turn in this direction
                animator.SetFloat("Turn", currentAngle);
                // wait a bit
                yield return new WaitForFixedUpdate();
                // calculate how we did
                angleDelta = CalculateAngleDelta(this.gameObject, turnTarget);
            }

            // stop whatever rotations are taking place
            while (Mathf.Abs(currentAngle) > 0.05f)
            {
                currentAngle *= 0.75f;
                animator.SetFloat("Turn", currentAngle);
                // wait for next frame
                yield return new WaitForFixedUpdate();
            }

//            angleDelta = CalculateAngleDelta(this.gameObject, turnTarget);
//            while (Mathf.Abs(angleDelta) > 1.0f)
//            {
//                //calculate the rotation needed 
//                Quaternion neededRotation = Quaternion.LookRotation(turnTarget.transform.position - transform.position);
//                //use spherical interpollation over time
//                Quaternion interpolatedRotation = Quaternion.Slerp(transform.rotation, neededRotation, Time.deltaTime * rotationSpeed);
//                // apply rotation
//                transform.rotation = interpolatedRotation;
//                // wait a frame
//                yield return new WaitForEndOfFrame();
//            }

            animator.SetFloat("Turn", 0.0f);

        }

        #endregion



        #region Walk

        void TouchedPosition(Vector3 position)
        {
            // dead people don't walk (in this game)
            if (Dead) return;

            WalkToPosition(position);

        }


        public void WalkToPosition(Vector3 position)
        {
            // dead people don't walk (in this game)
            if (Dead) return;

            // snap to ground
            position.y = 0.01f;
            // we're not targetting anyone (just a position)
            targetObject = null;
            targetGoal = position;
            targetGoalIsSet = true;
            navMeshAgent.destination = targetGoal;

        }


        public void TargetPersona(GameObject other)
        {
            // the Persona that we're going to follow
            targetObject = other;
            // their position
            targetGoal = other.transform.position;
            targetGoal.y = 0.01f;
            targetGoalIsSet = true;
            navMeshAgent.destination = targetGoal;
        }


        public void ClearTarget()
        {
            targetObject = null;
        }


        void ClickedPersona(GameObject other)
        {
            // if we're not concerned by this walk command
            if (!this.IsPlayer) return;

            // dead people don't walk (in this game at least)
            if (Dead) return;

            TargetPersona(other);
        }


        void Walk()
        {
            // TODO: calculate walk speed using navMeshAgent values
            float maxWalkSpeed = 1.1f;

            Vector3 worldDeltaPosition = navMeshAgent.nextPosition - transform.position;
            float magnitude = worldDeltaPosition.magnitude * 2.0f;

            float angleDelta = CalculateAngleDelta(this.gameObject, navMeshAgent.nextPosition) * 0.05f;
            float currentAngle = animator.GetFloat("Turn");

            float currentSpeed = animator.GetFloat("Speed");

            // calculate smooth factors
            float turnSmoothFactor = Time.deltaTime * 5.0f;
            float walkSmoothFactor = Time.deltaTime * 4.0f;

            float targetAngle = Mathf.Lerp(currentAngle, angleDelta, turnSmoothFactor);
            targetAngle = Mathf.Clamp(targetAngle, -1.5f, 1.5f);

            float targetSpeed = Mathf.Clamp(magnitude, 0.5f, maxWalkSpeed);

            Vector2 velocity = Vector2.zero;

            // make sure there's enough distance for walking
            if (navMeshAgent.remainingDistance > navMeshAgent.radius)
            {
                velocity.x = targetAngle;
                velocity.y = Mathf.Lerp(currentSpeed, targetSpeed, walkSmoothFactor);
            }
            else
            {
                velocity.x = Mathf.Lerp(currentAngle, 0.0f, turnSmoothFactor);
                velocity.y = Mathf.Lerp(currentSpeed, 0.0f, walkSmoothFactor);
            }

            // Update velocity if delta time is safe
            if (Time.deltaTime > 1e-5f)
            {
                walkVelocity = velocity;
            }

            // Update animation parameters
            animator.SetFloat("Turn", walkVelocity.x);
            animator.SetFloat("Speed", walkVelocity.y);

            // look in the direction we're walking
            LookAt lookAtScript = GetComponent<LookAt>();
            if (lookAtScript != null)
            {
                lookAtScript.lookAtTargetPosition = navMeshAgent.steeringTarget + transform.forward;
            }

            SnapAgentToPosition(0.9f);

        }

        public void StopWalking()
        {
            if (Dead) return;

            // remember that this is the new target
            targetGoal = transform.position;
            // stop the animation controller
            navMeshAgent.ResetPath();
            // broadcast that we're at the new target
            BroadcastDistance(0.0f);
            // we no longer have a targetGoal
            targetGoalIsSet = false;

            SnapAgentToPosition();

            StartCoroutine("SlowToStop");

        }


        IEnumerator SlowToStop()
        {

            float speed = animator.GetFloat("Speed");
            while (speed > 0.05f)
            {
                yield return new WaitForEndOfFrame();
                speed = Mathf.Lerp(speed, 0.0f, 0.05f);
                animator.SetFloat("Speed", speed);
            }
            animator.SetFloat("Speed", 0.0f);
            animator.SetFloat("Turn", 0.0f);

            SnapAgentToPosition();
        }


        bool IsAtDestination()
        {

            if (navMeshAgent.pathPending)
            {
                return true;
            }

            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }

            return false;

        }


        void BroadcastDistance(float distance)
        {

            // if there are listeners
            if (targetGoalIsSet && MovedListener != null)
            {   // broadcast movement change
                MovedListener(distance);
            }

            // make sure there are listeners
            if (this.IsPlayer && distance == 0.0f && ReachedTargetListener != null)
            {   // broadcast that we've reached the target
                ReachedTargetListener();
            }

        }


        void ReachedTarget()
        {
            // stop current movement
            StopWalking();

            // better annul any inevitable touch targets
            // make sure there are listeners
            if (this.IsPlayer && ReachedTargetListener != null)
            {   // broadcast that we've reached the target
                ReachedTargetListener();
            }
            // 
            BroadcastDistance(0.0f);

        }

        #endregion



        #region Animation

        void OnAnimatorMove()
        {
            if (Dead) return;

            // get animator rotation
            Quaternion targetRotation = animator.rootRotation;
            // smooth the rotation transition a little
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 50.0f);

            // get animator position
            Vector3 position = animator.rootPosition;
            position.y = navMeshAgent.nextPosition.y;
            transform.position = position;

            if (Walking)
            {   // broadcast our postition
                Vector3 deltaPosition = transform.position - targetGoal;
                BroadcastDistance(deltaPosition.sqrMagnitude);
            }
        }


        void SnapAgentToPosition(float easing = 1.0f)
        {
            Vector3 worldDeltaPosition = navMeshAgent.nextPosition - transform.position;
            // Pull agent towards character
            if (worldDeltaPosition.magnitude > navMeshAgent.radius)
            {
                navMeshAgent.nextPosition = transform.position + (easing * worldDeltaPosition);
            }
        }

        #endregion


        #region Die

        void Die()
        {   
            // if we don't contain a Ragdoll, we can't die
            if (!containsRagdoll) return;
            // note that we're no longer alive
            alive = false;
            // if there are any listeners
            if (PersonaDiedListener != null)
            {   // tell them that we've died
                PersonaDiedListener(this.gameObject);
            }
            // tell the player they're dead
            Flowchart thisFlowchart = GetComponent<Flowchart>();
            if (thisFlowchart == null)
            {
                thisFlowchart = GetComponentInChildren<Flowchart>();
            }
            // make sure we found it
            if (thisFlowchart != null)
            {
                thisFlowchart.SendFungusMessage("Die");
                // announce our death
                Invoke("DeathAnnouncement", 5.0f);
            }
            else
            {
                Debug.LogError("Flowchart not found during death sequence");
            }
        }


        void Die(Rigidbody hitBodypart, Vector3 hitVector)
        {
            // first, convert into a ragdoll
            Die();
            // apply the force
            StartCoroutine(ApplyImpact(hitBodypart, hitVector));
        }


        void DeathAnnouncement()
        {
            Fungus.Flowchart.BroadcastFungusMessage("GameOver");
        }


        IEnumerator ApplyImpact(Rigidbody hitBodypart, Vector3 hitVector, float duration = 0.25f)
        {
            // FIXME: For some reason this is still being called after our death  *:-<
            if (hitBodypart != null)
            {   
                // if this rigidbody is kinematic
                if (hitBodypart.isKinematic)
                {   // try to find a dynamic part
                    Rigidbody[] bodyparts = hitBodypart.gameObject.GetComponentsInChildren<Rigidbody>();
                    // go through each child rigidbody
                    foreach (Rigidbody childBodyPart in bodyparts)
                    {   // if this is one of the ragdoll bodyparts
                        if (!childBodyPart.isKinematic)
                        {   // this is our new target
                            hitBodypart = childBodyPart;
                            // all done
                            break;
                        }
                    }
                }
                float startTime = Time.time;
                Vector3 hitForce = hitVector.normalized * 2.0f;
                hitForce.y = 0.0f;

                while (Time.time - startTime < duration)
                {
                    hitBodypart.AddForce(hitForce, ForceMode.VelocityChange);
                    yield return new WaitForFixedUpdate();
                }
            }
        }

        #endregion


        #region Flowchart

        Flowchart GetFlowchart(GameObject gameObject)
        {
            // first try to find the flowchart in this GameObject
            Flowchart flowchart = gameObject.GetComponent<Flowchart>();
            // if it's not there
            if (flowchart == null)
            {
                // check in a child object
                flowchart = gameObject.GetComponentInChildren<Flowchart>();
            }
            return flowchart;
        }


        GameObject ExtractRootParentFrom(Flowchart flowchart)
        {
            return ExtractRootParentFrom(flowchart.gameObject);
        }


        GameObject ExtractRootParentFrom(GameObject theObject)
        {
            // try to get the player GameObject from this Flowchart's GameObject
            Persona personaScript = theObject.GetComponent<Persona>();
            // if it wasn't there
            if (personaScript == null)
            {
                // try to get the Persona from this flowchart's parents
                personaScript = theObject.GetComponentInParent<Persona>();
            }
            // if we found the Persona component
            if (personaScript != null)
            {   // return it's GameObject
                return personaScript.gameObject;
            }
            // couldn't find it (this is an error
            Debug.LogError("Couldn't find root parent object");
            return null;
        }


        void StartedDialogue(Flowchart flowchart)
        {
            // get a list of all the characters in this flowchart
            List<GameObject> personae = GetCharactersInFlowchart(flowchart);
            // if there are any listeners
            if (StartedDialogueWithListener != null)
            {   // tell them all the objects we've started talking to
                StartedDialogueWithListener(this.gameObject, personae);
            }
        }


        void StoppedDialogue(Flowchart flowchart)
        {

            // get a list of all the characters in this flowchart
            List<GameObject> personae = GetCharactersInFlowchart(flowchart);
            // if there are any listeners
            if (StoppedDialogueWithListener != null)
            {   // tell them all the objects we've stopped discussing with
                StoppedDialogueWithListener(this.gameObject, personae);
            }

        }

        /// <summary>
        /// Starts the flowchart.
        /// </summary>
        /// <param name="other">The other GameObject we're dialoguing with.</param>

        void StartFlowchart(GameObject other)
        {
            // find this flowchart in this character
            Flowchart flowchart = GetFlowchart(other.gameObject);

            // if we found this persona's flowchart
            if (flowchart != null)
            {
                // remember who we're interacting with
                currentInterlocutor = other;
                // remember which flowchart we're interacting with
                currentFlowchart = flowchart;
                //                // start this specific flowchart
                //                flowchart.SendFungusMessage("DialogEnter");
                // check to see if there's an InteractionEnter event waiting for us
                Handler_InteractionEnter interactionEnterEvent = currentFlowchart.GetComponent<Handler_InteractionEnter>();
                // did we find it?
                if (interactionEnterEvent != null)
                {
                    interactionEnterEvent.ExecuteBlock();
                }
                // Started a dialog using this flowchart
                StartedDialogue(flowchart);
            }

        }


        /// <summary>
        /// Stops the current flowchart and informs all concerned characters.
        /// </summary>

        void StopCurrentFlowchart()
        {

            if (currentFlowchart != null)
            {
                // Stopped a dialog with a Persona
                StoppedDialogue(currentFlowchart);
                // turn off Flowchart
                currentFlowchart.StopAllBlocks();
                currentFlowchart.StopAllCoroutines();
                // check to see if there's an InteractionEnter event waiting for us
                Handler_InteractionExit interactionExitEvent = currentFlowchart.GetComponent<Handler_InteractionExit>();
                // did we find it?
                if (interactionExitEvent != null)
                {
                    interactionExitEvent.ExecuteBlock();
                }
            }

            // hide any possible menus of our own
            Transform playerSayTransform = transform.FindChild("Dialogues/SayDialog");

            if (playerSayTransform != null)
            {
                playerSayTransform.gameObject.SetActive(false);
            }

            Transform playerMenuTransform = transform.FindChild("Dialogues/MenuDialog");

            // FIXME: This is ugly
            if (playerMenuTransform != null)
            {
                TurnOffPlayerMenu(playerMenuTransform.gameObject);
            }

        }


        /// <summary>
        /// Turns the off player menu (if any of its items is on)
        /// </summary>
        /// <param name="playerMenu">The Player menu GameObject.</param>

        void TurnOffPlayerMenu(GameObject playerMenu)
        {
            // make sure we actually have a player menu to turn off
            if (playerMenu == null) return;
            // get all the sliders
            Slider[] sliders = playerMenu.GetComponentsInChildren<Slider>();
            foreach (Slider slider in sliders)
            {
                // turn off sliders, if any
                if (slider.gameObject.activeInHierarchy)
                {
                    slider.gameObject.SetActive(false);
                }
            }
            // turn off optional buttons, if any
            Button[] buttons = playerMenu.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                if (button.gameObject.activeInHierarchy)
                {
                    button.gameObject.SetActive(false);
                }
            }
            // then turn off the menu itself, if neccesary
            if (playerMenu.activeInHierarchy) playerMenu.SetActive(false);
        }


        /// <summary>
        /// Determines whether this Persona GameObject is in the current flowchart.
        /// </summary>
        /// <returns><c>true</c> if the Persona GameObject is in current flowchart; otherwise, <c>false</c>.</returns>
        /// <param name="persona">Persona GameObject.</param>

        public bool PersonaIsInCurrentFlowchart(GameObject persona)
        {
            // if this is me, ignore this request
            if (persona == this.gameObject)
            {
                return false;
            }

            // if there's no current persona I'm interacting with
            if (currentInterlocutor == null)
            {
                return false;
            }

            Flowchart flowchart = null;

            // if there is a current flowchart
            if (currentFlowchart != null)
            {   // use the current flowchart for the test
                flowchart = currentFlowchart;
            }
            else
            {
                // try to extract a flowchart from the current persona
                flowchart = GetFlowchart(currentInterlocutor);
            }

            // still no flowchart? 
            if (flowchart == null)
            {   // COMPUTER SAYS NO
                return false;
            }

            // ok, so we've got a flowchart, who's in it?
            List<GameObject> characters = GetCharactersInFlowchart(currentFlowchart);

            // is this character in it?
            if (characters.Contains(persona))
            {
                return true;
            }

            // if we're here, then the answer is no
            return false;

        }


        /// <summary>
        /// Go into the Flowchart and figure out which characters are referenced there.
        /// </summary>
        /// <returns>A list of the GameObjects of characters in the flowchart.</returns>
        /// <param name="flowchart">The Flowchart.</param>

        List<GameObject> GetCharactersInFlowchart(Flowchart flowchart)
        {

            List<GameObject> possiblePersonaObjects = new List<GameObject>();

            if (flowchart == null)
            {
                Debug.LogError("Flowchart == null");
                return possiblePersonaObjects;
            }

            // FIXME: This doesn't work when there is no executing block
            // if we have a currently executing block
            List<Block> blocks = flowchart.GetExecutingBlocks();

            // FIXME: For some reason we now have to add ourselves to the list
            GameObject flowChartRootParent = ExtractRootParentFrom(flowchart);
            if (flowChartRootParent) possiblePersonaObjects.Add(flowChartRootParent);

            // FIXME: Fungus Update bug

            // go through each executing block
            foreach (Block block in blocks)
            {
                // get the command list
//                List<Command> commands = block.commandList;
                List<Command> commands = block.CommandList;
                // go through the command list
                foreach (Command command in commands)
                {
                    // if this is a say command
                    if (command.GetType().ToString() == "Fungus.Say")
                    {
                        // force type to Say
                        Say sayCommand = (Say)command;
                        // get the gameobject attached to this character
                        if (sayCommand == null)
                        {
                            Debug.LogError("sayCommand == null");
                            continue;
                        }
                        // GameObject persona = sayCommand.character.gameObject.transform.parent.gameObject;
//                        GameObject persona = ExtractRootParentFrom(sayCommand.character.gameObject);
                        GameObject persona = ExtractRootParentFrom(sayCommand._Character.gameObject);

                        // if this one isn't already in the list
                        if (!possiblePersonaObjects.Contains(persona))
                        {
                            // add it to the list of possible people we're talking to
                            possiblePersonaObjects.Add(persona);
                        }

                    } // if type
                } // foreach Command
            } // foreach(Block

            // if this list doesn't contain the player
            if (!possiblePersonaObjects.Contains(this.gameObject))
            {
                //          print("Force-add Player");
                possiblePersonaObjects.Add(this.gameObject);
            }

            return possiblePersonaObjects;

        }

        #endregion


        #region Tools

        /// <summary>
        /// Calculates the (signed) angle object A needs to turn delta to point towards object B.
        /// Code by Ben Pitt.
        /// http://answers.unity3d.com/answers/26791/view.html
        /// </summary>
        /// <returns>The angle delta.</returns>
        /// <param name="objectA">The object that needs to turn.</param>
        /// <param name="objectB">The object it needs to turn toward.</param>

        float CalculateAngleDelta(GameObject objectA, GameObject objectB)
        {
            return CalculateAngleDelta(objectA, objectB.transform.position);
        }


        /// <summary>
        /// Calculates the (signed) angle object A needs to turn delta to point towards the target position.
        /// Code by Ben Pitt.
        /// http://answers.unity3d.com/answers/26791/view.html
        /// </summary>
        /// <returns>The angle delta.</returns>
        /// <param name="objectA">The object that needs to turn.</param>
        /// <param name="target">The target position.</param>

        float CalculateAngleDelta(GameObject objectA, Vector3 target)
        {
            // get the delta of these two positions
            Vector3 deltaVector = (target - objectA.transform.position).normalized;
            // no zero divisions
            if (Vector3.zero == deltaVector) return 0.0f;
            // create a rotation looking in that direction
            Quaternion lookRotation = Quaternion.LookRotation(deltaVector);
            // get a "forward vector" for each rotation
            Vector3 forwardA = objectA.transform.rotation * Vector3.forward;
            Vector3 forwardB = lookRotation * Vector3.forward;
            // get a numeric angle for each vector, on the X-Z plane (relative to world forward)
            float angleA = Mathf.Atan2(forwardA.x, forwardA.z) * Mathf.Rad2Deg;
            float angleB = Mathf.Atan2(forwardB.x, forwardB.z) * Mathf.Rad2Deg;
            // get the signed difference in these angles
            float angleDifference = Mathf.DeltaAngle(angleA, angleB);

            return angleDifference;

        }


        /// <summary>
        /// For Debugging purposes. Creates a string containing the transform heirarchy to a specific Transform component.
        /// </summary>
        /// <returns>The complete Transform path to this specific Transform.</returns>
        /// <param name="current">The Transform we want to know the path to.</param>

        static string GetPath(Transform current)
        {
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
        static string GetPath(GameObject obj)
        {
            return GetPath(obj.transform);
        }

        #endregion
     
    }
    // class Persona

}
// namespace Fungus3D
