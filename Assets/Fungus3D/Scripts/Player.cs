using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Fungus;

namespace Fungus3D
{

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]

    // TODO: Subclass Player from Persona, moving NavMesh goal over to Persona
    // i.e. give Personae the power to walk. Yes, we have a Messiah complex ;-)

    public class Player : Fungus3D.Persona
    {

        #region Event Delegates

        public delegate void MovedDelegate(float distance);

        public static event MovedDelegate MovedListener;

        public delegate void ReachedTargetDelegate();

        public static event ReachedTargetDelegate ReachedTargetListener;

        public delegate void StartedDialogueWithDelegate(GameObject player, List<GameObject> personae);

        public static event StartedDialogueWithDelegate StartedDialogueWithListener;

        public delegate void StoppedDialogueWithDelegate(GameObject player, List<GameObject> personae);

        public static event StoppedDialogueWithDelegate StoppedDialogueWithListener;

        #endregion



        #region Event Listeners

        void OnEnable()
        {
//            Persona.ClickedOnPersonaListener += ClickedOnPersona;
            Persona.GoToPersonaListener += GoToPersona;
            Ground.GoToPositionListener += GoToPosition;
        }


        void OnDisable()
        {
//            Persona.ClickedOnPersonaListener -= ClickedOnPersona;
            Persona.GoToPersonaListener -= GoToPersona;
            Ground.GoToPositionListener -= GoToPosition;
        }

        #endregion



        #region Init

        protected override void Start()
        {   
            // call the base class Start method
            base.Start();

            // we are the player
            isPlayer = true;

            // set pointer to us
            currentPlayer = this.gameObject;

            // Don’t update position automatically
            navMeshAgent.updatePosition = false;
//            navMeshAgent.updateRotation = false;
        }

        #endregion



        #region Animation

        void Update()
        {
            if (!Dead)
            {
                UpdatePosition();
            }
        }

        void UpdatePosition()
        {
            Vector2 smoothDeltaPosition = Vector2.zero;
            Vector2 velocity = Vector2.zero;

            Vector3 worldDeltaPosition = navMeshAgent.nextPosition - transform.position;

            // Map 'worldDeltaPosition' to local space
            float dx = Vector3.Dot(transform.right, worldDeltaPosition);
            float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
            Vector2 deltaPosition = new Vector2(dx, dy);

            // Low-pass filter the deltaMove
            float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);

            smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

            // Update velocity if time advances
            if (Time.deltaTime > 1e-5f)
            {
                velocity = smoothDeltaPosition / Time.deltaTime;
            }

            bool shouldMove = velocity.magnitude > 0.5f && navMeshAgent.remainingDistance > navMeshAgent.radius;

            if (shouldMove)
            {
                animator.SetFloat("Forward", 1.0f);
            }
            else
            {
                animator.SetFloat("Forward", 0.0f);
            }

            // move head      
            //GetComponent<LookAt>().lookAtTargetPosition = navMeshAgent.steeringTarget + transform.forward;

            // Pull agent towards character
            if (worldDeltaPosition.magnitude > navMeshAgent.radius)
            {
                navMeshAgent.nextPosition = transform.position + 0.9f * worldDeltaPosition;
            }
        }

        void OnAnimatorMove()
        {
            if (Dead) return;

            // Update position to agent position
            //transform.position = navMeshAgent.nextPosition;

            // Update position based on animation movement using navigation surface height
            Vector3 position = animator.rootPosition;
            position.y = navMeshAgent.nextPosition.y;
            transform.position = position;

            if (Walking)
            {
                Vector3 deltaPosition = position - targetGoal;
                BroadcastDistance(deltaPosition.sqrMagnitude);
            }
        }

        #endregion



        #region Interaction

        /// <summary>
        /// Player clicked on the Player object
        /// Note: This requires a physics Raycaster on a camera
        /// </summary>

        public override void OnClick() {

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
                //Destroy(other);
                // Broadcast that we're reached the target
                ReachedTarget();

                return;
            }

        }


        void ReachedTarget()
        {
            // if we're still walking
            if (Walking)
            {   // stop current movement
                StopWalking();
            }

            // better annul any inevitable touch targets
            // make sure there are listeners
            if (ReachedTargetListener != null)
            {   // broadcast that we've reached the target
                ReachedTargetListener();
            }
            // 
            BroadcastDistance(0.0f);

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

//            // if we're interacting with another character
//            if (IsPlayer && Walking && other.tag == "Persona" && other == targetObject)
//            {
//                    // stop current movement
//                    StopWalking();
//            }

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



        #region NavMesh

        public void GoToPosition(Vector3 position)
        {
            if (Dead) return;

            targetObject = null;
            targetGoal = position;
            targetGoalIsSet = true;
            navMeshAgent.destination = targetGoal;

        }


        public void GoToPersona(GameObject other)
        {
            if (Dead) return;

            targetObject = other;

            Vector3 position = other.transform.position;
            position.y = 0.01f;
            targetGoal = position;
            targetGoalIsSet = true;
            navMeshAgent.destination = targetGoal;

        }

        void StopWalking()
        {
            if (Dead) return;

            // go to where we already are
            targetObject = null;
            // remember that this is the new target
            targetGoal = transform.position;
            navMeshAgent.ResetPath();
            // broadcast that we're at the new target
            BroadcastDistance(0.0f);
            // we no longer have a targetGoal
            targetGoalIsSet = false;

        }

        bool IsAtDestination()
        {
          
            if (navMeshAgent.pathPending)
            {
                return true;
            }

            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }

            return false;

        }


        void BroadcastDistance(float distance)
        {

            // if there are listeners
            if (targetGoalIsSet && MovedListener != null)
            {   // broadcast movement change
                MovedListener(distance);
            }

            // make sure there are listeners
            if (distance == 0.0f && ReachedTargetListener != null)
            {   // broadcast that we've reached the target
                ReachedTargetListener();
            }

        }

        #endregion



        #region Gestures

        void StartedDialogue(Flowchart flowchart)
        {
            // get a list of all the characters in this flowchart
            List<GameObject> personae = GetCharactersInFlowchart(flowchart);
            // if there are any listeners
            if (StartedDialogueWithListener != null)
            {   // tell them all the objects we've started talking to
                StartedDialogueWithListener(this.gameObject, personae);
            }
        }

        void StoppedDialogue(Flowchart flowchart)
        {

            // get a list of all the characters in this flowchart
            List<GameObject> personae = GetCharactersInFlowchart(flowchart);
            // if there are any listeners
            if (StoppedDialogueWithListener != null)
            {   // tell them all the objects we've stopped discussing with
                StoppedDialogueWithListener(this.gameObject, personae);
            }

        }

        #endregion



        #region Flowchart

        /// <summary>
        /// Starts the flowchart.
        /// </summary>
        /// <param name="other">The other GameObject we're dialoguing with.</param>

        void StartFlowchart(GameObject other)
        {
            // find this flowchart in this character
            Flowchart flowchart = GetFlowchart(other.gameObject);

            // if we found this persona's flowchart
            if (flowchart != null)
            {
                // remember who we're interacting with
                currentInterlocutor = other;
                // remember which flowchart we're interacting with
                currentFlowchart = flowchart;
                // start this specific flowchart
                flowchart.SendFungusMessage("DialogEnter");
                // Started a dialog using this flowchart
                StartedDialogue(flowchart);
            }

        }


        /// <summary>
        /// Stops the current flowchart and informs all concerned characters.
        /// </summary>

        void StopCurrentFlowchart()
        {

            if (currentFlowchart != null)
            {
                // Stopped a dialog with a Persona
                StoppedDialogue(currentFlowchart);
                // turn off Flowchart
                currentFlowchart.GetComponent<Flowchart>().StopAllBlocks();
                currentFlowchart.GetComponent<Flowchart>().StopAllCoroutines();
                // send a stop message to the current flowchart
                currentFlowchart.SendFungusMessage("DalogExit");
            }

            // hide any possible menus of our own
            Transform playerSayTransform = transform.FindChild("Dialogues/SayDialog");

            if (playerSayTransform != null)
            {
                playerSayTransform.gameObject.SetActive(false);
            }

            Transform playerMenuTransform = transform.FindChild("Dialogues/MenuDialog");

            // FIXME: This is ugly
            if (playerMenuTransform != null)
            {

                GameObject playerMenu = playerMenuTransform.gameObject;

                if (playerMenu != null)
                {
                    if (ChildIsActive(playerMenu, "TimeoutSlider")) SetActive(playerMenu, "TimeoutSlider", false);
                    if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton0")) SetActive(playerMenu, "ButtonGroup/OptionButton0", false);
                    if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton1")) SetActive(playerMenu, "ButtonGroup/OptionButton1", false);
                    if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton2")) SetActive(playerMenu, "ButtonGroup/OptionButton2", false);
                    if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton3")) SetActive(playerMenu, "ButtonGroup/OptionButton3", false);
                    if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton4")) SetActive(playerMenu, "ButtonGroup/OptionButton4", false);
                    if (ChildIsActive(playerMenu, "ButtonGroup/OptionButton5")) SetActive(playerMenu, "ButtonGroup/OptionButton5", false);
                    if (playerMenu.activeInHierarchy) playerMenu.SetActive(false);
                }

            }

        }


        bool ChildIsActive(GameObject parentObject, string path)
        {
            return parentObject.transform.FindChild(path).gameObject.activeInHierarchy;
        }


        void SetActive(GameObject parentObject, string path, bool newState)
        {
            parentObject.transform.FindChild(path).gameObject.SetActive(newState);
        }


        /// <summary>
        /// Determines whether this Persona GameObject is in the current flowchart.
        /// </summary>
        /// <returns><c>true</c> if the Persona GameObject is in current flowchart; otherwise, <c>false</c>.</returns>
        /// <param name="persona">Persona GameObject.</param>

        public bool PersonaIsInCurrentFlowchart(GameObject persona)
        {
            // if this is me, ignore this request
            if (persona == this.gameObject)
            {
                return false;
            }

            // if there's no current persona I'm interacting with
            if (currentInterlocutor == null)
            {
                return false;
            }

            Flowchart flowchart = null;

            // if there is a current flowchart
            if (currentFlowchart != null)
            {   // use the current flowchart for the test
                flowchart = currentFlowchart;
            }
            else
            {
                // try to extract a flowchart from the current persona
                flowchart = GetFlowchart(currentInterlocutor);
            }

            // still no flowchart? 
            if (flowchart == null)
            {   // COMPUTER SAYS NO
                return false;
            }

            // ok, so we've got a flowchart, who's in it?
            List<GameObject> characters = GetCharactersInFlowchart(currentFlowchart);
            
            // is this character in it?
            if (characters.Contains(persona))
            {
                return true;
            }

            // if we're here, then the answer is no
            return false;

        }


        /// <summary>
        /// Go into the Flowchart and figure out which characters are referenced there.
        /// </summary>
        /// <returns>A list of the GameObjects of characters in the flowchart.</returns>
        /// <param name="flowchart">The Flowchart.</param>

        List<GameObject> GetCharactersInFlowchart(Flowchart flowchart)
        {

            List<GameObject> possiblePersonaObjects = new List<GameObject>();

            if (flowchart == null)
            {
                Debug.LogError("Flowchart == null");
                return possiblePersonaObjects;
            }

            // FIXME: This doesn't work when there is no executing block
            // if we have a currently executing block
            List<Block> blocks = flowchart.GetExecutingBlocks();

            // FIXME: For some reason we now have to add ourselves to the list
            GameObject flowChartRootParent = ExtractRootParentFrom(flowchart);
            if (flowChartRootParent) possiblePersonaObjects.Add(flowChartRootParent);

            // go through each executing block
            foreach (Block block in blocks)
            {
                // get the command list
                List<Command> commands = block.commandList;
                // go through the command list
                foreach (Command command in commands)
                {
                    // if this is a say command
                    if (command.GetType().ToString() == "Fungus.Say")
                    {
                        // force type to Say
                        Say sayCommand = (Say)command;
                        // get the gameobject attached to this character
//                        GameObject persona = sayCommand.character.gameObject.transform.parent.gameObject;
                        GameObject persona = ExtractRootParentFrom(sayCommand.character.gameObject);

                        // if this one isn't already in the list
                        if (!possiblePersonaObjects.Contains(persona))
                        {
                            // add it to the list of possible people we're talking to
                            possiblePersonaObjects.Add(persona);
                        }

                    } // if type
                } // foreach Command
            } // foreach(Block

            // if this list doesn't contain the player
            if (!possiblePersonaObjects.Contains(this.gameObject))
            {
                //          print("Force-add Player");
                possiblePersonaObjects.Add(this.gameObject);
            }

            return possiblePersonaObjects;

        }

        #endregion

    }
    // class Player

}
// namespace Fungus3D