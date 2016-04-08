using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Fungus;

namespace Fungus3D
{

    [Serializable]
    public class PersonaEditor : EditorWindow
    {

        #region Variables

        [SerializeField]
        public UnityEngine.Object modelPrefab = null;

        [SerializeField]
        public string characterName = "Persona";

        [SerializeField]
        public Color characterColor = Color.gray;

        [SerializeField]
        public float xPosition = 0.0f;

        [SerializeField]
        public float zPosition = 0.0f;

        #endregion


        #region Menu

        /// <summary>
        /// Opens the persona window.
        /// </summary>

        [MenuItem("Tools/Fungus3D/Open Persona Window")]
        private static void OpenPersonaWindow()
        {
            //EditorWindow.GetWindow(typeof(PersonaEditor));
            EditorWindow.GetWindow(typeof(PersonaEditor), false, "Persona");
        }

        #endregion


        #region Init

        protected void OnEnable()
        {
            

        }

        #endregion


        #region GUI

        void OnGUI()
        {
            
            GUILayout.Label("Character", EditorStyles.boldLabel);
            characterName = EditorGUILayout.TextField("Character name", characterName);
            characterColor = EditorGUILayout.ColorField("Character color", characterColor);

            GUILayout.Label("Position", EditorStyles.boldLabel);
//            GUILayout.BeginHorizontal();
            xPosition = EditorGUILayout.FloatField("X", xPosition);
            zPosition = EditorGUILayout.FloatField("Z", zPosition);
//            GUILayout.EndHorizontal();

            GUILayout.Label("Model", EditorStyles.boldLabel);

            //            modelPrefab = EditorGUILayout.ObjectField ("Model Prefab", modelPrefab, typeof(UnityEngine.Object), false) as UnityEngine.Object;
            modelPrefab = (UnityEngine.Object)EditorGUILayout.ObjectField("Model Prefab", modelPrefab, typeof(UnityEngine.Object), false);

            if (GUILayout.Button("Create Persona"))
            {
                CreatePersona();
            }

        }

        #endregion


        #region Create

        void CreatePersona()
        {
            // 
            string path = "Assets/Fungus3D/Prefabs/Personae/Persona.prefab";
            UnityEngine.Object personaPrefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));

            if (personaPrefab == null)
            {
                Debug.LogError("Persona.prefab missing at Assets/Fungus3D/Prefabs/Personae/Persona.prefab");
            }

            // find "Personae" EmptyObject
            GameObject personae = FindMakeGameObject("", "Personae", null);

            // prepare the position of this Persona
            Vector3 position = Vector3.zero;
            position.x = xPosition;
            position.z = zPosition;

            // create the actual GameObject in the heirarchy
            GameObject persona = CreateObject(personaPrefab, personae, characterName);

            // position the Persona
            persona.transform.position = position;

            // set the Flowchart name
            Flowchart flowchartScript = persona.GetComponentInChildren<Flowchart>();
            GameObject flowchartGameObject = flowchartScript.gameObject;
            flowchartGameObject.name = characterName + "_Flowchart";

            // set the character name
            Character characterScript = persona.GetComponentInChildren<Character>();
            GameObject characterGameObject = characterScript.gameObject;
            characterGameObject.name = characterName + "_Character";
            characterScript.nameText = characterName;
            characterScript.nameColor = characterColor;

            // if we have defined a model
            if (modelPrefab != null)
            {
                // find the current model
                Transform modelTransform = persona.transform.FindChild("Model");
                if (modelTransform == null)
                {
                    Debug.Log("Couldn't find Persona/Model");
                    return;
                }
                GameObject modelParent = modelTransform.gameObject;
                // get a reference to the first child
                GameObject currentModel = modelParent.transform.GetChild(0).gameObject;
                // add our model to it
                GameObject newModel = CreateObject(modelPrefab, modelParent, modelPrefab.name);

                // get the current animator
                Animator personaAnimator = persona.GetComponent<Animator>();
                // get the current avatar
                Avatar currentPersonaAvatar = personaAnimator.avatar;

                // get the new animator
                Animator newAnimator = newModel.GetComponent<Animator>();
                // extract the new avatar from the new model
                Avatar newPersonaAvatar = newAnimator.avatar;

                // appy new avatar to persona
                personaAnimator.avatar = newPersonaAvatar;
                // delete old model
                DestroyImmediate(currentModel);
                // delete the new Animator
                DestroyImmediate(newAnimator);
            }

        }

        #endregion


        #region Tools

        GameObject CreateObject(UnityEngine.Object prefab, GameObject parent, string objectTitle)
        {
            return CreateObject(prefab, parent.transform, objectTitle);
        }

        GameObject CreateObject(UnityEngine.Object prefab, Transform parent, string objectTitle)
        {
            // use the name of the prefab if it isn't defined
            if (objectTitle == null || objectTitle == "")
            {
                objectTitle = prefab.name;
            }
            // ok, create the gameObject
            GameObject summaryObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            // construct this object
            summaryObject.name = objectTitle;
            summaryObject.transform.localPosition = Vector3.zero;   // FIXME: this fixes a randomesque behavior
            summaryObject.transform.localScale = Vector3.one;       // FIXME: this fixes a randomesque behavior
            // attach this object to the right container
            summaryObject.transform.SetParent(parent, false);
            summaryObject.transform.SetSiblingIndex(0);             // FIXME: This should be reversed

            return summaryObject;
        }


        /// <summary>
        /// Finds a GameObject. If the GameObject is not found, it will Instantiate() a new one based on the designated Prefab asset.
        /// </summary>
        /// <returns>The GameObject.</returns>
        /// <param name="heirarchyPath">The path to the GameObject in the heirarchy.</param>
        /// <param name="objectName">A name for the GameObject.</param>
        /// <param name="prefabObject">Prefab object.</param>
        /// <param name="shouldReplace">If set to <c>true</c>, the previously instantiated GameObject should replace.</param>

        GameObject FindMakeGameObject(string heirarchyPath, string objectName, UnityEngine.Object prefabObject, bool shouldReplace = false)
        {
            GameObject obj = null;
            GameObject previousGameObject = GameObject.Find(heirarchyPath + "/" + objectName);
            // should we replace this game object, or just redefine it
            if (previousGameObject != null && shouldReplace)
            {
                // destroy the previous object
                DestroyImmediate(previousGameObject);
                // if we're creating an empty object
                if (prefabObject == null)
                {   // create it
                    prefabObject = new GameObject();
                }
                obj = PrefabUtility.InstantiatePrefab(prefabObject) as GameObject;
                //                      Debug.Log ("Replacing");

            }
            else if (previousGameObject != null)
            {
                obj = previousGameObject;
                //                      Debug.Log ("Remapping");

            }
            else
            {
                // if we're creating an empty object
                if (prefabObject == null)
                {   // create it
                    prefabObject = new GameObject();
                }
                // 
                obj = PrefabUtility.InstantiatePrefab(prefabObject) as GameObject;
                //                      Debug.Log ("Creating");
            }

            obj.name = objectName;
            // make sure we have a parent
            if (heirarchyPath != "")
            {
                // attach this object to the parent object
                GameObject rootObject = GameObject.Find(heirarchyPath);
                //              Debug.Log (unityPath + "\t" + rootObject + "\t" + obj);
                obj.transform.SetParent(rootObject.transform, false);
            }

            return obj;
        }

        #endregion
    }

}
