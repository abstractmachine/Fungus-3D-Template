using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Fungus;

public class Phylactere : MonoBehaviour, IWriterListener {

	#region Variables

	bool visible = false;
	Text storyText;
	RectTransform panelRectTransform;
	public float lineHeight = 59.0f;

	#endregion



	#region Init


	void Start() {
           
	}

	#endregion



	#region Loop

	void Update() {

		if (visible) {
			LookAtCamera();
		}

	}


	// Turn towards camera permanently


	void LookAtCamera() {

		// look at the camera
		//transform.LookAt(Camera.main.transform, Vector3.up);
		//transform.Rotate(0, 180, 0);

		transform.rotation = Camera.main.transform.rotation;

	}

	#endregion


	#region Interfaces

	// Called when a user input event (e.g. a click) has been handled by the Writer
	public void OnInput() {
	}

	// Called when the Writer starts writing new text
	public void OnStart(AudioClip audioClip) {
		visible = true;
		CalculateLineHeight();
	}

	// Called when the Writer has paused writing text (e.g. on a {wi} tag)
	public void OnPause() {
	}

	// Called when the Writer has resumed writing text
	public void OnResume() {
	}

	// Called when the Writer has finshed writing text
	public void OnEnd() {
		visible = false;
	}

	// Called every time the Writer writes a new character glyph
	public void OnGlyph() {
		CalculateLineHeight();
	}

	#endregion


	#region Treatment

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
