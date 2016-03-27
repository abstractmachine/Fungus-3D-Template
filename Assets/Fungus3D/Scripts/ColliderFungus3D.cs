using UnityEngine;
using System.Collections;

namespace Fungus3D
{
    // Note: this must be on an "Ignore Raycast" layer if the collider is larger than the click collider
    public class ColliderFungus3D : MonoBehaviour
    {
        #region Members

        // The GameObject that will fire when an object enters/stays/exits collisions
        protected GameObject rootParent;

        #endregion


        #region Accessors

        public GameObject RootParent { get { return rootParent; } }

        #endregion


        #region Init

        protected virtual void Start()
        {
            IgnoreRaycast(true);
            GetRootParent();
        }


        protected void GetRootParent()
        {
            // try to find the main gameObject as a Persona
            Persona personaScript = GetComponentInParent<Persona>();
            if (personaScript != null)
            {   
                rootParent = personaScript.gameObject;
            }
            else
            {   // try to find the main gameObject as a Persona
                Player playerScript = GetComponentInParent<Player>();
                if (playerScript != null)
                {
                    rootParent = playerScript.gameObject;
                }
                else
                {
                    Debug.LogError("No root parent gameObject set for this collider " + this.gameObject);
                }
            }

        }
        // GetRootParentGameObject


        protected void IgnoreRaycast(bool shouldIgnore)
        {
            // make sure the Ignore Raycast layer is active
            if (shouldIgnore && gameObject.layer != LayerMask.NameToLayer("Ignore Raycast"))
            {
                Debug.LogError("This GameObject should be on an 'Ignore Raycast' layer: " + this.gameObject);
            } // or inactive
            else if (!shouldIgnore && gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
            {
                
            }

        }

        #endregion

    }
    // class Collider Physical

}
// namespace Fungus3D