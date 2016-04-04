using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fungus;

namespace Fungus3D
{

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]

    // TODO: move NavMesh goal over to Persona
    // i.e. give Personae the power to walk. Yes, we have a Messiah complex ;-)

    public class Player : Fungus3D.Persona
    {
        #region Init

        protected override void Start()
        {   
            // call the base class Start method
            base.Start();

            // are we the player?
            if (tag == "Player")
            {
                isPlayer = true;
                // set pointer to ourselves
                currentPlayer = this.gameObject;
            }
        }

        #endregion



        #region Interaction

        /// <summary>
        /// Player clicked on the Player object
        /// Note: This requires a physics Raycaster on a camera
        /// </summary>

        public override void OnClick()
        {

            OnClickPlayer(null);

        }

        /// <summary>
        /// Handle the click on Player object
        /// </summary>
        /// <param name="clickedObject">Clicked object.</param>

        public void OnClickPlayer(GameObject clickedObject)
        {
            // dead Players can't click
            if (Dead) return;

            // if we're not talking to anyone
            if (currentFlowchart == null || currentInterlocutor == null)
            {      
                // are we walking?
                if (Walking)
                {
                    StopWalking();            
                }
                // not talking to anyone, so no need to go on
                return;
            }

            // ok, this is a dialog click
            OnClickDialog(clickedObject);

        }


        public void OnClickDialog(GameObject clickedObject)
        {
            // if we currently have a menuDialog active
            if (transform.FindChild("Dialogues/MenuDialog").gameObject.activeSelf)
            {   // players MUST make choice when menuDialog is active
                print("menuDialog still active");
                return;
            }

            // error catching
            if (currentFlowchart == null)
            {
                Debug.LogError("currentFlowchart == null. IsPlayer=" + IsPlayer);
                return;
            }

            // ok, we do NOT have a menuDialog active

            // if there is no current dialogue
            if (!currentFlowchart.HasExecutingBlocks())
            {   
                // if we're still in collision with a Persona
                if (currentInterlocutor != null)
                {
                    // try to force restart that previous dialogue
                    StartFlowchart(currentInterlocutor);
                }
                // whatever the case, leave this method
                return;
            }

            List<GameObject> charactersInFlowchart = GetCharactersInFlowchart(currentFlowchart);

            // if the clicked object isn't even in the current dialog
            if (clickedObject != null && !charactersInFlowchart.Contains(clickedObject))
            {
                Debug.LogWarning("Character " + GetPath(clickedObject.transform) + " isn't in flowchart " + currentFlowchart.name);
                return;
            }

            // go through each persona we're potentially talking to
            foreach (GameObject characterObject in charactersInFlowchart)
            {
                // make sure that object isn't us
                //          if (characterObject == this.gameObject) {
                //              continue;
                //          }
                // get the path to their SayDialog
                SayDialog personaSayDialog = characterObject.GetComponentInChildren<SayDialog>();
                // if this dialog is actually something
                if (personaSayDialog != null)
                {
                    // check to see if that dialog object is active
                    if (personaSayDialog.gameObject.activeSelf)
                    {
                        // ok, push dat button!
                        personaSayDialog.continueButton.onClick.Invoke();
                        // all done
                        return;
                    }
                } // if (personaSayDialog)
            } // foreach
        }

        #endregion



        #region Triggers

        public override void OnInteractionEnter(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                Debug.Log(this.gameObject.name + "<Player>().OnInteractionEnter(" + other.name + ")");
            }

            // if we're dead, ingore the rest
            if (Dead) return;

            // if we're the player interacting with a persona
            if (IsPlayer && other.tag == "Persona" && other == targetObject)
            {
                // make sure we're not already talking with someone else
                if (currentInterlocutor != null && currentInterlocutor != other) return;

                // tell the Persona to turn towards us, the Player
                other.GetComponent<Persona>().TurnTowards(this.gameObject);

                // start talking
                StartFlowchart(other);

                // Broadcast that we're reached the target
                ReachedTarget();

                return;

            }

            // if we're touching the TouchTarget && we're at the end
            if (IsPlayer && other.tag == "TouchTarget")
            {
                // get rid of the TouchTarget
                Destroy(other);
                // Broadcast that we're reached the target
                ReachedTarget();

                return;
            }

        }


        public override void OnInteractionStay(GameObject other)
        {
            // if the logging level is full verbose
            if (collisionLogLevel == NetworkLogLevel.Full)
            {   // log activity
                print(this.gameObject.name + "<Player>().OnInteractionStay(" + other.name + ")");
            }

            // if we're dead, ingore the rest
            if (Dead) return;

            // if we're interacting with another character
            if (IsPlayer && Walking && other.tag == "Persona" && other == targetObject)
            {

                // start talking
                StartFlowchart(other);
                // stop current movement
                ReachedTarget();
            }

        }


        public override void OnInteractionExit(GameObject other)
        {
            // if the logging level is at least informational
            if (collisionLogLevel >= NetworkLogLevel.Informational)
            {   // log activity
                print(this.gameObject.name + "<Player>().OnInteractionExit(" + other.name + ")");
            }

            // if we're dead, ignore the rest
            if (Dead) return;

            // ignore anyone who is not a Persona
            if (IsPlayer && other.tag != "Persona")
            {
                return;
            }
          
            // make sure this is the actual person we were interacting with
            if (other != currentInterlocutor)
            {
                return;
            }

            StopCurrentFlowchart();

            currentInterlocutor = null;
            currentFlowchart = null;

        }

        #endregion

    }
    // class Player

}
// namespace Fungus3D