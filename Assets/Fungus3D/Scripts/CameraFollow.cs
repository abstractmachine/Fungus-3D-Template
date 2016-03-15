using UnityEngine;
using System.Collections;

namespace Fungus3D {

    public class CameraFollow : MonoBehaviour {

        #region Event Delegates

        public delegate void CameraMovedDelegate();
        public static event CameraMovedDelegate CameraMoved;

        #endregion



        #region Event Listeners

        void OnEnable()
        {
            Player.PlayerMoved += PlayerMoved;
        }


        void OnDisable()
        {
            Player.PlayerMoved -= PlayerMoved;
        }

        #endregion



        #region Variables

    	public GameObject target;

        float zoomLevel = 7.4f;
        float zoomInSpeed = 0.5f;
        float zoomOutSpeed = 0.25f;

        float zoomMax = 10.0f;
        float zoomMin = 5.0f;

        float cameraDistance = 30;  // this x/y/z distance of the camera to our persona

        #endregion



        #region Loop

        void Update() {

            FollowTarget();
            UpdateZoom();
        }


        void FollowTarget() {

            // the camera constantly follows the persona
            float t = 0.5f * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, target.transform.position + new Vector3(-cameraDistance, cameraDistance*1.5f, -cameraDistance), t);
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

            float thisZoomSpeed = (Camera.main.orthographicSize < zoomLevel) ? zoomOutSpeed : zoomInSpeed;

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

        void SetZoom(float newZoom) {

            zoomLevel = Mathf.Min(zoomMax,Mathf.Max(zoomMin,newZoom));
            //zoomLevel += (newZoom - zoomLevel) * 0.025f;
            //zoomLevel = distance;
        }

        void ZoomOut() {

            SetZoom(zoomMax);
        }

        #endregion

    }

}
