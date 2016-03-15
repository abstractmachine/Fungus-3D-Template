using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class Template_Ground : MonoBehaviour, IPointerClickHandler {

    #region Variables

    public GameObject playerGameObject;
    public GameObject touchRipplePrefab;
    public GameObject touchSpotPrefab;

    #endregion


    #region DelegateReceivers

    void OnEnable()
    {
        Template_Player.PlayerReachedTarget += PlayerReachedTarget;
    }


    void OnDisable()
    {
        Template_Player.PlayerReachedTarget -= PlayerReachedTarget;
    }

    void PlayerReachedTarget() {

        RemovePreviousTouches();
    }

    #endregion



    #region Interaction

	public void OnPointerClick(PointerEventData eventData) {

		checkHit(eventData.position);

    }

	void checkHit(Vector2 loc) {

		// cast ray down into the world from the screen touch point
		Ray ray = Camera.main.ScreenPointToRay(loc);
		// show in debugger
		//Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
        
		// detect collisions with that point
		RaycastHit[] hits = Physics.RaycastAll(ray);

		// we should have hit at least something, like the ground
		if (hits.Length == 0) {
			Debug.LogWarning("No hit detection");
			return;
		}

		bool didHitGround = false;
		Vector3 groundHitPoint = Vector3.zero;

		foreach (RaycastHit hit in hits) {      
			// make sure it's a ground click/touch
			if (hit.transform.name == "Ground") {
				didHitGround = true;
				groundHitPoint = hit.point;
			}
		}

		if (didHitGround) {
			// get the point on the plane where we clicked and go there
			TouchedGround(groundHitPoint);
		}

	}


    public void TouchedObject(GameObject other) {

        Vector3 position = other.transform.position;
        position.y = 0.01f;

		// tell the player who to start walking to
        playerGameObject.GetComponent<Template_Player>().GoToObject(other);

        // if we were already showing a click exploder
        RemovePreviousTouches();
        // show click
        ShowClick(position);

	}

   
	public void TouchedGround(Vector3 position) {

		// reposition to ground
		position.y = 0.01f;
		// tell the player where to start walking
        Template_Player playerScript = playerGameObject.GetComponent<Template_Player>();
        if (playerScript != null)
        {
            playerScript.GoToPosition(position);
        }
        else
        {
            //playerGameObject.GetComponent<Walk>().SetTargetPosition(position);
        }

        // if we were already showing a click exploder
        RemovePreviousTouches();
        // show click
        ShowClick(position);

    }

    #endregion


    #region Clicks

    void RemovePreviousTouches() {
        
        RemoveRipples();
        RemoveTouchTarget();

    }

    void RemoveRipples() {

        // kill all other clicks
        GameObject[] otherClicks;
        // remove ripples
        otherClicks = GameObject.FindGameObjectsWithTag("TouchRipple");
        foreach (GameObject obj in otherClicks) {
            Destroy(obj);
        }

    }


    public void RemoveTouchTarget() {

        GameObject[] otherClicks;
        // remove touch targets ("X")
        otherClicks = GameObject.FindGameObjectsWithTag("TouchTarget");
        foreach (GameObject obj in otherClicks) {
            Destroy(obj);
        }

    }


    void ShowClick(Vector3 position) {

        // show xSpot
        GameObject touchSpot = Instantiate(touchSpotPrefab, position, Quaternion.Euler(90, 0, 0)) as GameObject;
        touchSpot.name = "TouchTarget";
        touchSpot.transform.parent = GameObject.Find("Ground").transform;

        // montrer où on a cliqué
        GameObject touchRipple = Instantiate(touchRipplePrefab, position, Quaternion.Euler(90, 0, 0)) as GameObject;
        touchRipple.name = "TouchRipple";
        touchRipple.transform.parent = GameObject.Find("Ground").transform;
        // blow up in a co-routine
        StartCoroutine(Explode(touchRipple));

    }


    IEnumerator Explode(GameObject touchPoint) {

        //      // match background color for the sprite color
        //      float timeSaturation = Camera.main.GetComponent<Daylight>().TimeSaturation;
        //      timeSaturation = Mathf.Min(1.0f, 1.3f - timeSaturation);
        //      Color c = new Color(timeSaturation, timeSaturation, timeSaturation, 1.0f);
        //      GetComponent<SpriteRenderer>().color = c;

        float explosionSpeed = 0.025f;

        while (touchPoint != null && touchPoint.transform.localScale.x < 0.5f) {      
            touchPoint.transform.localScale = touchPoint.transform.localScale + new Vector3(explosionSpeed, explosionSpeed, explosionSpeed);
            yield return new WaitForEndOfFrame();
        }

        Destroy(touchPoint);

    }

    #endregion

}
