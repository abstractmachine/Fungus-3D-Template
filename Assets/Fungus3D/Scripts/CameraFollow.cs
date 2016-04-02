using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Fungus3D {

    public class CameraFollow : MonoBehaviour {



        #region Variables

        [SerializeField]
        int targetFrameRate = 60;

        public GameObject target;

        float smoothTime = 0.3f;
        private Vector3 velocity = Vector3.zero;

        float zoomLevel = 7.4f;
        float zoomInSpeed = 0.5f;
        float zoomOutSpeed = 0.25f;
        float zoomDialogSpeed = 0.75f;

        float zoomMax = 15.0f;
        float zoomMin = 7.0f;
        float zoomDialog = 4.0f;

        float cameraDistance = 30;  // this x/y/z distance of the camera to our persona

        bool dialogOn = false;

        #endregion



        #region Event Delegates

        public delegate void CameraMovedDelegate();
        public static event CameraMovedDelegate CameraMoved;

        #endregion



        #region Event Listeners

        void OnEnable()
        {
            Player.MovedListener += PlayerMoved;
            Player.StartedDialogueWithListener += PlayerStartedDialogueWith;
            Player.StoppedDialogueWithListener += PlayerStoppedDialogueWith;
           
        }


        void OnDisable()
        {
            Player.MovedListener -= PlayerMoved;
            Player.StartedDialogueWithListener -= PlayerStartedDialogueWith;
            Player.StoppedDialogueWithListener -= PlayerStoppedDialogueWith;
        }

        #endregion


        #region Init

        void Start()
        {   
            // set game speed to 60
            Application.targetFrameRate = targetFrameRate;
        }

        #endregion


        #region Loop

        void Update() {

            FollowTarget();
            UpdateZoom();
        }


        void FollowTarget() {

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

        void UpdateZoom() {

            float thisZoomSpeed = zoomInSpeed;

            // if we're talking, or we're talking and are still zoomed in
            if (dialogOn || zoomLevel < zoomMin)
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

        void PlayerMoved(float distance) {

            //float zoomScale = (Screen.height / 1536.0f) * 0.5f;
            float zoomScale = 0.01f;
            float zoomDistance = Mathf.Pow(distance,1.5f) * zoomScale;
            SetZoom(zoomDistance);

        }

        void PlayerStartedDialogueWith(GameObject player, List<GameObject> personae) {

            dialogOn = true;

            ResetZoom();

        }

        void PlayerStoppedDialogueWith(GameObject player, List<GameObject> personae) {

            dialogOn = false;

            ResetZoom();

        }


        void ResetZoom() {

            SetZoom(zoomLevel);

        }

        void ZoomOut() {

            SetZoom(zoomMax);
        }

        void SetZoom(float newZoom) {

            zoomLevel = Mathf.Min(zoomMax,Mathf.Max((dialogOn) ? zoomDialog : zoomMin, newZoom));
            //zoomLevel += (newZoom - zoomLevel) * 0.025f;
            //zoomLevel = distance;
        }

        #endregion

    }

}
