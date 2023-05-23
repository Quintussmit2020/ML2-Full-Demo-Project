using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEditor.Rendering;

public class PermissionsChecker : MonoBehaviour
{
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();
    public Button audioSettingsButton;
    public Button continueButton;

    public TextMeshProUGUI textAudioIntents;
    public TextMeshProUGUI textAudioPermissions;

    public GameObject spatialMenu;

    // subscribe to permission events
    private void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
    }


    // unsubscribe from permission events
    private void OnDestroy()
    {
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }

    // Start is called before the first frame update
    void Start()
    {
        MLPermissions.RequestPermission(MLPermission.VoiceInput, permissionCallbacks);
      
        audioSettingsButton.onClick.AddListener(OpenAudioSettings);
        continueButton.onClick.AddListener(ContinueToSpatial);


        audioSettingsButton.gameObject.SetActive(false);
        //continueButton.gameObject.SetActive(false);
    }

    private void ContinueToSpatial()
    {
        spatialMenu.SetActive(true);
        this.gameObject.SetActive(false);        
    }

    private void OpenAudioSettings()
    {
        UnityEngine.XR.MagicLeap.SettingsIntentsLauncher.LaunchSystemVoiceInputSettings();
        Application.Quit();
    }

    // on voice permission denied, disable script
    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to initialize voice intents due to missing or denied {MLPermission.VoiceInput} permission. Please add to manifest. Disabling script.");
        enabled = false;
        textAudioPermissions.text = "Audio permissions: Not set, enable in manifest";
    }

    // on voice permission granted, initialize voice input
    private void OnPermissionGranted(string permission)
    {
        if (permission == MLPermission.VoiceInput)
            textAudioPermissions.text = "Audio permissions: Ok";
            InitializeVoiceInput();
        
        // To do spatial permissions check - get it from stickyplacement SpatialCheck method

    }
    // check if voice commands setting is enabled, then set up voice intents
    private void InitializeVoiceInput()
    {
        bool isVoiceEnabled = MLVoice.VoiceEnabled;

        // if voice setting is enabled, try to set up voice intents
        if (isVoiceEnabled)
        {
            Debug.Log("Voice commands setting is enabled");
            textAudioIntents.text = "Audio intents: Ok";
            continueButton.gameObject.SetActive(true);

        }

        
        else
            gameObject.SetActive(true);
    }

}
