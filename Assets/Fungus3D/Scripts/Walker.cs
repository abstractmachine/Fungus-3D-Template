using UnityEngine;
using System.Collections;

namespace Fungus3D
{

    public class Walker : MonoBehaviour
    {

        #region Members

        NavMeshAgent navMeshAgent;
        Animator animator;

        bool targetSet = false;
//        Vector3 targetPosition = Vector3.zero;

        float maxWalkSpeed = 0.45f;
        Vector2 velocity = Vector2.zero;

        #endregion


        #region Event Listeners

        void OnEnable()
        {
            Ground.GoToPositionListener += GoToPosition;
        }


        void OnDisable()
        {
            Ground.GoToPositionListener -= GoToPosition;
        }

        #endregion


        #region Init

        void Start()
        {

            animator = GetComponent<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            // Don’t update position automatically
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;

        }

        #endregion


        #region Movement

        void FixedUpdate()
        {
            // if we're following a touch target
            if (targetSet)
            {   
                Walk();
            }

        }

        void Turn(float angleDelta)
        {
            // Create the Low-pass filter for the delta
            float smoothFactor = Mathf.Min(1.0f, Time.deltaTime / 0.1f);
            // change scale of movement
            angleDelta *= 0.1f;
            // get the current angle
            float currentAngle = animator.GetFloat("Turn");
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
        }


        void Walk()
        {
            Vector3 worldDeltaPosition = navMeshAgent.nextPosition - transform.position;
            float magnitude = worldDeltaPosition.magnitude;

            float angleDelta = CalculateAngleDelta(this.gameObject, navMeshAgent.nextPosition) * 0.05f;
            float currentAngle = animator.GetFloat("Turn");

            float currentSpeed = animator.GetFloat("Speed");

            // Low-pass filter the deltaMove
            float walkSmoothFactor = Mathf.Min(1.0f, Time.deltaTime/0.15f);
            float turnSmoothFactor = Mathf.Min(1.0f, Time.deltaTime * 5.0f);

            float targetAngle = Mathf.Lerp(currentAngle, angleDelta, turnSmoothFactor);
            float targetSpeed = Mathf.Clamp(magnitude, 0.0f, maxWalkSpeed);

            // make sure there's enough distance for walking
            if (magnitude > 0.1f && navMeshAgent.remainingDistance > navMeshAgent.radius)
            {
                velocity.x = targetAngle;
                velocity.y = Mathf.Lerp(currentSpeed, targetSpeed, walkSmoothFactor);
            }
            else
            {
                velocity.x = Mathf.Lerp(currentAngle, 0.0f, turnSmoothFactor);
                velocity.y = Mathf.Lerp(currentSpeed, 0.0f, walkSmoothFactor);
            }

            // Update animation parameters
            animator.SetFloat("Turn", velocity.x);
            animator.SetFloat("Speed", velocity.y);
            
            GetComponent<LookAt>().lookAtTargetPosition = navMeshAgent.steeringTarget + transform.forward;

            // Pull agent towards character
            if (worldDeltaPosition.magnitude > navMeshAgent.radius)
            {
                navMeshAgent.nextPosition = transform.position + (0.9f * worldDeltaPosition);
            }

        }

        #endregion


        #region Mecanim

        void OnAnimatorMove()
        {
            // get animator rotation
            Quaternion targetRotation = animator.rootRotation;
            // smooth the rotation transition a little
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 50.0f);

            // get animator position
            Vector3 position = animator.rootPosition;
            position.y = navMeshAgent.nextPosition.y;
            transform.position = position;
        }

        #endregion


        #region Goto

        void GoToPosition(Vector3 position)
        {
            // force the y axis to the plane
            position.y = 0.0f;
            // remember this position
//            targetPosition = position;
            // indicate that we've set a target
            targetSet = true;
            // tell the navigation system where we want to go
            navMeshAgent.destination = position;
        }

        #endregion


        #region Tools

        protected float CalculateAngleDelta(GameObject objectA, GameObject objectB)
        {
            return CalculateAngleDelta(objectA, objectB.transform.position);
        }

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

        #endregion

    }

}