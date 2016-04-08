using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    // declare this command in Fungus
    [CommandInfo("Action", "Wave", "Wave at the Player")]

    /// <summary>
    /// The Wave Command for Fungus
    /// </summary>

    public class Action_Wave : Action_Command
    {
        #region Action

        /// <summary>
        /// This code is executed when the command fires.
        /// </summary>

        public override void OnEnter()
        {
            // tell this character wave at the Player
            animator.SetTrigger("Wave");
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
            if (actor != null)
            {
                return actor.name + " Waves arm.";
            }
            // display the name of the target
            return "Wave arm";
        }

        #endregion

    }

}
