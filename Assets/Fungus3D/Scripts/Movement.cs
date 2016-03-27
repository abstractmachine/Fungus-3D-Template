using UnityEngine;
using System.Collections;

namespace Fungus3D
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]

    public class Movement : MonoBehaviour
    {
        /*

        #region Enum

        //Possible states of the ragdoll
        enum MovementState {
            undefined,
            // not yet defined
            navMeshAgent,
            // NavMesh & Mecanim are in control. Joystick Controller turned off
            blendToAnim,
            // NavMesh & Mecanim in control, but LateUpdate() is used to partially blend in the last ragdolled pose
            ragdolled,
            // Physics controls the ragdoll. Mecanim & Joystick Controller turned off
            controller
            // Joystick Controller & Mecanim in control
        };

        #endregion


        #region Fields

        [SerializeField] float movingTurnSpeed = 360;
        [SerializeField] float stationaryTurnSpeed = 180;
        [SerializeField] float moveSpeedMultiplier = 0.5f;
        [SerializeField] float animSpeedMultiplier = 0.9f;

        #endregion


        #region Members

        bool alive = true;

        GameObject targetObject;
        Vector3 goal;
        bool goalSet = false;

        //The current state
        MovementState movementState = MovementState.undefined;

        const float k_Half = 0.5f;
        float turnAmount;
        float forwardAmount;

        Vector2 smoothDeltaPosition = Vector2.zero;
        Vector2 velocity = Vector2.zero;

        Transform cameraTransform;
        NavMeshAgent navMeshAgent;
        Animator animator;
        Rigidbody body;

        #endregion



        #region Get/Set

        bool Walking { get { return navMeshAgent.velocity.sqrMagnitude > 0.01f; } }

        bool Dead { get { return !alive; } }

        #endregion



        #region Event Listeners

        void OnEnable()
        {
            Persona.GoToPersona += GoToPersona;
            Ground.GoToPosition += GoToPosition;
        }


        void OnDisable()
        {
            Persona.GoToPersona -= GoToPersona;
            Ground.GoToPosition -= GoToPosition;
        }

        #endregion


        #region Init

        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody>();

            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            cameraTransform = Camera.main.transform;

            // Don’t update position automatically
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
        }

        #endregion


        #region Loop

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Die();
            }

            if (!Dead)
            {
                UpdateController();
            }

            if (!Dead && movementState == MovementState.navMeshAgent)
            {
                UpdatePosition();
            }
        }

        #endregion


        #region Interaction

        void UpdateController()
        {
            if (Dead)
            {
                return;
            }

            // read inputs
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            bool action = Input.GetButton("Action");
            // did we interact? and we're not yet in the controller state
            if ((h != 0.0f || v != 0.0f || action) && movementState != MovementState.controller)
            {   // we're changing states
                ChangeState(MovementState.controller);
            }

            // calculate camera relative direction to move:
            Vector3 cameraForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 movementVector = v * cameraForward + h * cameraTransform.right;
            // normalize this vector before applying speed
            movementVector.Normalize();
            // adjust movement speed to desired gameplay
            movementVector *= moveSpeedMultiplier;

            // ok, we can move now
            Move(movementVector);
        }

        #endregion


        #region Die

        void Die(Rigidbody hitBodypart, Vector3 hitVector) {

            // first, convert into a ragdoll
            Die();
            // TODO: add force to the bodypart that we hit
            // ...

        }


        void Die() {

            alive = false;

//            // if there are any listeners
//            if (PlayerDied != null)
//            {   // convert into a rigidbody
//                PlayerDied(this.gameObject);
//            }

        }

        #endregion


        #region State Changes

        void ChangeState(MovementState newState)
        {
            switch (newState)
            {
                case MovementState.navMeshAgent:
                    // turn on navMeshAgent
                    navMeshAgent.enabled = true;
                    // turn off rigidbody
                    body.isKinematic = true;
                    break;

                case MovementState.blendToAnim:
                    break;

                case MovementState.controller:
                    // turn off navMeshAgent
                    navMeshAgent.enabled = false;
                    // turn on rigidbody
                    body.isKinematic = false;
                    break;

                case MovementState.ragdolled:
                    break;
            }

            movementState = newState;

        }

        #endregion



        #region NavMesh

        public void GoToPosition(Vector3 position)
        {
            if (Dead)
            {
                return;
            }

            if (movementState != MovementState.navMeshAgent)
            {
                ChangeState(MovementState.navMeshAgent);
            }

            targetObject = null;
            goal = position;
            goalSet = true;
            navMeshAgent.destination = position;

        }


        public void GoToPersona(GameObject other)
        {
            if (Dead)
            {
                return;
            }

            if (movementState != MovementState.navMeshAgent)
            {
                ChangeState(MovementState.navMeshAgent);
            }

            targetObject = other;

            Vector3 position = other.transform.position;
            position.y = 0.01f;
            goal = position;
            goalSet = true;
            navMeshAgent.destination = position;

        }

        #endregion


        #region Movement

        //        public void OnAnimatorMove()
        //        {
        //            // we implement this function to override the default root motion.
        //            // this allows us to modify the positional speed before it's applied.
        //            if (Time.deltaTime > 0)
        //            {
        //                Vector3 v = (animator.deltaPosition * moveSpeedMultiplier) / Time.deltaTime;
        //
        //                // we preserve the existing y part of the current velocity.
        //                v.y = body.velocity.y;
        //                body.velocity = v;
        //            }
        //        }

        void OnAnimatorMove()
        {

            if (Dead)
            {
                return;
            }

            // Update position to agent position
            transform.position = navMeshAgent.nextPosition;

            // Update position based on animation movement using navigation surface height
//            Vector3 position = animator.rootPosition;
//            // position.y = navMeshAgent.nextPosition.y;
//            transform.position = position;

            if (Walking)
            {
//                Vector3 deltaPosition = position - goal;
//                BroadcastDistance(deltaPosition.sqrMagnitude);
            }
        }



        void UpdatePosition()
        {   
            Vector3 worldDeltaPosition = navMeshAgent.nextPosition - transform.position;

            // Map 'worldDeltaPosition' to local space
            float dx = Vector3.Dot (transform.right, worldDeltaPosition);
            float dy = Vector3.Dot (transform.forward, worldDeltaPosition);
            Vector2 deltaPosition = new Vector2 (dx, dy);

            // Low-pass filter the deltaMove
            float smooth = Mathf.Min(1.0f, Time.deltaTime/0.15f);
            smoothDeltaPosition = Vector2.Lerp (smoothDeltaPosition, deltaPosition, smooth);

            turnAmount = Mathf.Atan2(worldDeltaPosition.x, worldDeltaPosition.z);
            forwardAmount = worldDeltaPosition.z;

            // Update velocity if time advances
            if (Time.deltaTime > 1e-5f) velocity = smoothDeltaPosition / Time.deltaTime;

            bool shouldMove = velocity.magnitude > 0.5f && navMeshAgent.remainingDistance > navMeshAgent.radius;

            if (shouldMove)
            {
                animator.SetFloat ("Turn", turnAmount);
                animator.SetFloat ("Forward", forwardAmount);
            }
            else
            {
                animator.SetFloat ("Turn", 0);
                animator.SetFloat ("Forward", 0);
            }

        }



        public void Move(Vector3 move)
        {
            move = transform.InverseTransformDirection(move);

            turnAmount = Mathf.Atan2(move.x, move.z);
            forwardAmount = move.z;

            ApplyExtraTurnRotation();

            UpdateAnimator(move);
        }


        void StopMovement()
        {

            UpdateAnimator(Vector3.zero);

        }


        void UpdateAnimator(Vector3 move)
        {
            // update the animator parameters
            animator.SetFloat("Forward", forwardAmount, 0.1f, Time.deltaTime);
            animator.SetFloat("Turn", turnAmount, 0.1f, Time.deltaTime);

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            if (move.magnitude > 0)
            {
                animator.speed = animSpeedMultiplier;
            }
        }

        void ApplyExtraTurnRotation()
        {
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(stationaryTurnSpeed, movingTurnSpeed, forwardAmount);
            transform.Rotate(0, turnAmount * turnSpeed * Time.deltaTime, 0);
        }

        #endregion
*/

    } // class Movement

} // namespace Fungus3D
