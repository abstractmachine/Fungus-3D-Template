using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    /// <summary>
    /// This handler fires when interacting with the Persona's Interaction Trigger
    /// </summary>

    [EventHandlerInfo("Triggers", "InteractionEnter", "Start this block when the player enters this gameObject's 'Interaction' Trigger")]

    public class Handler_InteractionEnter : EventHandler
    {
        /// <summary>
        /// Fire the ExecuteBlock method
        /// </summary>
        /// 
        public void OnEnter()
        {
            ExecuteBlock();
        }


        /// <summary>
        /// The summary of this Event
        /// </summary>

        public override string GetSummary()
        {
            return "InteractionEnter Summary";
        }

    }
}
