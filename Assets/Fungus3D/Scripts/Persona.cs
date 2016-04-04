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
        protected bool alive = true;
        // are we a/the player?
        protected bool isPlayer = false;

        // a static pointer to the current Player
        protected static GameObject currentPlayer = null;

        // has the ragdoll been configured?
        bool containsRagdoll = false;

        // movement
        protected Vector2 walkVelocity = Vector2.zero;

        // targetting
        protected GameObject targetObject;
        protected Vector3 targetGoal;
        protected bool targetGoalIsSet = false;

        // the current persona we're talking to
        protected GameObject currentInterlocutor = null;
        protected Flowchart currentFlowchart = null;

        // access to (required) components
        protected Animator animator;
        protected NavMeshAgent navMeshAgent;

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
        /// <value><c>true</c> if dead; otherwise, <c>false</c>.</value>
        public bool Dead { get { return !alive; } }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Fungus3D.Persona"/> is alive.
        /// </summary>
        /// <value><c>true</c> if alive; otherwise, <c>false</c>.</value>
        public bool Alive { get { return alive; } }

        #endregion



        #region Event Delegates

        public delegate void GoToPersonaDelegate(GameObject persona, GameObject actor);

        public static event GoToPersonaDelegate GoToPersonaListener;

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
            Player.StartedDialogueWithListener += PlayerStartedDialogueWith;
            Player.StoppedDialogueWithListener += PlayerStoppedDialogueWith;
            Persona.GoToPersonaListener += GoToPersona;
            Ground.GoToPositionListener += GoToPosition;
        }


        void OnDisable()
        {
            Player.StartedDialogueWithListener -= PlayerStartedDialogueWith;
            Player.StoppedDialogueWithListener += PlayerStoppedDialogueWith;
            Persona.GoToPersonaListener -= GoToPersona;
            Ground.GoToPositionListener -= GoToPosition;
        }

        #endregion



        #region Init

        protected virtual void Start()
        {
            // get a reference to the NavMeshAgent component
            navMeshAgent = GetComponent<NavMeshAgent>();

            // Don’t update position automatically
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;

            // get a reference to the animator component
            animator = GetComponent<Animator>();

            // has the ragdoll been configured
            if (GetComponent<Ragdoll>() != null)
            {
                containsRagdoll = true;
            }
            // make sure all dependencies are functional
            CheckForDependencies();
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
        }

        #endregion



        #region Interaction

        /// <summary>
        /// Player clicked on this Persona
        /// Note: This requires a physics Raycaster on a camera
        /// </summary>

        public virtual void OnClick()
        {
            // if we're not the player
            if (!this.IsPlayer)
            {
                OnClickPersona();
            }
        }


        void OnClickPersona()
        {

            // FIXME: Theoretically, the player shouldn't always have to be in the flowchart
            // Persona should be able to discuss amongst themselves (cf. The Sims)

            Player playerScript = currentPlayer.GetComponent<Player>();
            // check to see if we're in the current flowchart discussion with the player
            if (playerScript.PersonaIsInCurrentFlowchart(this.gameObject))
            {   // tell the player we've clicked on us
                playerScript.OnClickDialog(this.gameObject);
                return;
            }

            // if we're currently interacting with the player
            if (currentInterlocutor != null)
            {
                print(this.gameObject);
                // force the dialog to pick up where it was (or to start over)
                playerScript.OnClickDialog(this.gameObject);
                return;
            }

            // ok we're not part of the current discussion

            // if there are any listeners
            if (GoToPersonaListener != null)
            {   // tell the player to come here
                GoToPersonaListener(this.gameObject, currentPlayer);
            }

        }

        #endregion



        #region Dialog

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


        #region Collisions

        /// <summary>
        /// Called whenever another GameObject enters the Proximity trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject</param>

        public virtual void OnProximityEnter(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                Debug.Log(this.gameObject.name + "<Persona>().OnProximityEnter(" + other.name + ")");
            }

            // only register intersections with the player
            if (this.tag != "Player") return;

            // tell the Persona to turn towards us, the Player
            other.GetComponent<Persona>().TurnTowards(this.gameObject);

            // find the flowchart in this character
            Flowchart flowchart = GetFlowchart(other); 

            // if we found this persona's flowchart
            if (flowchart != null)
            {   
                // check to see if there's a ProximityEnter event waiting for us
                ProximityEnter proximityEnterEvent = flowchart.GetComponent<ProximityEnter>();
                // did we find it?
                if (proximityEnterEvent != null)
                {
                    proximityEnterEvent.ExecuteBlock();
                }
            } // if (flowchart)

        } // OnProximityEnter


        /// <summary>
        /// Called while another GameObject stays in the Proximity trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public virtual void OnProximityStay(GameObject other)
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

        public virtual void OnProximityExit(GameObject other)
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
                ProximityExit proximityExitEvent = flowchart.GetComponent<ProximityExit>();
                // did we find it?
                if (proximityExitEvent != null)
                {
                    proximityExitEvent.ExecuteBlock();
                }
            } // if (flowchart

        } // OnProximityExit


        /// <summary>
        /// Called whenever another GameObject enters the Dialog trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public virtual void OnInteractionEnter(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                Debug.Log(this.gameObject.name + "<Persona>().OnInteractionEnter(" + other.name + ")");
            }

            if (!IsPlayer) OnInteractionEnterPersona(other);
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


        /// <summary>
        /// Called while another GameObject stays in the Dialog trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public virtual void OnInteractionStay(GameObject other)
        {
            // if the logging level is full verbose
            if (collisionLogLevel == NetworkLogLevel.Full)
            {   // log activity
                print(this.gameObject.name + "<Persona>().OnInteractionStay(" + other.name + ")");
            }

            // do nothing
        }


        /// <summary>
        /// Called whenever another GameObject exits the Dialog trigger of this Persona
        /// </summary>
        /// <param name="other">The other GameObject.</param>

        public virtual void OnInteractionExit(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                print(this.gameObject.name + "<Persona>().OnInteractionExit(" + other.name + ")");
            }

            // make sure this is the actual person we were interacting with
            if (other == currentInterlocutor)
            {   
                currentInterlocutor = null;
            }
        }

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

        public virtual void OnPhysicalEnter(Collision impact)
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

        public virtual void OnPhysicalStay(Collision impact)
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

        public virtual void OnPhysicalExit(Collision impact)
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

        public void GoToPosition(Vector3 position, GameObject actor)
        {
            // dead people don't walk (in this game)
            if (Dead) return;

            // if we're not concerned by this walk command
            if (!IsPlayer && actor != this.gameObject) return;

            targetObject = null;
            targetGoal = position;
            targetGoalIsSet = true;
            navMeshAgent.destination = targetGoal;

        }


        public void GoToPersona(GameObject other, GameObject actor)
        {
            // dead people don't walk (in this game)
            if (Dead) return;

            // if we're not concerned by this walk command
            if (!IsPlayer && actor != this.gameObject) return;

            targetObject = other;

            Vector3 position = other.transform.position;
            position.y = 0.01f;
            targetGoal = position;
            targetGoalIsSet = true;
            navMeshAgent.destination = targetGoal;

        }


        protected void Walk()
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

        protected void StopWalking()
        {
            if (Dead) return;

            // go to where we already are
            targetObject = null;
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
                speed = Mathf.Lerp(speed, 0.0f, 0.1f);
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


        protected void BroadcastDistance(float distance)
        {

            // if there are listeners
            if (targetGoalIsSet && MovedListener != null)
            {   // broadcast movement change
                MovedListener(distance);
            }

            // make sure there are listeners
            if (distance == 0.0f && ReachedTargetListener != null)
            {   // broadcast that we've reached the target
                ReachedTargetListener();
            }

        }


        protected void ReachedTarget()
        {
            // stop current movement
            StopWalking();

            // better annul any inevitable touch targets
            // make sure there are listeners
            if (ReachedTargetListener != null)
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


        protected void SnapAgentToPosition(float easing = 1.0f)
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
            this.gameObject.GetComponentInChildren<Flowchart>().SendFungusMessage("Die");
            // announce our death
            Invoke("DeathAnnouncement", 5.0f);
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

        protected Flowchart GetFlowchart(GameObject gameObject)
        {
            Flowchart flowchart = gameObject.GetComponentInChildren<Flowchart>();
            return flowchart;
        }


        protected GameObject ExtractRootParentFrom(Flowchart flowchart)
        {
            return ExtractRootParentFrom(flowchart.gameObject);
        }


        protected GameObject ExtractRootParentFrom(GameObject theObject)
        {
            // try to get the player GameObject from this flowchart
            Player playerScript = theObject.GetComponentInParent<Player>();
            // if the playerScript is there
            if (playerScript != null)
            {   // add the player to our list
                return playerScript.gameObject;
            }
            else
            {
                // get the Persona GameObject
                Persona personaScript = theObject.GetComponentInParent<Persona>();
                // if the Persona script is there
                if (personaScript != null)
                {
                    return personaScript.gameObject;
                }
            }
            // couldn't find it (this is an error
            Debug.LogError("Couldn't find root parent object");
            return null;
        }

        protected void StartedDialogue(Flowchart flowchart)
        {
            // get a list of all the characters in this flowchart
            List<GameObject> personae = GetCharactersInFlowchart(flowchart);
            // if there are any listeners
            if (StartedDialogueWithListener != null)
            {   // tell them all the objects we've started talking to
                StartedDialogueWithListener(this.gameObject, personae);
            }
        }

        protected void StoppedDialogue(Flowchart flowchart)
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

        protected void StartFlowchart(GameObject other)
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
                InteractionEnter interactionEnterEvent = currentFlowchart.GetComponent<InteractionEnter>();
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

        protected void StopCurrentFlowchart()
        {

            if (currentFlowchart != null)
            {
                // Stopped a dialog with a Persona
                StoppedDialogue(currentFlowchart);
                // turn off Flowchart
                currentFlowchart.GetComponent<Flowchart>().StopAllBlocks();
                currentFlowchart.GetComponent<Flowchart>().StopAllCoroutines();
                // send a stop message to the current flowchart
                //                currentFlowchart.SendFungusMessage("DialogExit");
                // check to see if there's an InteractionEnter event waiting for us
                InteractionExit interactionExitEvent = currentFlowchart.GetComponent<InteractionExit>();
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

        protected void TurnOffPlayerMenu(GameObject playerMenu)
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

        protected List<GameObject> GetCharactersInFlowchart(Flowchart flowchart)
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

            // go through each executing block
            foreach (Block block in blocks)
            {
                // get the command list
                List<Command> commands = block.commandList;
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
                        //                        GameObject persona = sayCommand.character.gameObject.transform.parent.gameObject;
                        GameObject persona = ExtractRootParentFrom(sayCommand.character.gameObject);

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

        protected float CalculateAngleDelta(GameObject objectA, GameObject objectB)
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

        protected float CalculateAngleDelta(GameObject objectA, Vector3 target)
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

        protected static string GetPath(Transform current)
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
        protected static string GetPath(GameObject obj)
        {
            return GetPath(obj.transform);
        }

        #endregion
     
    }
    // class Persona

}
// namespace Fungus3D
