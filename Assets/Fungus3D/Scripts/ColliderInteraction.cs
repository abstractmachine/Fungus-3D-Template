using UnityEngine;
using System.Collections;

namespace Fungus3D
{
    
    public class ColliderInteraction : ColliderFungus3D
    {

        #region Collisions

        public void OnTriggerEnter(Collider trigger)
        {
            // is thisthe TouchTarget?
            if (trigger.tag == "TouchTarget" && rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnInteractionEnter(trigger.gameObject);
                return;
            }

            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;
            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<ColliderInteraction>().RootParent;

            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnInteractionEnter(otherRootParent);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnInteractionEnter(otherRootParent);
            }
        }

        public void OnTriggerStay(Collider trigger)
        {
            // is thisthe TouchTarget?
            if (trigger.tag == "TouchTarget" && rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnInteractionStay(trigger.gameObject);
                return;
            }

            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;
            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<ColliderInteraction>().RootParent;

            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnInteractionStay(otherRootParent);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnInteractionStay(otherRootParent);
            }
        }


        public void OnTriggerExit(Collider trigger)
        {
            // is thisthe TouchTarget?
            if (trigger.tag == "TouchTarget" && rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnInteractionExit(trigger.gameObject);
                return;
            }

            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;
            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<ColliderInteraction>().RootParent;

            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnInteractionExit(otherRootParent);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnInteractionExit(otherRootParent);
            }
        }

        #endregion

    } // class Dialog

} // namespace Fungus3D
