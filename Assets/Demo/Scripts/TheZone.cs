using UnityEngine;
using System.Collections;
using Fungus;

public class TheZone : MonoBehaviour {

    void OnTriggerEnter(Collider trigger) {

        if (trigger.gameObject.tag == "Player")
        {
            Fungus.Flowchart.BroadcastFungusMessage("GameOver");
        }

    }

}
