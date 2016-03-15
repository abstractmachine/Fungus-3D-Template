using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Fungus;

public class Template_Phylactere : MonoBehaviour, IWriterListener {

    #region Listeners

    void OnEnable()
    {
        Template_CameraFollow.CameraMoved += CameraMoved;
    }


    void OnDisable()
    {
        Template_CameraFollow.CameraMoved -= CameraMoved;
    }

    #endregion



	#region Variables

//	bool visible = false;
	Text storyText;
	RectTransform panelRectTransform;
	public float lineHeight = 59.0f;

	#endregion



	#region Loop

    /// <summary>
    /// Turn this phylactere towards the camera(s)
    /// </summary>
 
    void CameraMoved() {

        // transform.rotation = Quaternion.Lerp(transform.rotation, Camera.main.transform.rotation, 0.5f);
        transform.rotation = Quaternion.Lerp(transform.rotation, Camera.main.transform.rotation, 1.0f);

	}

	#endregion


	#region Interfaces

    /// <summary>
    /// Called when a user input event (e.g. a click) has been handled by the Writer
    /// </summary>

	public void OnInput() {
	}

	
    /// <summary>
    /// Called when the Writer starts writing new text
    /// </summary>
    /// <param name="audioClip">The AudioClip that is currently playing</param>

	public void OnStart(AudioClip audioClip) {
//		visible = true;
		CalculateLineHeight();
	}


	/// <summary>
    /// Called when the Writer has paused writing text (e.g. on a {wi} tag).
    /// </summary>

	public void OnPause() {
	}


    /// <summary>
    /// Called when the Writer has resumed writing text.
    /// </summary>

	public void OnResume() {
	}


    /// <summary>
    /// Called when the Writer has finshed writing text
    /// </summary>

	public void OnEnd() {
//		visible = false;
	}


	/// <summary>
    /// Called every time the Writer writes a new character glyph.
    /// </summary>

	public void OnGlyph() {
		CalculateLineHeight();
	}

	#endregion


	#region Treatment

    /// <summary>
    /// Calculate the line height based on number of lines in the dialog text
    /// </summary>

	void CalculateLineHeight() {

		// make sure that we have a dialog object
		if (storyText == null) {
			// get the script that controls the storyText
			storyText = GetComponent<SayDialog>().storyText.GetComponent<Text>();
		}

		if (storyText != null) {
			// we first need to force update the canvas text rendering
			Canvas.ForceUpdateCanvases();
			// so that we can read the line count
			int lineCount = storyText.cachedTextGenerator.lineCount;
			// force to minimum text line count
			lineCount = Mathf.Max(1, lineCount);

			float panelMargins = 75.0f;

			if (panelRectTransform == null) {
				// get the panel that controls the size
				panelRectTransform = transform.FindChild("Panel").GetComponent<RectTransform>();
			}

			if (panelRectTransform != null) {
				Vector2 sizeDelta = panelRectTransform.sizeDelta;
				sizeDelta.y = panelMargins + (lineCount * lineHeight);
				panelRectTransform.sizeDelta = sizeDelta;
			}

		}

	}

	#endregion

}
