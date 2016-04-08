using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    // declare this command in Fungus
    [CommandInfo("Action", "Sitting", "Determine whether this GameObject should be sitting (true) or standing (false)")]

    /// <summary>
    /// The WalkTo Command for Fungus
    /// </summary>

    public class Action_Sitting : Action_Command
    {
        #region Struct

        public enum SitType {
            sit, stand
        }

        #endregion


        #region Members

        // whether we should be sitting or not
        public SitType sitOrStand = SitType.sit;

        #endregion


        #region Action

        /// <summary>
        /// This code is executed when the command fires.
        /// </summary>

        public override void OnEnter()
        {
            // tell this character to walk towards this object/persona
            animator.SetBool("Sitting", sitOrStand == SitType.sit);
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
            if (sitOrStand == SitType.sit)
            {
                return name + " sits down";
            }
            else
            {
                return name + " stands up";
            }
        }

        #endregion

    }

}
