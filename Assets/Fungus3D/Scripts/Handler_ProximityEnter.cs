using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    /// <summary>
    /// This handler fires when interacting with the Persona's Proxmity Trigger
    /// </summary>

    [EventHandlerInfo("Triggers", "ProximityEnter", "Start this block when the player enters this gameObject's proximity Trigger")]

    public class ProximityEnter : EventHandler
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
            return "ProximityEnter Summary";
        }

    }
}
