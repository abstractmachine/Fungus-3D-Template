using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace Fungus3D
{

    public class Collider_Click : Collider_Fungus3D, IPointerClickHandler
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
            rootParent.GetComponent<Persona>().OnClick();
        }

        #endregion

    }
    // class Click

}
 // namespace Fungus3D
