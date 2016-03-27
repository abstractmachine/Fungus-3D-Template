using UnityEngine;
using System.Collections;

namespace Fungus3D
{
    // Note: this must be on an "Ignore Raycast" layer if the collider is larger than the click collider
    public class ColliderPhysical : ColliderFungus3D
    {

        #region Collisions

        public void OnCollisionEnter(Collision impact)
        {   
            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnPhysicalEnter(impact);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnPhysicalEnter(impact);
            }
        }

        public void OnCollisionStay(Collision impact)
        {   
            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnPhysicalStay(impact);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnPhysicalStay(impact);
            }
        }


        public void OnCollisionExit(Collision impact)
        {   
            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnPhysicalExit(impact);
            }
            else
            {
                rootParent.GetComponent<Persona>().OnPhysicalExit(impact);
            }
        }

        #endregion

    }
    // class Proximity

}
// namespace Fungus3D