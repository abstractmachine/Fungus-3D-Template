using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace Fungus3D
{

    public class ColliderClick : ColliderFungus3D, IPointerClickHandler
    {   

        #region Init

        protected override void Start()
        {   // in the case of click, we don't want to ignore raycasts on this layer
            IgnoreRaycast(false);
            // we need the root parent just like the others
            GetRootParent();
        }

        #endregion


        #region Interaction

        /// <summary>
        /// When a player touches/clicks this character
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (rootParent.tag == "Player")
            {
                rootParent.GetComponent<Player>().OnClick();
            }
            else
            {
                rootParent.GetComponent<Persona>().OnClick();
            }
        }

        #endregion

    } // class Click

} // namespace Fungus3D
