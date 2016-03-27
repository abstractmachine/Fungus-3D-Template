using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fungus;

// code adapted from: https://github.com/nbzeman/Ragdoll

namespace Fungus3D
{

    public class Ragdoll : MonoBehaviour
    {
        #region Enum

        //Possible states of the ragdoll
        enum RagdollState
        {
            animated,
            //Mecanim is fully in control
            ragdolled,
            //Mecanim turned off, physics controls the ragdoll
            blendToAnim
            //Mecanim in control, but LateUpdate() is used to partially blend in the last ragdolled pose
        }

        #endregion


        #region Members

        //The current state
        RagdollState state = RagdollState.animated;

        //How long do we blend when transitioning from ragdolled to animated
        float ragdollToMecanimBlendTime = 0.5f;
        float mecanimToGetUpTransitionTime = 0.05f;

        //A helper variable to store the time when we transitioned from ragdolled to blendToAnim state
        float ragdollingEndTime = -100;

        //Declare a class that will hold useful information for each body part
        public class BodyPart
        {
            public Transform transform;
            public Vector3 storedPosition;
            public Quaternion storedRotation;
        }
        //Additional vectors for storing the pose the ragdoll ended up in.
        Vector3 ragdolledHipPosition, ragdolledHeadPosition, ragdolledFeetPosition;

        //Declare a list of body parts, initialized in Start()
        List<BodyPart> bodyParts = new List<BodyPart>();

        //Declare an Animator member variable, initialized in Start to point to this gameobject's Animator component.
        Animator animator;

        #endregion



        #region Event Listeners

        void OnEnable()
        {
            Persona.PersonaDied += PlayerDied;
        }


        void OnDisable()
        {
            Persona.PersonaDied -= PlayerDied;
        }

        #endregion


        #region Get/Set

        public bool IsARagdoll { get { return state != RagdollState.animated; } }

        #endregion


        #region Init

        // Initialization, first frame of game
        void Start()
        {
            //Set all RigidBodies to kinematic so that they can be controlled with Mecanim
            //and there will be no glitches when transitioning to a ragdoll
            setKinematic(true);
		
            //Find all the transforms in the character, assuming that this script is attached to the root
            Component[] components = GetComponentsInChildren(typeof(Transform));
		
            //For each of the transforms, create a BodyPart instance and store the transform 
            foreach (Component c in components)
            {
                // ignore our colliders
                if (c.tag == "Collider") continue;
                // add the body part
                BodyPart bodyPart = new BodyPart();
                bodyPart.transform = c as Transform;
                bodyParts.Add(bodyPart);
            }
		
            //Store the Animator component
            animator = GetComponent<Animator>();
        }

        #endregion


        #region Change State

        void PlayerDied(GameObject player) {
            
            // activate the ragdoll on this model
            StartRagdoll();
        }

        public void StartRagdoll()
        {   
            // if we're not in an animation state
            if (state != RagdollState.animated)
            {   // no need to transition to ragdoll
                return;
            }
            //Transition from animated to ragdolled
            setKinematic(false); //allow the ragdoll RigidBodies to react to the environment

            animator.enabled = false; //disable animation

            // turn off NavMeshAgent if it exists
            if (GetComponent<NavMeshAgent>() != null) {
                GetComponent<NavMeshAgent>().enabled = false; // disable navigation agent
            }

            // turn off CapsuleCollider if it exists
            if (GetComponent<CapsuleCollider>() != null && GetComponent<CapsuleCollider>().enabled)
            {
                GetComponent<CapsuleCollider>().enabled = false;
            }

            // turn off CapsuleCollider if it exists
            if (GetComponent<Rigidbody>() != null)
            {
                GetComponent<Rigidbody>().isKinematic = true;
            }

            state = RagdollState.ragdolled;

        }

        public void StartMecanim()
        {   // if we're not in a ragdoll state
            if (state != RagdollState.ragdolled)
            {   // no need to transition to mecanim
                return;
            }
            //Transition from ragdolled to animated through the blendToAnim state
            setKinematic(true); //disable gravity etc.
            ragdollingEndTime = Time.time; //store the state change time
            animator.enabled = true; //enable animation
            state = RagdollState.blendToAnim;  

            //Store the ragdolled position for blending
            foreach (BodyPart b in bodyParts)
            {
                b.storedRotation = b.transform.rotation;
                b.storedPosition = b.transform.position;
            }

            //Remember some key positions
            ragdolledFeetPosition = 0.5f * (animator.GetBoneTransform(HumanBodyBones.LeftToes).position + animator.GetBoneTransform(HumanBodyBones.RightToes).position);
            ragdolledHeadPosition = animator.GetBoneTransform(HumanBodyBones.Head).position;
            ragdolledHipPosition = animator.GetBoneTransform(HumanBodyBones.Hips).position;

        }

        #endregion


        #region Transition

        void LateUpdate()
        {
            //Blending from ragdoll back to animated
            if (state == RagdollState.blendToAnim)
            {
                TransitionToAnimator();
            }
        }

        void TransitionToAnimator()
        {

            if (Time.time <= ragdollingEndTime + mecanimToGetUpTransitionTime)
            {
                //If we are waiting for Mecanim to start playing the get up animations, update the root of the mecanim
                //character to the best match with the ragdoll
                Vector3 animatedToRagdolled = ragdolledHipPosition - animator.GetBoneTransform(HumanBodyBones.Hips).position;
                Vector3 newRootPosition = transform.position + animatedToRagdolled;

                //Now cast a ray from the computed position downwards and find the highest hit that does not belong to the character 
                RaycastHit[] hits = Physics.RaycastAll(new Ray(newRootPosition, Vector3.down)); 
                newRootPosition.y = 0;
                foreach (RaycastHit hit in hits)
                {
                    if (!hit.transform.IsChildOf(transform))
                    {
                        newRootPosition.y = Mathf.Max(newRootPosition.y, hit.point.y);
                    }
                }
                transform.position = newRootPosition;

                //Get body orientation in ground plane for both the ragdolled pose and the animated get up pose
                Vector3 ragdolledDirection = ragdolledHeadPosition - ragdolledFeetPosition;
                ragdolledDirection.y = 0;

                Vector3 meanFeetPosition = 0.5f * (animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
                Vector3 animatedDirection = animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPosition;
                animatedDirection.y = 0;

                //Try to match the rotations. Note that we can only rotate around Y axis, as the animated characted must stay upright,
                //hence setting the y components of the vectors to zero. 
                transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragdolledDirection.normalized);
            }
            //compute the ragdoll blend amount in the range 0...1
            float ragdollBlendAmount = 1.0f - (Time.time - ragdollingEndTime - mecanimToGetUpTransitionTime) / ragdollToMecanimBlendTime;
            ragdollBlendAmount = Mathf.Clamp01(ragdollBlendAmount);

            //In LateUpdate(), Mecanim has already updated the body pose according to the animations. 
            //To enable smooth transitioning from a ragdoll to animation, we lerp the position of the hips 
            //and slerp all the rotations towards the ones stored when ending the ragdolling
            foreach (BodyPart b in bodyParts)
            {
                if (b.transform != transform)
                { //this if is to prevent us from modifying the root of the character, only the actual body parts
                    //position is only interpolated for the hips
                    if (b.transform == animator.GetBoneTransform(HumanBodyBones.Hips))
                        b.transform.position = Vector3.Lerp(b.transform.position, b.storedPosition, ragdollBlendAmount);
                    //rotation is interpolated for all body parts
                    b.transform.rotation = Quaternion.Slerp(b.transform.rotation, b.storedRotation, ragdollBlendAmount);
                }
            }

            //if the ragdoll blend amount has decreased to zero, move to animated state
            if (ragdollBlendAmount == 0)
            {
                state = RagdollState.animated;
            }
        }

        #endregion


        #region Tools

        //A helper function to set the isKinematc property of all RigidBodies in the children of the
        //game object that this script is attached to
        void setKinematic(bool newValue)
        {
            
            //Get an array of components that are of type Rigidbody
            Component[] components = GetComponentsInChildren(typeof(Rigidbody));

            //For each of the components in the array, treat the component as a Rigidbody and set its isKinematic property
            foreach (Component c in components)
            {
                (c as Rigidbody).isKinematic = newValue;
            }
        }

        #endregion
	
    } // class Ragdoll

} // namespace Fungus3D