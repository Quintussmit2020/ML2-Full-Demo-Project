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


    void Start()
    {
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

        MLSegmentedDimmer.Activate();
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
