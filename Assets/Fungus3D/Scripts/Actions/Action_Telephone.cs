using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    // declare this command in Fungus
    [CommandInfo("Action", "Telephone", "Determine whether this GameObject should talking on the phone")]

    /// <summary>
    /// The WalkTo Command for Fungus
    /// </summary>

    public class Action_Telephone : Command
    {
        // whether we should be sitting or not
        public bool calling = false;
        // who should be sitting
        public GameObject actor;

        Animator animator;
        NavMeshAgent navMeshAgent;


        void Start()
        {
            if (actor == null)
            {
                actor = this.GetComponentInParent<Persona>().gameObject;
            }

            // get the animator
            animator = actor.GetComponentInParent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Couldn't find Animator in parent");
            }
        }


        /// <summary>
        /// This code is executed when the command fires.
        /// </summary>

        public override void OnEnter()
        {
            // tell this character to walk towards this object/persona
            animator.SetBool("Calling", calling);
            // move on to next Fungus command
            Continue();
        }


        /// <summary>
        /// This is the summary that appears in the list of Fungus block commands
        /// </summary>

        public override string GetSummary()
        {
            string name = gameObject.GetComponentInParent<Animator>().gameObject.name;

            if (actor != null)
            {
                name = actor.name;
            }

            // if we haven't configured the target yet
            if (calling)
            {
                return name + " Talking on the phone.";
            }
            else
            {
                return name + " Hung up the phone.";
            }
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
