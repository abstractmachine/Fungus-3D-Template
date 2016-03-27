using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;
using System.Collections.Generic;

using Fungus;

namespace Fungus3D
{

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]

    // Note: Persona is the base class for Player

    public class Persona : MonoBehaviour
    {

        #region Variables

        [Tooltip("How verbose do we want the logs to be?")] 
        public NetworkLogLevel collisionLogLevel = NetworkLogLevel.Off;

        // are we alive?
        protected bool alive = true;
        // are we a/the player?
        protected bool isPlayer = false;
        // has the ragdoll been configured for this persona?
        bool containsRagdoll = false;
        // the current persona we're talking to
        protected GameObject interlocutor = null;
        // access to (required) components
        protected Animator animator;
        protected NavMeshAgent navMeshAgent;

        #endregion


        #region Accessors

        public bool IsPlayer { get { return isPlayer; } }
        public bool Walking { get { return navMeshAgent.velocity.sqrMagnitude > 0.01f; } }
        public bool Dead { get { return !alive; } }
        public bool Alive { get { return alive; } }

        #endregion



        #region Event Delegates

        public delegate void ClickedOnPersonaDelegate(GameObject persona);

        public static event ClickedOnPersonaDelegate ClickedOnPersona;

        public delegate void GoToPersonaDelegate(GameObject persona);

        public static event GoToPersonaDelegate GoToPersona;

        public delegate bool PersonaIsInCurrentFlowchartDelegate(GameObject persona);

        public static event PersonaIsInCurrentFlowchartDelegate PersonaIsInCurrentFlowchart;

        public delegate void PersonaDiedDelegate(GameObject player);

        public static event PersonaDiedDelegate PersonaDied;

        #endregion



        #region Listeners

        void OnEnable()
        {
            Player.PlayerStartedDialogueWith += PlayerStartedDialogueWith;
            Player.PlayerStoppedDialogueWith += PlayerStoppedDialogueWith;
        }


        void OnDisable()
        {
            Player.PlayerStartedDialogueWith -= PlayerStartedDialogueWith;
            Player.PlayerStoppedDialogueWith += PlayerStoppedDialogueWith;
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



        #region Init

        protected virtual void Start()
        {
            // load the required componenets
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            // has the ragdoll been configured
            if (GetComponent<Ragdoll>() != null)
            {
                containsRagdoll = true;
            }
            // make sure all dependencies are functional
            CheckForDependencies();
        }

        void CheckForDependencies() {
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



        #region Loop

        void Update() {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Die();
            }
        }

        #endregion



        #region Interaction

        /// <summary>
        /// When clicking on this Persona
        /// Note: This requires a physics Raycaster on a camera
        /// </summary>

        public virtual void OnClick()
        {
            // get access to player
            GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");

            if (playerGameObject == null)
            {
                Debug.LogWarning("Player doesn't exist!");
                return;
            }

            // FIXME: Theoretically, the player shouldn't always have to be in the flowchart

            // check to see if there's a listener for responding to flowchart inclusion test 
            if (PersonaIsInCurrentFlowchart != null)
            {   // check to see if we're in the current flowchart discussion with the player
                if (PersonaIsInCurrentFlowchart(this.gameObject))
                {       // make sure someone's listening
                    if (ClickedOnPersona != null)
                    {   // click on the player
                        ClickedOnPersona(this.gameObject);
                    }
                }
            }

            // if we're currently talking to the player
            if (interlocutor != null)
            {
                // make sure someone's listening
                if (ClickedOnPersona != null)
                {   // click on the player
                    ClickedOnPersona(this.gameObject);
                }
                return;
            }

            // ok we're not part of the current discussion

            // if there are any listeners
            if (GoToPersona != null)
            {   // tell the player to come here
                GoToPersona(this.gameObject);
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

            // find this flowchart in this character
            Flowchart flowchart = GetFlowchart(other); 

            // if we found this persona's flowchart
            if (flowchart != null)
            {   // tell this flowchart we've entered into its space
                flowchart.SendFungusMessage("ProximityEnter");
            }
        }


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
            {   // tell this flowchart we've entered into its space
                flowchart.SendFungusMessage("ProximityExit");
            }
        }


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

            // only register intersections with the player
            if (other.tag != "Player") return;
                
            // make sure we're not already talking to this player
            if (interlocutor == other) return;

            // make sure we're not already talking with someone else
            if (interlocutor != null) return;

            // ok, register this as valid other
            interlocutor = other;
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
            if (other == interlocutor)
            {
                interlocutor = null;
            }
        }

        // TODO: Collisions don't fire (!?)

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
                //Vector3 velocity = contact.normal;
                Vector3 velocity = impact.gameObject.transform.forward.normalized;
                // we hit a specific rigidbody and we'll use the impact normal for calculating force
                Die(contactBody, velocity); //contact.normal);
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

            //            print("OnCollisionStay " + impact.gameObject.name);
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

            //            print("OnCollisionStay " + impact.gameObject.name);
        }

        #endregion



        #region Turn

        public void TurnTowards(GameObject target)
        {
            StartCoroutine(Turn(target));
        }


        IEnumerator Turn(GameObject target)
        {

            // which way do we have to turn?
            float angleDelta = CalculateAngleDelta(this.gameObject, target);

            // if we need to turn to the left
            if (angleDelta < -10)
            {
                // start turning left
                animator.SetBool("TurnLeft", true);
                // wait for us to get close enough
                while (angleDelta < -15)
                {
                    // update angle delta
                    angleDelta = CalculateAngleDelta(this.gameObject, target);
                    // calculate speed
                    float speed = 1.0f + Mathf.Abs(angleDelta * 0.005f);
                    // speed up the faster we are from the target angle
                    animator.speed = speed;
                    // wait for the next frame
                    yield return new WaitForEndOfFrame();
                }
                // stop turning left
                animator.SetBool("TurnLeft", false);
                // set speed back to normal (1)
                animator.speed = 1.0f;
            }
            // if we need to turn to the right
            else if (angleDelta > 10)
            {
                // start turning right
                animator.SetBool("TurnRight", true);
                // wait for us to get close enough
                while (angleDelta > 15)
                {
                    // update angle delta
                    angleDelta = CalculateAngleDelta(this.gameObject, target);
                    // calculate speed
                    float speed = 1.0f + Mathf.Abs(angleDelta * 0.005f);
                    // speed up the faster we are from the target angle
                    animator.speed = speed;
                    // wait for the next frame
                    yield return new WaitForEndOfFrame();
                }
                // stop turning right
                animator.SetBool("TurnRight", false);
                // set speed back to normal (1)
                animator.speed = 1.0f;
            }

            // now force a final turn directly towards that gameObject
            float rotationSpeed = 15.0f;

            for (float countdown = 1.0f; countdown >= 0.0f; countdown -= Time.deltaTime)
            {
                //calculate the rotation needed 
                Quaternion neededRotation = Quaternion.LookRotation(target.transform.position - transform.position);

                //use spherical interpollation over time
                Quaternion interpolatedRotation = Quaternion.Slerp(transform.rotation, neededRotation, Time.deltaTime * rotationSpeed);

                transform.rotation = interpolatedRotation;

                yield return new WaitForEndOfFrame();
            }

            yield return null;


        }

        #endregion


        #region Die

        void Die()
        {   // if we don't contain a Ragdoll, we can't die
            if (!containsRagdoll) return;
            // note that we're no longer alive
            alive = false;
            // if there are any listeners
            if (PersonaDied != null)
            {   // tell them that we've died
                PersonaDied(this.gameObject);
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

        #endregion


        #region Tools

        /// <summary>
        /// Calculates the direction object A needs to turn delta to point towards object B.
        /// Code by Ben Pitt.
        /// http://answers.unity3d.com/answers/26791/view.html
        /// </summary>
        /// <returns>The angle delta as a signed float (-180° to 180°).</returns>
        /// <param name="objectA">Object A.</param>
        /// <param name="objectB">Object B.</param>

        float CalculateAngleDelta(GameObject objectA, GameObject objectB)
        {

            // get the delta of these two positions
            Vector3 deltaVector = (objectB.transform.position - objectA.transform.position).normalized;
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

        #endregion
     


    }
    // class Persona

}
// namespace Fungus3D
