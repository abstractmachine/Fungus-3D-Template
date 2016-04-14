using UnityEngine;
using System.Collections;

namespace Fungus3D
{
    public class Action : MonoBehaviour
    {
        #region Aim

        public void AimAt(string targetName)
        {
            // get the player
            GameObject player = GameObject.FindGameObjectWithTag(targetName);
            // did we find that object?
            if (player == null)
            {
                Debug.LogError("Couldn't find target '" + targetName + "'");
                return;
            }
            // aim towards the heart
            Vector3 target = player.transform.position + new Vector3(0f, 1f, 0f);
            // start turning
            TurnTowards(target);
        }


        void TurnTowards(Vector3 target)
        {
            // stop any previously running Turn co-routines
            StopCoroutine("Turn");
            // start turning
            StartCoroutine(Turn(target));
        }


        // FIXME: This should use the Persona rotation

        IEnumerator Turn(Vector3 target)
        {
            float rotationSpeed = 15.0f;

            target.y = 0.0f;

            yield return new WaitForEndOfFrame();
            //transform.LookAt(target);

            for (float countdown = 1.0f; countdown >= 0.0f; countdown -= Time.deltaTime)
            {
                //calculate the rotation needed 
                Quaternion neededRotation = Quaternion.LookRotation(target - transform.position);

                //use spherical interpollation over time
                Quaternion interpolatedRotation = Quaternion.Slerp(transform.rotation, neededRotation, Time.deltaTime * rotationSpeed);

                transform.rotation = interpolatedRotation;

                yield return new WaitForEndOfFrame();
            }

            yield return null;

        }

        #endregion


        #region Shoot

        public void Shoot(GameObject bulletPrefab)
        {

            // get the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            // create the bullet
            GameObject bullet = (GameObject)Instantiate(bulletPrefab);
            // get objects GameObject
            GameObject bullets = GameObject.Find("Bullets");
            // if we need to
            if (bullets == null)
            {   // create the parent GameObject that contains the bullets
                bullets = new GameObject("Bullets");
            }
            // put this bullet into the objects parent
            bullet.transform.SetParent(bullets.transform);
            bullet.name = "Bullet";
            // move up to the gun level
            bullet.transform.position = this.transform.position + new Vector3(0f, 1.5f, 0f);
            // aim towards the heart
            Vector3 target = player.transform.position + new Vector3(0f, 0f, 0f);

            StartCoroutine(Bullet(bullet, target));

        }


        IEnumerator Bullet(GameObject bullet, Vector3 target) {

            // turn towards our target
            bullet.transform.LookAt(target);
            // move the bullet out a bit
            bullet.transform.Translate(Vector3.forward*1.5f, Space.Self);
            // how hard to shoot
            float force = 1000.0f;
            // add force to the bullet
            bullet.GetComponent<Rigidbody>().AddForce(bullet.transform.forward * force);
            // wait a little
            yield return new WaitForSeconds(5.0f);
            // a few seconds later
            if (bullet != null)
            {   // destroy the bullet 
                Destroy(bullet);
            }
        }

        #endregion

    } // class Action

} // namespace Fungus3D
