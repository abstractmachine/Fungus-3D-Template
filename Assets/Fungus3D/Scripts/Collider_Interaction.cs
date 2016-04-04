using UnityEngine;
using System.Collections;

namespace Fungus3D
{
    
    public class Collider_Interaction : Collider_Fungus3D
    {

        #region Collisions

        public void OnTriggerEnter(Collider trigger)
        {
            // is this the TouchTarget?
            if (trigger.tag == "TouchTarget" && rootParent.tag == "Player")
            {
                rootParent.GetComponent<Persona>().OnInteractionEnter(trigger.gameObject);
                return;
            }

            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;
            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<Collider_Interaction>().RootParent;
            // start the interaction with it
            rootParent.GetComponent<Persona>().OnInteractionEnter(otherRootParent);
        }

        public void OnTriggerStay(Collider trigger)
        {
            // is thisthe TouchTarget?
            if (trigger.tag == "TouchTarget" && rootParent.tag == "Player")
            {
                rootParent.GetComponent<Persona>().OnInteractionStay(trigger.gameObject);
                return;
            }

            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;
            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<Collider_Interaction>().RootParent;
            rootParent.GetComponent<Persona>().OnInteractionStay(otherRootParent);
        }


        public void OnTriggerExit(Collider trigger)
        {
            // is thisthe TouchTarget?
            if (trigger.tag == "TouchTarget" && rootParent.tag == "Player")
            {
                rootParent.GetComponent<Persona>().OnInteractionExit(trigger.gameObject);
                return;
            }

            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;
            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<Collider_Interaction>().RootParent;
            rootParent.GetComponent<Persona>().OnInteractionExit(otherRootParent);
        }

        #endregion

    }
    // class Dialog

}
 // namespace Fungus3D
