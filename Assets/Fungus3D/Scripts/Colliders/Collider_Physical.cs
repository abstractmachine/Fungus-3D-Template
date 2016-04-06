using UnityEngine;
using System.Collections;

namespace Fungus3D
{
    // Note: this must be on an "Ignore Raycast" layer if the collider is larger than the click collider
    public class Collider_Physical : Collider_Fungus3D
    {

        #region Collisions

        public void OnCollisionEnter(Collision impact)
        {   
            rootParent.GetComponent<Persona>().OnPhysicalEnter(impact);
        }

        public void OnCollisionStay(Collision impact)
        {   
            rootParent.GetComponent<Persona>().OnPhysicalStay(impact);
        }


        public void OnCollisionExit(Collision impact)
        {   
            rootParent.GetComponent<Persona>().OnPhysicalExit(impact);
        }

        #endregion

    }
    // class Proximity

}
// namespace Fungus3D