using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.ARSubsystems;

public class MarkerController : MonoBehaviour
{
    // marker settings
    // marker settings
    public float qrCodeMarkerSize; // size of the expected qr code, in meters
    public float arucoMarkerSize; // size of the expected aruco marker, in meters
    public MLMarkerTracker.MarkerType markerType; // QR? Aruco? EAN_13? etc
    public MLMarkerTracker.ArucoDictionaryName arucoDict; // for Aruco markers, which "dictionary" or type of Aruco markers?

    // the object that will be instantiated on the marker
    [Tooltip("The object that will be instantiated.")]
    public GameObject trackerObject;

    //[Tooltip("The object that will be instantiated.")]
    //public GameObject trackedObject;
    //[Tooltip("This is the object that will act as the segmented dimmer object.")]
    ////public GameObject dimmerObject;
    //[Tooltip("A string that will be used to define the individual marker. Your marker needs to return the same string.")]
    //public string markerID;


    //bool to check if scanning should be active or not
   // private bool isScanning;

    //Start the ML inputs and controller actions
    private MagicLeapInputs magicLeapInputs;
    private MagicLeapInputs.ControllerActions controllerActions;

    // Permission checking properties
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    public MenuController menuController;

    // Add the Menu Canvas object here
    private bool markerFeature = false;



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

    private void OnEnable()
    {
        MLMarkerTracker.OnMLMarkerTrackerResultsFound += OnTrackerResultsFound;
    }
    private void OnDisable()
    {
        MLMarkerTracker.OnMLMarkerTrackerResultsFound -= OnTrackerResultsFound;
    }

    private void Start()
    {
        // create a tracker settings object with variables defined above
        MLMarkerTracker.TrackerSettings trackerSettings = MLMarkerTracker.TrackerSettings.Create(
            true, markerType, qrCodeMarkerSize, arucoDict, arucoMarkerSize, MLMarkerTracker.Profile.Default);

        // start marker tracking with tracker settings object
        _ = MLMarkerTracker.SetSettingsAsync(trackerSettings);
        Debug.Log("Start tracking");

        menuController.onMarkerButtonPress += StartScanning;

    }

    // when the marker is detected...
    private void OnTrackerResultsFound(MLMarkerTracker.MarkerData data)
    {
       if(markerFeature)
        {
            // instantiate the tracker prefab object and align with worldspace up
            GameObject obj = Instantiate(trackerObject, data.Pose.position, data.Pose.rotation);
            obj.transform.up = Vector3.up;

            // stop scanning after object has been instantiated
            _ = MLMarkerTracker.StopScanningAsync();
        }
    }

    private void StartScanning(bool activeState)
    {
        Debug.Log("Hey it's John Lemon!!!");
        markerFeature = activeState;
        _ = MLMarkerTracker.StartScanningAsync();
    }

    private void OnPermissionGranted(string permission)
    {



    }
    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Planes Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
        enabled = false;
    }


}
