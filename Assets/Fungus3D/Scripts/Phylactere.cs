using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Fungus;

namespace Fungus3D
{
    public class Phylactere : MonoBehaviour, IWriterListener
    {
        
        #region Variables

        bool visible = false;
        Text storyText;

        float scaleFactor = 0.0004f;

        // TODO: this is value is arbitrary and should be calculated dynamically
        float lineHeight = 59.0f;
//        float characterWidth = 20.0f;

        Vector2 panelMargins = new Vector2(150.0f, 75.0f);

        RectTransform panelRectTransform;
        RectTransform storyRectTransform;

        Vector2 storyOffsetMin;
        Vector2 storyOffsetMax;
        Vector2 storyAnchorMin;
        Vector2 storyAnchorMax;
        Vector2 storyPivot;

        Vector2 panelOffsetMin;
        Vector2 panelOffsetMax;
        Vector2 panelAnchorMin;
        Vector2 panelAnchorMax;
        Vector2 panelPivot;

        #endregion



        #region Get/Set

        public bool IsVisible { get { return visible; } }

        #endregion



        #region Listeners

        void OnEnable()
        {
            CameraFollow.CameraMoved += CameraMoved;
        }


        void OnDisable()
        {
            CameraFollow.CameraMoved -= CameraMoved;
        }

        /// <summary>
        /// Turn this phylactere towards the camera(s) whenver it/they move
        /// </summary>

        void CameraMoved()
        {
            // transform.rotation = Quaternion.Lerp(transform.rotation, Camera.main.transform.rotation, 0.5f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Camera.main.transform.rotation, 1.0f);
        }

        #endregion



        #region Interfaces

        /// <summary>
        /// Called when a user input event (e.g. a click) has been handled by the Writer
        /// </summary>

        public void OnInput()
        {
        }


        void Awake()
        {

            // get the script that controls the storyText
            storyText = GetComponent<SayDialog>().storyText.GetComponent<Text>();

            // get the default text width
            storyRectTransform = storyText.gameObject.GetComponent<RectTransform>();

            storyOffsetMin = storyRectTransform.offsetMin;
            storyOffsetMax = storyRectTransform.offsetMax;
            storyAnchorMin = storyRectTransform.anchorMin;
            storyAnchorMax = storyRectTransform.anchorMax;
            storyPivot = storyRectTransform.pivot;

            // get the default panel width
            panelRectTransform = transform.FindChild("Panel").GetComponent<RectTransform>();

            panelOffsetMin = panelRectTransform.offsetMin;
            panelOffsetMax = panelRectTransform.offsetMax;
            panelAnchorMin = panelRectTransform.anchorMin;
            panelAnchorMax = panelRectTransform.anchorMax;
            panelPivot = panelRectTransform.pivot;

        }

    	
        /// <summary>
        /// Called when the Writer starts writing new text
        /// </summary>
        /// <param name="audioClip">The AudioClip that is currently playing</param>

        public void OnStart(AudioClip audioClip)
        {
            // set the size of the phylactère in relation to the camera angle
            SetSize();

            visible = true;
            CalculateLineHeight();
        }


        void SetSize() 
        {
            // set the scale in relation to the camera distance
            float targetScale = Camera.main.orthographicSize * scaleFactor;
            // get current scale
            float currentScale = transform.localScale.x;
            // lerp to final scale
            float finalScale = Mathf.Lerp(currentScale, targetScale, 0.25f);
            // set that as the scale
            transform.localScale = new Vector3(finalScale, finalScale, finalScale);
        }


        /// <summary>
        /// Called when the Writer has paused writing text (e.g. on a {wi} tag).
        /// </summary>

        public void OnPause()
        {
        }


        /// <summary>
        /// Called when the Writer has resumed writing text.
        /// </summary>

        public void OnResume()
        {
        }


        /// <summary>
        /// Called when the Writer has finshed writing text
        /// </summary>

        public void OnEnd()
        {
            visible = false;
        }


        /// <summary>
        /// Called every time the Writer writes a new character glyph.
        /// </summary>

        public void OnGlyph()
        {
            CalculateLineHeight();
        }

        #endregion



        #region Treatment

        /// <summary>
        /// Calculate the line height based on number of lines in the dialog text
        /// </summary>

        void CalculateLineHeight()
        {

            if (storyText != null)
            {
                // we first need to force update the canvas text rendering
                Canvas.ForceUpdateCanvases();
                // so that we can read the line count
                int lineCount = storyText.cachedTextGenerator.lineCount;
                // force to minimum text line count
                lineCount = Mathf.Max(1, lineCount);

                // HACK: Fungus resizes text panel whenever there is a character image. This hack deactivates it

//                storyRectTransform.anchorMin = new Vector2(0f, 0f);
//                storyRectTransform.anchorMax = new Vector2(1f, 1f);
//                storyRectTransform.pivot = new Vector2(0.5f, 0.5f);

                storyRectTransform.offsetMin = storyOffsetMin;
                storyRectTransform.offsetMax = storyOffsetMax;
                storyRectTransform.anchorMin = storyAnchorMin;
                storyRectTransform.anchorMax = storyAnchorMax;
                storyRectTransform.pivot = storyPivot;

//                panelRectTransform.anchorMin = new Vector2(0.5f, 0f);
//                panelRectTransform.anchorMax = new Vector2(0.5f, 0f);
//                panelRectTransform.pivot = new Vector2(0.5f, 0f);

                panelRectTransform.offsetMin = panelOffsetMin;
                panelRectTransform.offsetMax = panelOffsetMax;
                panelRectTransform.anchorMin = panelAnchorMin;
                panelRectTransform.anchorMax = panelAnchorMax;
                panelRectTransform.pivot = panelPivot;

                // recalculate the size of the text panel
                Vector2 sizeDelta = panelRectTransform.sizeDelta;

//                // if we have only one line
//                if (storyText.text.Length < 15)
//                {   // how many characters on that line?
//                    int characterCount = Mathf.Max(5,storyText.text.Length);
//                    // calculate character width
//                    sizeDelta.x = panelMargins.x + (characterCount * characterWidth);
//                    print(panelMargins.x + "\t" + characterCount + "\t" + characterWidth + "\t" + sizeDelta.x);
//                }
//                else
//                {
//                    sizeDelta.x = storyWidth;
//                }

//                sizeDelta.x = storyWidth;

                // configure the height
                sizeDelta.y = panelMargins.y + (lineCount * lineHeight);
                // apply changes
                panelRectTransform.sizeDelta = sizeDelta;

            }

        }

        #endregion

    } // class Phylactere

}
// namespace Fungus3D