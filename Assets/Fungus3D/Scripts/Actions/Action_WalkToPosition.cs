using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    // declare this command in Fungus
    [CommandInfo("Action", "WalkToPosition", "Start walking to a position")]

    /// <summary>
    /// The WalkTo Command for Fungus
    /// </summary>

    public class Action_WalkToPosition : Command
    {
        // where the Persona walks to
        public GameObject targetObject;
        // who should be sitting
        public GameObject actor;

        Persona personaScript;

        Animator animator;
        NavMeshAgent navMeshAgent;


        void Awake()
        {
            if (actor == null)
            {
                actor = this.GetComponentInParent<Persona>().gameObject;
            }

            // first, get the Persona script
            personaScript = actor.GetComponentInParent<Persona>();
            // make sure we found a Persona
            if (personaScript == null)
            {
                Debug.LogError("Couldn't find parent object");
                return;
            }
            // get the animator
            animator = actor.GetComponentInParent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Couldn't find Animator in parent");
            }
            // get the navMeshAgent
            navMeshAgent = actor.GetComponentInParent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                Debug.LogError("Couldn't find NavMeshAgent in parent");
            }
        }


        /// <summary>
        /// This code is executed when the command fires.
        /// </summary>

        public override void OnEnter()
        {
            // make sure target object is defined
            if (targetObject == null)
            {
                Debug.LogError("Error: Target object undefined");
                return;
            }
            // get the target position
            Vector3 targetPosition = targetObject.transform.position;
            targetPosition.y = 0.0f;
            // tell this character to walk there
            personaScript.WalkToPosition(targetPosition);
            // move on to next Fungus command
            Continue();
        }


        /// <summary>
        /// This is the summary that appears in the list of Fungus block commands
        /// </summary>

        public override string GetSummary()
        {
            // if we haven't configured the target yet
            if (targetObject == null)
            {
                return "Error: Target undefined";
            }

            if (actor != null)
            {
                return actor.name + " Walk to " + targetObject.name;
            }

            // display the name of the target
            return "Walk to " + targetObject.name;
        }


        /// <summary>
        /// Defines the color of this command in the list of Fungus block commands
        /// </summary>

        public override Color GetButtonColor()
        {
            return Action.buttonColor;
        }

    }

}
