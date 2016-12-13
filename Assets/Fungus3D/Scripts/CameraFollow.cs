using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Fungus3D
{
    public class CameraFollow : MonoBehaviour
    {
        
        #region Variables

        [SerializeField]
        int targetFrameRate = 60;

        public GameObject target;

        float smoothTime = 0.3f;
        private Vector3 velocity = Vector3.zero;

        float zoomLevel = 9.4f;
        float zoomInSpeed = 0.6f;
        float zoomOutSpeed = 0.25f;
        float zoomDialogSpeed = 0.8f;

        float zoomMax = 25.0f;
        float zoomMin = 10.0f;
        float zoomDialog = 8.5f;

        float cameraDistance = 30;
        // this x/y/z distance of the camera to our persona

        bool dialogOn = false;

        List<GameObject> proximities = new List<GameObject>();

        // previously hidden items
        private Dictionary<MeshRenderer, Material> hiddenMaterials;
        //This is the material with the Transparent/Diffuse With Shadow shader
        public Material HiderMaterial;

        #endregion


        #region Event Delegates

        public delegate void CameraMovedDelegate();

        public static event CameraMovedDelegate CameraMoved;

        #endregion


        #region Event Listeners

        void OnEnable()
        {
            Persona.MovedListener += PlayerMoved;

            Persona.StartedProximityWithListener += PlayerStartedProximityWith;
            Persona.StoppedProximityWithListener += PlayerStoppedProximityWith;

            Collider_Proximity.StartedProximityWithListener += PlayerStartedProximityWith;
            Collider_Proximity.StoppedProximityWithListener += PlayerStoppedProximityWith;

            Persona.StartedDialogueWithListener += PlayerStartedDialogueWith;
            Persona.StoppedDialogueWithListener += PlayerStoppedDialogueWith;
        }


        void OnDisable()
        {
            Persona.MovedListener -= PlayerMoved;

            Persona.StartedProximityWithListener -= PlayerStartedProximityWith;
            Persona.StoppedProximityWithListener -= PlayerStoppedProximityWith;

            Collider_Proximity.StartedProximityWithListener -= PlayerStartedProximityWith;
            Collider_Proximity.StoppedProximityWithListener -= PlayerStoppedProximityWith;

            Persona.StartedDialogueWithListener -= PlayerStartedDialogueWith;
            Persona.StoppedDialogueWithListener -= PlayerStoppedDialogueWith;
        }

        #endregion


        #region Init

        void Start()
        {   
            // set game speed to 60
            Application.targetFrameRate = targetFrameRate;
            // make sure there's a target
            if (target == null)
            {
                target = GameObject.FindGameObjectWithTag("Player");
            }

            // set empty dictionary <object,material>
            hiddenMaterials = new Dictionary<MeshRenderer, Material>();
        }

        #endregion


        #region Loop

        void Update()
        {
            FollowTarget();
            UpdateZoom();
        }

        void FixedUpdate() 
        {
//            resetHiddenMaterials();
//            checkForOcclusion();
        }


        void FollowTarget()
        {
            // the camera constantly follows the persona
//            float smoothTime = 0.5f * Time.deltaTime;

            Vector3 cameraTarget = target.transform.position + new Vector3(-cameraDistance, cameraDistance * 1.5f, -cameraDistance);

            transform.position = Vector3.SmoothDamp(transform.position, cameraTarget, ref velocity, smoothTime);
            //            transform.position = Vector3.Lerp(transform.position, target.transform.position + new Vector3(-cameraDistance, cameraDistance*1.5f, -cameraDistance), smoothTime);
            // all the cameras look at the target
            foreach (Camera camera in Camera.allCameras)
            {
                camera.transform.LookAt(target.transform);
            }
            // if there are any listeners
            if (CameraMoved != null)
            {   // tell them that the camera moved
                CameraMoved();
            }
        }


        void UpdateZoom()
        {
            float thisZoomSpeed = zoomInSpeed;

            // if we're talking, or we're talking and are still zoomed in
            if (dialogOn || proximities.Count > 0 || zoomLevel < zoomMin)
            {
                thisZoomSpeed = zoomDialogSpeed;
            }
            else if (Camera.main.orthographicSize < zoomLevel)
            {
                thisZoomSpeed = zoomOutSpeed;
            }

            float orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, zoomLevel, Time.deltaTime * thisZoomSpeed);

            // tell each camera
            foreach (Camera camera in Camera.allCameras)
            {   // to zoom to this level
                camera.orthographicSize = orthographicSize;
            }
        }

        #endregion


        #region Zoom

        void PlayerMoved(float distance)
        {
            //float zoomScale = (Screen.height / 1536.0f) * 0.5f;
            float zoomScale = 0.02f;
            float zoomDistance = Mathf.Pow(distance, 2.5f) * zoomScale;
            SetZoom(zoomDistance);
        }


        void PlayerStartedProximityWith(GameObject player, GameObject persona)
        {
            // if we haven't already added this Persona to the list
            if (!proximities.Contains(persona))
            {
                proximities.Add(persona);
            }

//            Debug.Log(proximities.Count);
        }


        void PlayerStoppedProximityWith(GameObject player, GameObject persona)
        {
            // if we have this Persona is in the list
            if (proximities.Contains(persona))
            {
                proximities.Remove(persona);
            }

//            Debug.Log(proximities.Count);
        }


        void PlayerStartedDialogueWith(GameObject player, List<GameObject> personae)
        {
            dialogOn = true;

            ResetZoom();
        }


        void PlayerStoppedDialogueWith(GameObject player, List<GameObject> personae)
        {
            dialogOn = false;

            ResetZoom();
        }


        void ResetZoom()
        {
            SetZoom(zoomLevel);
        }


        void ZoomOut()
        {
            SetZoom(zoomMax);
        }


        void SetZoom(float newZoom)
        {
            if (dialogOn || proximities.Count > 0)
            {
                zoomLevel = zoomDialog;
                return;
            }

            newZoom *= 0.01f;

            zoomLevel = newZoom;
            zoomLevel = Mathf.Max(zoomLevel, zoomMin);
            zoomLevel = Mathf.Min(zoomLevel, zoomMax);

//            Debug.Log("SetZoom = " + zoomLevel + "\t" + dialogOn + "\t" + proximities.Count + "\t" + newZoom);

            //zoomLevel += (newZoom - zoomLevel) * 0.025f;
            //zoomLevel = distance;
        }

        #endregion


        #region Hider

        void checkForOcclusion()
        {
            if (HiderMaterial == null) return;

            //Cast a ray from this object's transform the the watch target's transform

//            RaycastHit[] hits = Physics.RaycastAll(
//                                    Camera.main.transform.position,
//                                    target.transform.position - Camera.main.transform.position,
//                                    Vector3.Distance(target.transform.position, Camera.main.transform.position)
//                                );
            RaycastHit[] hits = Physics.RaycastAll(
                Camera.main.transform.position,
                target.transform.position - Camera.main.transform.position,
                Vector3.Distance(target.transform.position, Camera.main.transform.position),
                LayerMask.GetMask("Obstacles")
            );

            //Loop through all overlapping objects and disable their mesh renderer
            if (hits.Length == 0) return;

            foreach (RaycastHit hit in hits)
            {
                // only obstacles are transparent
//                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Obstacles")) continue;

                Debug.Log(hit.collider.gameObject.name);

                // make sure we're not trying to hide the wrong object
                //if (hit.collider.name == "Persona" || hit.collider.name == "Player" || hit.collider.name == "Ground" || hit.collider.name == "Clicker") continue;

                // get pointers to all the child objects
                MeshRenderer[] meshRenderers = hit.collider.gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    // if already in dictionary, forget it
                    if (hiddenMaterials.ContainsKey(meshRenderer)) continue;
                    // remember this material (to turn it back on)
                    hiddenMaterials.Add(meshRenderer, meshRenderer.material);
                    meshRenderer.material = HiderMaterial;
                } // foreach MeshRenderer
            } // foreach

        }


        void resetHiddenMaterials()
        {
            if (HiderMaterial == null) return;

            //reset and clear all the previous objects
            if (hiddenMaterials.Count > 0)
            {
                foreach (MeshRenderer meshRenderer in hiddenMaterials.Keys)
                {
                    if (meshRenderer == null) continue;
                    // extract material from dictionary
                    meshRenderer.material = hiddenMaterials[meshRenderer];
                }
                hiddenMaterials.Clear();
            }

        }

        #endregion

    }

}
