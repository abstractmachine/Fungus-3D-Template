using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Fungus3D {

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]

    // Subclass Persona in Player (cf. Player)

    public class Persona : MonoBehaviour {



        #region Variables

        public GameObject bulletPrefab;

        GameObject currentPlayer = null;

        Animator animator;
//        NavMeshAgent navMeshAgent;

        #endregion



        #region Event Delegates

        public delegate void ClickedOnPersonaDelegate(GameObject persona);
        public static event ClickedOnPersonaDelegate ClickedOnPersona;

        public delegate void GoToPersonaDelegate(GameObject persona);
        public static event GoToPersonaDelegate GoToPersona;

        public delegate bool PersonaIsInCurrentFlowchartDelegate(GameObject persona);
        public static event PersonaIsInCurrentFlowchartDelegate PersonaIsInCurrentFlowchart;

        #endregion



        #region Listeners

        void OnEnable()
        {
            Player.PlayerStartedDialogueWith += PlayerStartedDialogueWith;
            Player.PlayerStoppedDialogueWith += PlayerStoppedDialogueWith;

            Player.TurnTowards += TurnTowards;
        }


        void OnDisable()
        {
            Player.PlayerStartedDialogueWith -= PlayerStartedDialogueWith;
            Player.PlayerStoppedDialogueWith += PlayerStoppedDialogueWith;

            Player.TurnTowards -= TurnTowards;
        }

        void PlayerStartedDialogueWith(GameObject player, List<GameObject> personae) {
            
            // go through list of personae in this flowchart
            foreach (GameObject persona in personae)
            {   // if we're in this flowchart
                if (persona == this.gameObject)
                {   // do something
                    break;
                }
            }

        }

        void PlayerStoppedDialogueWith(GameObject player, List<GameObject> personae) {
            
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

        void Start() {

//            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

        }

        #endregion



        #region Interaction

        /// <summary>
        /// When clicking on this Persona
        /// </summary>

    	void OnMouseDown() {

    		// get access to player
    		GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");

            if (playerGameObject == null) {
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
    		if (currentPlayer != null) {
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
            {    // tell the player to come here
                GoToPersona(this.gameObject);
            }

    	}

        #endregion


        #region Actions

        public void Aim() {

            // get the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            // aim towards the heart
            Vector3 target = player.transform.position + new Vector3(0f, 1f, 0f);
            // start turning
            StartCoroutine(TurnTowards(target));

        }

        IEnumerator TurnTowards(Vector3 target) {

            float rotationSpeed = 15.0f;

            yield return new WaitForEndOfFrame();
            //transform.LookAt(target);

            for (float countdown = 1.0f; countdown >= 0.0f; countdown -= Time.deltaTime)
            {
                //calculate the rotation needed 
                Quaternion neededRotation = Quaternion.LookRotation(target - transform.position);

                //use spherical interpollation over time
                Quaternion interpolatedRotation = Quaternion.Slerp(transform.rotation, neededRotation, Time.deltaTime * rotationSpeed);

                transform.rotation = interpolatedRotation;

                yield return new WaitForEndOfFrame();
            }

            yield return null;

        }

        public void Shoot() {
            
            // get the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            // creat the bullet
            GameObject bullet = (GameObject)Instantiate(bulletPrefab);
            // put this bullet into the objects parent
            bullet.transform.SetParent(GameObject.Find("Objects").transform);
            bullet.name = "Bullet";
            // move up to the gun level
            bullet.transform.position = this.transform.position + new Vector3(0f, 1.45f, 0f);
            // aim towards the heart
            Vector3 target = player.transform.position + new Vector3(0f, 1f, 0f);
            // turn towards our target
            bullet.transform.LookAt(target);
            // move out to the edge of the gun shaft
            //bullet.transform.Translate(new Vector3(0f, 0f, 1f));

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
            // else we're already close enough
            else {
                yield return null;
            }


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

        float CalculateAngleDelta(GameObject objectA, GameObject objectB) {

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
            float angleDifference = Mathf.DeltaAngle( angleA, angleB );

            return angleDifference;

        }

        #endregion
     


    }

}
