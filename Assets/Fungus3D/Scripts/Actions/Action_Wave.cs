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

    public class Action_Wave : Command
    {
        Animator animator;

        void Start()
        {
            // get the animator
            animator = GetComponentInParent<Animator>();
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
            // tell this character wave at the Player
            animator.SetTrigger("Wave");
            // move on to next Fungus command
            Continue();
        }


        /// <summary>
        /// This is the summary that appears in the list of Fungus block commands
        /// </summary>

        public override string GetSummary()
        {
            // display the name of the target
            return "Wave at the Player";
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
