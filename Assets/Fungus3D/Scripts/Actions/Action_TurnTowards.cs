using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    // declare this command in Fungus
    [CommandInfo("Action", "TurnTowards", "Turn facing GameObject")]

    /// <summary>
    /// The WalkTo Command for Fungus
    /// </summary>

    public class Action_TurnTowards : Action_Command
    {
        #region Members

        // where the Persona walks to
        public GameObject targetObject;

        #endregion


        #region Init

        protected override void Awake()
        {
            // call base Awake() method
            base.Awake();

            // if there is no defined target, turn towards player
            if (actor.tag == "Player" && targetObject == null)
            {
                targetObject = this.gameObject;
            }
            else if (targetObject == null)
            {
                targetObject = GameObject.FindGameObjectWithTag("Player");
                // if we are still null
                if (targetObject == null)
                {
                    // error
                    Debug.LogError("No Player defined in Scene");
                }
            }
        }

        #endregion


        #region Enter

        /// <summary>
        /// This code is executed when the command fires.
        /// </summary>

        public override void OnEnter()
        {
            // make sure target object is defined
            if (targetObject == null && actor == null)
            {
                Debug.LogError("Error: both Actor and Target objects are undefined");
                return;
            }
            // tell this character to walk there
            personaScript.TurnTowards(targetObject);
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
            if (targetObject == null && actor == null)
            {
                return "Turn towards Player";
            }

            if (actor != null && targetObject != null)
            {
                return "Turn " + actor.name + " towards " + targetObject.name;
            }

            if (actor != null)
            {
                return "Turn " + actor.name + " towards this GameObject";
            }

            if (targetObject != null)
            {
                return "Turn towards " + targetObject.name;
            }

            // display the name of the target
            return "Error: Target undefined";
        }

        #endregion

    }

}
