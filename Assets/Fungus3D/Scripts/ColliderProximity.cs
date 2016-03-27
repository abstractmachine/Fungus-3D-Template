using UnityEngine;
using System.Collections;

namespace Fungus3D
{
    // Note: this must be on an "Ignore Raycast" layer if the collider is larger than the click collider
    public class ColliderProximity : ColliderFungus3D
    {
        
        #region Collisions

        public void OnTriggerEnter(Collider trigger)
        {   
            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;

            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<ColliderProximity>().RootParent;

            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnProximityEnter(otherRootParent);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnProximityEnter(otherRootParent);
            }
        }

        public void OnTriggerStay(Collider trigger)
        {   
            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.gameObject.tag != "Collider") return;

            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<ColliderProximity>().RootParent;

            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnProximityStay(otherRootParent);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnProximityStay(otherRootParent);
            }
        }


        public void OnTriggerExit(Collider trigger)
        {   
            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;

            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<ColliderProximity>().RootParent;

            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnProximityExit(otherRootParent);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnProximityExit(otherRootParent);
            }
        }

        #endregion
    
    }
    // class Proximity

}
 // namespace Fungus3D