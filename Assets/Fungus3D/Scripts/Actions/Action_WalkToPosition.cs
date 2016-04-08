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

    public class Action_WalkToPosition : Action_Command
    {
        #region Members

        // where the Persona walks to
        public GameObject targetObject;

        #endregion


        #region Action

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

        #endregion


        #region Summary

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

        #endregion

    }

}
