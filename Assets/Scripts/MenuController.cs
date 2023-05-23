using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;
using UnityEngine.InputSystem.Android;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;

public class MenuController : MonoBehaviour
{
    //Floating settings
    public Transform cameraTransform;
    public float distanceFromCamera = 1f;
    public float moveSpeed = 5f;

    private bool handsFeature = false;


    public Button closeButton;
    public Button mediaPlayerButton;
    public Button stickyNoteButton;
    public Button placeArtButton;
    public Button cubeButton;
    public Button webFeatureButton;
    public Button markerFeatureButton;
    public Button handFeatureButton;

    // wall placement objects
    public GameObject menuIndicator;
    public GameObject menuRoot;

    ControllerActionsManager controllerActionsManager;

    // voice intents configuration instance (needs to be assigned in Inspector)
    public MLVoiceIntentsConfiguration VoiceIntentsConfiguration;

    //public UnityEvent onMediaButtonPress;  // Define the event
    public event Action<bool> onMediaButtonPress;
    public event Action<bool> onWebButtonPress;
    public event Action<bool> onArtButtonPress;
    public event Action<bool> onCubebButtonPress;
    public event Action<bool> onStickyButtonPress;
    public event Action<bool> onMarkerButtonPress;
    public event Action<bool> onHandButtonPress;


    // public static event Action<Pose> onStickyNoteButtonPress;



    private MagicLeapInputs magicLeapInputs;
    private MagicLeapInputs.ControllerActions controllerActions;
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    public GameObject artPlacementFeature;
    public GameObject cubesFeature;
    public GameObject mediaPlayerFeature;
    public GameObject stickyNoteFeature;
    public GameObject webFeature;

    //Settings for localization and persistent saving
    [Tooltip("How often, in seconds, to check if localization has changed.")]
    public float SearchInterval = 10;
    //Track the objects we already created to avoid duplicates
    private Dictionary<string, int> _persistentObjectsById = new Dictionary<string, int>();
    //Track the type of object we created
    private Dictionary<string, string> _persistentObjectsType = new Dictionary<string, string>();

    private string _localizedSpace;
    //Spatial Anchor properties
    private MLAnchors.Request _spatialAnchorRequest;
    private MLAnchors.Request.Params _anchorRequestParams;
    private Pose objectPose;
    private MLAnchors.Anchor anchor;
    //Used to force search localization even if the current time hasn't expired
    private bool _searchNow;
    //The timestamp when anchors were last searched for
    private float _lastTick;
    //The amount of searches that were performed.
    //Used to make sure anchors are fully localized before instantiating them.
    private int numberOfSearches;
    [Tooltip("Drag all the different type of objects that can be placed in the scene into this array.")]
    public GameObject[] objectsToInstantiate;
    [Tooltip("Drag the stickynote prefab into this slot as well.")]
    public GameObject stickyNote;


    private void OnEnable()
    {
        ArtController.OnCreateObjectAnchor += CreateObjectAnchor;
        StickyNoteController.OnCreateObjectAnchor += CreateObjectAnchor;
        CloseObject.InstanceIDEvent += CloseObjectEvent;
    }

    private void OnDisable()
    {
        ArtController.OnCreateObjectAnchor -= CreateObjectAnchor;
        StickyNoteController.OnCreateObjectAnchor -= CreateObjectAnchor;
        CloseObject.InstanceIDEvent -= CloseObjectEvent;
    }

    void Start()
    {
        //Load JSON Data from file at start
        SimpleAnchorBinding.Storage.LoadFromFile();
        // First check for spatialization
        SpatialCheck();
        _spatialAnchorRequest = new MLAnchors.Request();

        menuRoot.SetActive(true);
        closeButton.onClick.AddListener(CloseMenu);
        mediaPlayerButton.onClick.AddListener(MediaPLayerActivate);
        stickyNoteButton.onClick.AddListener(StickyNoteActivate);
        placeArtButton.onClick.AddListener(ArtPlacementActivate);
        cubeButton.onClick.AddListener(CubesActivate);
        webFeatureButton.onClick.AddListener(WebActivate);
        markerFeatureButton.onClick.AddListener(MarkerActivate);
        handFeatureButton.onClick.AddListener(HandsActivate);

       //Start the ML2 inputs
        magicLeapInputs = new MagicLeapInputs();
        magicLeapInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(magicLeapInputs);

        //set up button presses
        controllerActions.Menu.performed += Menu_performed;

        MLPermissions.RequestPermission(MLPermission.VoiceInput, permissionCallbacks);
    

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

    private bool Localize()
        //this function recreates all the gameobjects that were saved in the JSON file when the app is started up
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
            var savedAnchor = SimpleAnchorBinding.Storage.Bindings.Find(x => x.Id == anchor.Id); //Find the anchor ID in the saved file 
            if (savedAnchor != null && _persistentObjectsById.ContainsKey(anchor.Id) == false) //If the anchor ID is in the saved file and the object hasn't been instantiated yet
            {

                //for each saved anchor, instantiate the correct prefab 

                foreach (GameObject obj in objectsToInstantiate)
                {
                    if (savedAnchor.JsonData == obj.name)
                    {
                        if (obj.name == "StickyNote")
                        {
                            var persistentObject = Instantiate(stickyNote, anchor.Pose.position, anchor.Pose.rotation);
                            //Grab the text from the saved file for the stickynote
                            TextMeshProUGUI stickyText = persistentObject.GetComponentInChildren<TextMeshProUGUI>();
                            stickyText.text = savedAnchor.StickyText;
                            /* create a list of instantiated objects for this session. This is so that we 
                             * can check later which one was closed and delete the associated anchor*/
                            _persistentObjectsById.Add(anchor.Id, persistentObject.GetInstanceID());
                        }
                        else
                        {
                            var persistentObject = Instantiate(obj, anchor.Pose.position, anchor.Pose.rotation);
                            //create a list of instantiated objects for this session. This is so that we can check later which one was closed and delete the associated anchor
                            _persistentObjectsById.Add(anchor.Id, persistentObject.GetInstanceID());
                            _persistentObjectsType.Add(anchor.Id, obj.name);
                        }
                    }

                }

            }
        }

        return true;
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

    private void SpatialCheck()
    {
        var result = MLPermissions.CheckPermission(MLPermission.SpatialAnchors);

        if (result.IsOk)
        {
            MLResult mlResult = MLAnchors.GetLocalizationInfo(out MLAnchors.LocalizationInfo info);
#if !UNITY_EDITOR
            if (info.LocalizationStatus == MLAnchors.LocalizationStatus.NotLocalized)
            {
                Debug.Log("Localisation not set up");
                //Opens the space app on th device
                UnityEngine.XR.MagicLeap.SettingsIntentsLauncher.LaunchSystemSettings("com.magicleap.intent.action.SELECT_SPACE");
                Application.Quit();
            }
            else
            {
                //debug print the error
                Debug.Log("GetLocalizationInfo Error " + mlResult);
            }
#endif

            //If we were able to get the localization info, debug it.
            if (mlResult.IsOk)
            {

                Debug.Log("GetLocalizationInfo " + mlResult);
            }
            else
            {
                Debug.Log("GetLocalizationInfo Error " + mlResult);
            }
        }
    }


    private void CloseObjectEvent(int instanceID)
    {
        /*Gets the event fired by the close button on the sticky note script "CloseSticky" and receives the ID of the
        gameobject that was closed. 
        */
        string anchorToDestroy = _persistentObjectsById.FirstOrDefault(x => x.Value == instanceID).Key;
        RemoveAnchor(anchorToDestroy);

        Debug.Log("Instance ID received: " + instanceID + " and anchor is " + anchorToDestroy);
    }

    public void SetAnchor(Pose activeObjectPose)
    {

        Pose objectPose = new Pose(activeObjectPose.position, activeObjectPose.rotation);
        // Create a new anchor at the location of the controller.
        MLAnchors.Anchor.Create(objectPose, 300, out MLAnchors.Anchor anchor);
        // Publish the anchor to the map after it is created.
        anchor.Publish();
        Debug.Log("anchor created at" + anchor);
    }

        
    private bool RemoveAnchor(string id)
    //Returns true if the ID existed in the localized space and in the saved data
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

    public void CreateObjectAnchor(Pose ObjectPose, int instanceID, string stickyText, string objectType)
    {
        Pose objectPose = new Pose(ObjectPose.position, ObjectPose.rotation);
        // Create a new anchor at the location of the controller.
        MLAnchors.Anchor.Create(objectPose, 300, out MLAnchors.Anchor anchor);
        // Publish the anchor to the map after it is created.
        var result = anchor.Publish();
        if (result.IsOk)
        {
            //Save the anchor to the persistent storage
            SimpleAnchorBinding savedAnchor = new SimpleAnchorBinding();
            savedAnchor.Bind(anchor, instanceID.ToString(), stickyText, objectType);
            SimpleAnchorBinding.Storage.SaveToFile();
            //Add the anchor to the dictionary to link the instance ID and the anchor ID for recreating later if scene is loaded again
            _persistentObjectsById.Add(instanceID.ToString(), instanceID);
            //add the anchor to the dictionary to link the instance ID and the object type for recreating later if scene is loaded again
            _persistentObjectsType.Add(instanceID.ToString(), objectType);
            Debug.Log("anchor created at" + anchor);
        }
        else
        {
            Debug.Log("anchor creation failed" + result);
        }
    }


    private void HandsActivate()
    {
        if (!handsFeature)
        {
            //menuRoot.SetActive(false);
            onHandButtonPress?.Invoke(true);
            handsFeature = true;
            TMP_Text buttonText = handFeatureButton.GetComponentInChildren<TMP_Text>();
            buttonText.text = "Hand tracking off";
        }
        else
        {
            onHandButtonPress?.Invoke(false);
            handsFeature = false;
            TMP_Text buttonText = handFeatureButton.GetComponentInChildren<TMP_Text>();
            buttonText.text = "Hand tracking on";
        }

    }

    private void MarkerActivate()
    {
        menuRoot.SetActive(false);
        onMarkerButtonPress?.Invoke(true);

    }
    private void WebActivate()
    {
        menuRoot.SetActive(false);
        onWebButtonPress?.Invoke(true);
        
    }


    private void MediaPLayerActivate()
    {
        onMediaButtonPress?.Invoke(true);
        menuRoot.SetActive(false);
    }


    private void CubesActivate()
    {
        onCubebButtonPress?.Invoke(true);
        menuRoot.SetActive(false);
    }

    private void ArtPlacementActivate()
    {
        onArtButtonPress?.Invoke(true);
        menuRoot.SetActive(false);
    }
        private void StickyNoteActivate()
    {
        onStickyButtonPress?.Invoke(true);
        menuRoot.SetActive(false);
    }

    private void Menu_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {        
       
            if (menuRoot.activeSelf)
            {
                menuRoot.SetActive(false);
            }
            else
            {
                menuRoot.SetActive(true);
                menuRoot.SetActive(true);
                onCubebButtonPress?.Invoke(false);
                onArtButtonPress?.Invoke(false);
                onStickyButtonPress?.Invoke(false);
                onArtButtonPress?.Invoke(false);
                //onMediaButtonPress?.Invoke(false);
                onMarkerButtonPress?.Invoke(false);
            }
        

    }

    void Update()
    {
        // Calculate the position of the fixation point in world space
        Vector3 fixationPoint = cameraTransform.position + cameraTransform.forward * distanceFromCamera;

        // Move the object to the fixation point smoothly
        transform.position = Vector3.Lerp(transform.position, fixationPoint, moveSpeed * Time.deltaTime);

        // Rotate the object to face the camera
        transform.LookAt(cameraTransform);
        transform.Rotate(new Vector3(0f, 180f, 0f));
    }

    void CloseMenu()
    {
        UnityEngine.Debug.Log("Close button pressed");
        // Close the object by setting it to inactive
        menuRoot.SetActive(false);
    }

   //void Mediaplayer()
   // {
   //     Debug.Log("Media player button pressed");
   //     onMediaButtonPress?.Invoke();  // Raise the event
   //     //MediaPLayerActivate();
   //     menuRoot.SetActive(false);
   // }

    private void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
    }

    private void OnDestroy()
    {
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }
    private void OnPermissionGranted(string permission)
    {
        if (permission == MLPermission.VoiceInput)
            InitializeVoiceInput();
        UnityEngine.Debug.Log("Voice commands permission granted");

    }




    private void OnPermissionDenied(string permission)
    {
        UnityEngine.Debug.LogError($"Failed to initialize voice intents due to missing or denied {MLPermission.VoiceInput} permission. Please add to manifest. Disabling script.");
        enabled = false;
    
    }
    private void InitializeVoiceInput()
    {
        bool isVoiceEnabled = MLVoice.VoiceEnabled;

        // if voice setting is enabled, try to set up voice intents
        if (isVoiceEnabled)
        {
            var result = MLVoice.SetupVoiceIntents(VoiceIntentsConfiguration);
            if (result.IsOk)
            {
                MLVoice.OnVoiceEvent += MLVoiceOnOnVoiceEvent;
                UnityEngine.Debug.Log("Voice intents activated");
            }
            else
            {
                UnityEngine.Debug.LogError("Voice commands could not initialize:" + result);
            }
        }

        // if voice setting is disabled, open voice settings so user can enable it
        else
        {
            UnityEngine.Debug.Log("Voice commands setting is disabled - opening settings");
            UnityEngine.XR.MagicLeap.SettingsIntentsLauncher.LaunchSystemVoiceInputSettings();
            Application.Quit();
        }
    }

    private void MLVoiceOnOnVoiceEvent(in bool wasSuccessful, in MLVoice.IntentEvent voiceEvent)
    {
        UnityEngine.Debug.Log("Voice commands - listening");
        if (wasSuccessful)
    
            UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
        {
            //if (voiceEvent.EventID == 101)
            //{
            //    Debug.Log("Voice commands - open menu");
            //    menuRoot.SetActive(true);

            //}
            //if (voiceEvent.EventID == 102)
            //{
            //    Debug.Log("Voice commands - close menu");
            //    menuRoot.SetActive(false);
            //}
            //if (voiceEvent.EventID == 103)
            //{
            //    Debug.Log("Voice commands - close menu");
            //    onWebButtonPress?.Invoke(true);
            //}

            switch (voiceEvent.EventID)
            {
                case 101:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    menuRoot.SetActive(true);
                    break;

                case 102:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    menuRoot.SetActive(false);
                    break;

                case 103:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onWebButtonPress?.Invoke(true);
                    menuRoot.SetActive(false);
                    break;
                case 104:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onWebButtonPress?.Invoke(false);

                    break;
                case 105:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onArtButtonPress?.Invoke(true);
                    menuRoot.SetActive(false);
                    break;
                case 106:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onArtButtonPress?.Invoke(false);
                    break;
                case 107:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onMarkerButtonPress?.Invoke(true);
                    menuRoot.SetActive(false);
                    break;
                case 108:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onMarkerButtonPress?.Invoke(false);
                    break;
                case 109:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onCubebButtonPress?.Invoke(true);
                    menuRoot.SetActive(false);
                    break;
                case 110:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onCubebButtonPress?.Invoke(false);
                    break;
                case 111:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onStickyButtonPress?.Invoke(true);
                    menuRoot.SetActive(false);
                    break;
                case 112:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onStickyButtonPress?.Invoke(false);
                    break;
                case 113:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onMediaButtonPress?.Invoke(true);
                    menuRoot.SetActive(false);
                    break;
                case 114:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onMediaButtonPress?.Invoke(false);
                    break;
                case 115:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onHandButtonPress?.Invoke(true);
                    TMP_Text buttonText = handFeatureButton.GetComponentInChildren<TMP_Text>();
                    buttonText.text = "Hand tracking off";
                    break;
                case 116:
                    UnityEngine.Debug.Log("Voice commands was " + voiceEvent.EventID);
                    onHandButtonPress?.Invoke(false);
                    TMP_Text buttonText2 = handFeatureButton.GetComponentInChildren<TMP_Text>();
                    buttonText2.text = "Hand tracking on";
                    break;
                default:
                    break;
            }
        }
    }

}
