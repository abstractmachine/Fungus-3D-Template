using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    /// <summary>
    /// This handler fires when interacting with the Persona's Interaction Trigger
    /// </summary>

    [EventHandlerInfo("Triggers", "InteractionExit", "Start this block when the player exits this gameObject's 'Interaction' Trigger")]

    public class InteractionExit : EventHandler
    {
        /// <summary>
        /// Fire the ExecuteBlock method
        /// </summary>
        /// 
        public void OnExit()
        {
            ExecuteBlock();
        }


        /// <summary>
        /// The summary of this Event
        /// </summary>

        public override string GetSummary()
        {
            return "InteractionExit Summary";
        }

    }
}
