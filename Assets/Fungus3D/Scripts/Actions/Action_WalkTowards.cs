using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    // declare this command in Fungus
    [CommandInfo("Action", "WalkTowards", "Start following someone/somthing")]

    /// <summary>
    /// The WalkTo Command for Fungus
    /// </summary>

    public class Action_WalkTowards : Action_Command
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
            Debug.Log(name + " Walk Towards " + targetObject.name);
            // tell this character to walk towards this object/persona
            personaScript.TargetPersona(targetObject);
//            Debug.Log(personaObject.name + ".WalkTo(" + targetObject.name + ")");
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
