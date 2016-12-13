using UnityEngine;
using System.Collections;

namespace Fungus3D
{
    // Note: this must be on an "Ignore Raycast" layer if the collider is larger than the click collider
    public class Collider_Proximity : Collider_Fungus3D
    {

        #region Event Delegates

        //FIXME: This is a hack. Remove

        public delegate void StartedProximityWithDelegate(GameObject player, GameObject personae);
        public static event StartedProximityWithDelegate StartedProximityWithListener;

        public delegate void StoppedProximityWithDelegate(GameObject player, GameObject personae);
        public static event StoppedProximityWithDelegate StoppedProximityWithListener;

        #endregion

        
        #region Collisions

        public void OnTriggerEnter(Collider trigger)
        {   
            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;

            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<Collider_Proximity>().RootParent;
            rootParent.GetComponent<Persona>().OnProximityEnter(otherRootParent);

            // FIXME: This is a hack. Remove.
            if (rootParent.tag == "Player")
            {
                StartedProximity(rootParent, otherRootParent);
            }
        }

        public void OnTriggerStay(Collider trigger)
        {   
            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.gameObject.tag != "Collider") return;

            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<Collider_Proximity>().RootParent;
            rootParent.GetComponent<Persona>().OnProximityStay(otherRootParent);
        }


        public void OnTriggerExit(Collider trigger)
        {   
            // if this is not a collider (for example it's a ragdoll), forget it
            if (trigger.tag != "Collider") return;

            // if we're not of the same ilk
            if (trigger.name != this.name) return;

            // get the RootParent from the other object
            GameObject otherRootParent = trigger.gameObject.GetComponent<Collider_Proximity>().RootParent;
            rootParent.GetComponent<Persona>().OnProximityExit(otherRootParent);

            // FIXME: This is a hack. Remove.
            if (rootParent.tag == "Player")
            {
                StoppedProximity(rootParent, otherRootParent);
            }
        }

        #endregion

        //FIXME: This is a hack. Remove

        void StartedProximity(GameObject player, GameObject persona)
        {
            // if there are any listeners
            if (Collider_Proximity.StartedProximityWithListener != null)
            {   // tell them all the objects we've started talking to
                StartedProximityWithListener(rootParent, persona);
            }
        }


        void StoppedProximity(GameObject player, GameObject persona)
        {
            // if there are any listeners
            if (Collider_Proximity.StoppedProximityWithListener != null)
            {   // tell them all the objects we've started talking to
                StoppedProximityWithListener(rootParent, persona);
            }
        }
    
    }
    // class Proximity

}
// namespace Fungus3D