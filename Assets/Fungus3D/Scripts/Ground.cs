using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace Fungus3D
{
    
    public class Ground : MonoBehaviour, IPointerClickHandler
    {

        #region Variables

        public GameObject playerGameObject;
        public GameObject touchRipplePrefab;
        public GameObject touchTargetPrefab;

        #endregion



        #region Event Delegates

        public delegate void GoToPositionDelegate(Vector3 position, GameObject actor);

        public static event GoToPositionDelegate GoToPositionListener;

        #endregion



        #region Event Listeners

        void OnEnable()
        {
            Persona.ReachedTargetListener += PlayerReachedTarget;
            Persona.GoToPersonaListener += GoToPersona; 
        }


        void OnDisable()
        {
            Persona.ReachedTargetListener -= PlayerReachedTarget;
            Persona.GoToPersonaListener -= GoToPersona;
        }

        void PlayerReachedTarget()
        {

            RemovePreviousTouches();
        }

        #endregion



        #region Interaction

        /// <summary>
        /// Raises the pointer click event.
        /// </summary>
        /// <param name="eventData">Event data.</param>

        public void OnPointerClick(PointerEventData eventData)
        {
            checkHit(eventData.position);
        }


        /// <summary>
        /// Checks to see if we hit anything
        /// </summary>
        /// <param name="loc">Location.</param>

        void checkHit(Vector2 loc)
        {

            // cast ray down into the world from the screen touch point
            Ray ray = Camera.main.ScreenPointToRay(loc);
            // show in debugger
            //Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            
            // detect collisions with that point
            RaycastHit[] hits = Physics.RaycastAll(ray);

            // we should have hit at least something, like the ground
            if (hits.Length == 0)
            {
                Debug.LogWarning("No hit detection");
                return;
            }

            bool didHitGround = false;
            Vector3 groundHitPoint = Vector3.zero;

            foreach (RaycastHit hit in hits)
            {      
                // make sure it's a ground click/touch
                if (hit.transform.name == "Ground")
                {
                    didHitGround = true;
                    groundHitPoint = hit.point;
                }
            }

            if (didHitGround)
            {
                // get the point on the plane where we clicked and go there
                TouchedGround(groundHitPoint);
            }

        }


        /// <summary>
        /// The Player Touched the ground.
        /// This requires activating a PhysicsRaycaster on one of the cameras
        /// </summary>
        /// <param name="position">Position.</param>

        void TouchedGround(Vector3 position)
        {

            // reposition to ground
            position.y = 0.01f;

            // if someone's listening
            if (GoToPositionListener != null)
            {
                // tell the player to go somewhere (null == no defined actor == player)
                GoToPositionListener(position, null);
            }

            // if we were already showing a click exploder
            RemovePreviousTouches();
            // show click
            ShowTouch(position);

        }


        /// <summary>
        /// Someone clicked on a persona are we are now walking to see them.
        /// </summary>
        /// <param name="persona">The Persona GameObject.</param>

        void GoToPersona(GameObject persona, GameObject actor)
        {
            // make sure the actor is null (or is the player)
            if (actor != null && actor.tag != "Player") return;

            Vector3 position = persona.transform.position;
            position.y = 0.01f;

            // if we were already showing a click exploder
            RemovePreviousTouches();
            // show click
            ShowTouch(position);

        }

        #endregion



        #region Clicks

        /// <summary>
        /// Removes the previous touch targets and ripples.
        /// </summary>

        void RemovePreviousTouches()
        {
            
            RemoveRipples();
            RemoveTouchTarget();

        }

        /// <summary>
        /// Removes any ripples.
        /// </summary>

        void RemoveRipples()
        {

            // kill all other clicks
            GameObject[] otherClicks;
            // remove ripples
            otherClicks = GameObject.FindGameObjectsWithTag("TouchRipple");
            foreach (GameObject obj in otherClicks)
            {
                Destroy(obj);
            }

        }

        /// <summary>
        /// Removes any touch targets.
        /// </summary>

        public void RemoveTouchTarget()
        {

            GameObject[] otherClicks;
            // remove touch targets ("X")
            otherClicks = GameObject.FindGameObjectsWithTag("TouchTarget");
            foreach (GameObject obj in otherClicks)
            {
                Destroy(obj);
            }

        }


        /// <summary>
        /// Draw a target and start a ripple effect at the position where we clicked
        /// </summary>
        /// <param name="position">Position.</param>

        void ShowTouch(Vector3 position)
        {
            // show TouchTarget
            GameObject touchTarget = Instantiate(touchTargetPrefab, position, Quaternion.Euler(90, 0, 0)) as GameObject;
            touchTarget.name = "TouchTarget";
            touchTarget.transform.parent = GameObject.Find("Ground").transform;

            // show Ripple where we clicked
            GameObject touchRipple = Instantiate(touchRipplePrefab, position, Quaternion.Euler(90, 0, 0)) as GameObject;
            touchRipple.name = "TouchRipple";
            touchRipple.transform.parent = GameObject.Find("Ground").transform;
            // blow up in a co-routine
            StartCoroutine(ExpandRipple(touchRipple));
        }


        /// <summary>
        /// This draws the expanding ripple
        /// </summary>
        /// <param name="touchPoint">The location of the ripple.</param>

        IEnumerator ExpandRipple(GameObject touchPoint)
        {
            //      // match background color for the sprite color
            //      float timeSaturation = Camera.main.GetComponent<Daylight>().TimeSaturation;
            //      timeSaturation = Mathf.Min(1.0f, 1.3f - timeSaturation);
            //      Color c = new Color(timeSaturation, timeSaturation, timeSaturation, 1.0f);
            //      GetComponent<SpriteRenderer>().color = c;

            float explosionSpeed = 0.025f;

            while (touchPoint != null && touchPoint.transform.localScale.x < 0.5f)
            {      
                touchPoint.transform.localScale = touchPoint.transform.localScale + new Vector3(explosionSpeed, explosionSpeed, explosionSpeed);
                yield return new WaitForEndOfFrame();
            }

            Destroy(touchPoint);
        }

        #endregion

    } // class Ground

} // namespace Fungus3D
