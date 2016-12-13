using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    public class Action_Command : Command
    {
        #region Members

        // who should be doing the action
        public GameObject actor;

        protected Persona personaScript;
        protected Animator animator;
        protected NavMeshAgent navMeshAgent;

        #endregion


        #region Init

        protected virtual void Awake()
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

        #endregion


        #region UI

        /// <summary>
        /// Defines the color of this command in the list of Fungus block commands
        /// </summary>

        public override Color GetButtonColor()
        {
            return new Color(1.0f, 0.75f, 1.0f, 1.0f);
        }

        #endregion

    }

}
