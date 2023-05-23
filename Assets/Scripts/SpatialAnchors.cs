using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.MagicLeap;

public class SpatialAnchors : MonoBehaviour
{
    [Tooltip("How often, in seconds, to check if localization has changed.")]
    public float SearchInterval = 10;

    //Track the objects we already created to avoid duplicates
    private Dictionary<string, int> _persistentObjectsById = new Dictionary<string, int>();

    private string _localizedSpace;

    //Spatial Anchor properties
    private MLAnchors.Request _spatialAnchorRequest;
    private MLAnchors.Request.Params _anchorRequestParams;
    private Pose publicStickyPose;
    private MLAnchors.Anchor anchor;

    //Used to force search localization even if the current time hasn't expired
    private bool _searchNow;
    //The timestamp when anchors were last searched for
    private float _lastTick;

    //The amount of searches that were performed.
    //Used to make sure anchors are fully localized before instantiating them.
    private int numberOfSearches;

    [Tooltip("Art prefab.")]
    public GameObject artPiece;

    void Start()
    {
        //Load JSON Data from file at start
        SimpleAnchorBinding.Storage.LoadFromFile();

        _spatialAnchorRequest = new MLAnchors.Request();
        CloseObject.InstanceIDEvent += CloseObject_InstanceIDEvent;
    }

    private void OnDestroy()
    {
        CloseObject.InstanceIDEvent -= CloseObject_InstanceIDEvent;
    }

    /*Gets the event fired by the close button on the script "CloseObject" and receives the ID of the
 gameobject that was closed. 
   */
    private void CloseObject_InstanceIDEvent(int instanceID)
    {
        string anchorToDestroy = _persistentObjectsById.FirstOrDefault(x => x.Value == instanceID).Key;
        RemoveAnchor(anchorToDestroy);

        Debug.Log("Instance ID received: " + instanceID + " and anchor is " + anchorToDestroy);
    }




    //public bool DeleteAnchor(int instanceID)
    //{
    //    Debug.Log("Delete acnor is running");
    //    string anchorToDestroy = _persistentObjectsById.FirstOrDefault(x => x.Value == instanceID).Key;
    //    if (anchorToDestroy != null)
    //    {
    //        Debug.Log("Anchor to destroy is" + anchorToDestroy);
    //    }
    //    else
    //    {
    //        Debug.Log("Anchor to destroy iswas not found");
    //    }
        
    //    //Delete the anchor using the Anchor's ID
    //    var savedAnchor = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == anchorToDestroy);
    //    //Delete the gameObject if it exists
    //    if (savedAnchor != null)
    //    {
    //        MLAnchors.Anchor.DeleteAnchorWithId(anchorToDestroy);
    //        savedAnchor.UnBind();
    //        SimpleAnchorBinding.Storage.SaveToFile();
    //        Debug.Log("Instance ID received: " + instanceID + " and anchor is " + anchorToDestroy);
    //        return true;
    //    }
    //    else
    //    {
    //        return false;
    //    }

        
    //}



    // Update is called once per frame
    void Update()
    {
        //Debug.Log(artPiece.name);
    }

    void LateUpdate()
    {
        // Only search when the update time lapsed 
        if (!_searchNow && Time.time - _lastTick < SearchInterval)
            return;

        _lastTick = Time.time;

        MLResult mlResult = MLAnchors.GetLocalizationInfo(out MLAnchors.LocalizationInfo info);
        if (!mlResult.IsOk)
        {
            Debug.Log("Could not get localization Info " + mlResult);
            return;
        }

        if (info.LocalizationStatus == MLAnchors.LocalizationStatus.NotLocalized)
        {
            //Clear the old visuals
            ClearVisuals();
            _localizedSpace = "";
            numberOfSearches = 0;
            Debug.Log("Not Localized " + info.LocalizationStatus);
            return;
        }

        //If we are in a new space or have not localized yet then try to localize
        if (info.SpaceId != _localizedSpace)
        {
            ClearVisuals();
            if (Localize())
            {
                _localizedSpace = info.SpaceId;
            }
        }
    }

    private void ClearVisuals()
    {
        foreach (var prefab in _persistentObjectsById.Values)
        {
            GameObject objToDestroy = GameObject.FindObjectsOfType<GameObject>().FirstOrDefault(obj => obj.GetInstanceID() == prefab);

            if (objToDestroy != null)
            {
                Destroy(objToDestroy);
            }

            //Destroy(prefab);
        }
        _persistentObjectsById.Clear();
    }

    public void SearchNow()
    {
        _searchNow = true;
    }


    private bool Localize()
    {

        MLResult startStatus = _spatialAnchorRequest.Start(new MLAnchors.Request.Params(Camera.main.transform.position, 100, 0, false));
        numberOfSearches++;

        if (!startStatus.IsOk)
        {
            Debug.LogError("Could not start" + startStatus);
            return false;
        }


        MLResult queryStatus = _spatialAnchorRequest.TryGetResult(out MLAnchors.Request.Result result);

        if (!queryStatus.IsOk)
        {
            Debug.LogError("Could not get result " + queryStatus);
            return false;
        }

        //Wait a search to make sure anchors are initialized
        if (numberOfSearches <= 1)
        {
            Debug.LogWarning("Initializing Anchors");
            //Search again
            _searchNow = true;
            return false;
        }


        for (int i = 0; i < result.anchors.Length; i++)
        {
            /*grab the list of anchors and the related gameobjects for each*/
            MLAnchors.Anchor anchor = result.anchors[i];
            var savedAnchor = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == anchor.Id);
            if (savedAnchor != null && _persistentObjectsById.ContainsKey(anchor.Id) == false)
            {
                /* go through the list, check the type of prefab that was at the position, and instantiate it again
                AtlasPopulationMode the same position */
                if (savedAnchor.JsonData == "ArtpieceTemplate(Clone)")
                {
                    Debug.Log("Creating " + savedAnchor.JsonData);
                    var persistentObject = Instantiate(artPiece, anchor.Pose.position, anchor.Pose.rotation);
                    ////Grab the text from the saved file for the stickynote
                    //TextMeshProUGUI stickyText = persistentObject.GetComponentInChildren<TextMeshProUGUI>();
                    //stickyText.text = savedAnchor.StickyText;
                    /* create a list of instantiated objects for this session. This is so that we 
                     * can check later which one was closed and delete the associated anchor*/

                    _persistentObjectsById.Add(anchor.Id, persistentObject.GetInstanceID());
                }

            }
        }

        return true;
    }

    //Returns true if the ID existed in the localized space and in the saved data
    private bool RemoveAnchor(string id)
    {
        //Delete the anchor using the Anchor's ID
        var savedAnchor = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == id);
        //Delete the gameObject if it exists
        if (savedAnchor != null)
        {

            MLAnchors.Anchor.DeleteAnchorWithId(id);
            savedAnchor.UnBind();
            SimpleAnchorBinding.Storage.SaveToFile();
            return true;
        }

        return false;
    }



    public void SetAnchor(GameObject objectToSave, string textToSave, Pose anchorPose)
    {

        //Create an anchor for the placed object
        MLAnchors.Anchor.Create(anchorPose, 300, out MLAnchors.Anchor anchor);
        //Publish the anchor
        var result = anchor.Publish();
        if (result.IsOk)
        {
            SimpleAnchorBinding savedAnchor = new SimpleAnchorBinding();
            savedAnchor.Bind(anchor, textToSave, objectToSave.name, objectToSave.name);

            _persistentObjectsById.Add(anchor.Id, objectToSave.GetInstanceID());
            SimpleAnchorBinding.Storage.SaveToFile();
        }

    }


}
