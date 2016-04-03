using UnityEngine;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    /// <summary>
    /// This handler fires when interacting with the Persona's Proxmity Trigger
    /// </summary>

    [EventHandlerInfo("Triggers", "ProximityExit", "Start this block when the player exits this gameObject's proximity Trigger")]

    public class ProximityExit : EventHandler
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
            return "ProximityExit Summary";
        }

    }
}
