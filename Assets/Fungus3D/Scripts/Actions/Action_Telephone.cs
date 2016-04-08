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

    public class Action_Telephone : Action_Command
    {
        #region Members

        // whether we should be sitting or not
        public bool calling = false;

        #endregion


        #region Action

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

        #endregion


        #region Summary

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

        #endregion

    }

}
