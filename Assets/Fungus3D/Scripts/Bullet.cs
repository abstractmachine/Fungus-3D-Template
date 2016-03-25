using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {

    float force = 1500.0f;

	void Start () {

        GetComponent<Rigidbody>().AddForce(transform.forward * force);

        Destroy(this.gameObject, 2.5f);

	}
}
