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
    public float QrCodeMarkerSize = 0.1f;
    public float ArucoMarkerSize = 0.1f;
    public MLMarkerTracker.MarkerType Type = MLMarkerTracker.MarkerType.QR;
    public MLMarkerTracker.ArucoDictionaryName ArucoDict = MLMarkerTracker.ArucoDictionaryName.DICT_5X5_100;
    private Dictionary<string, GameObject> _markers = new Dictionary<string, GameObject>();
    private ASCIIEncoding _asciiEncoder = new System.Text.ASCIIEncoding();

    [Tooltip("The object that will be instantiated.")]
    public GameObject trackerObject;
    [Tooltip("This is the object that will act as the segmented dimmer object.")]
    //public GameObject dimmerObject;
    //[Tooltip("A string that will be used to define the individual marker. Your marker needs to return the same string.")]
    public string markerID;

    //public float qrCodeMarkerSize; // size of the expected qr code, in meters
    //public float arucoMarkerSize; // size of the expected aruco marker, in meters
    //public MLMarkerTracker.MarkerType markerType; // QR? Aruco? EAN_13? etc
    //public MLMarkerTracker.ArucoDictionaryName arucoDict; // for Aruco markers, which "dictionary" or type of Aruco markers?

    //bool to check if scanning should be active or not
    private bool isScanning;

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
        if (!MLPermissions.CheckPermission(MLPermission.MarkerTracking).IsOk)
        {
            Debug.LogError("Cannot start marker tracker." + MLPermission.MarkerTracking + " permission not found in manifest.");
            this.enabled = false;
            return;
        }
        MLMarkerTracker.TrackerSettings trackerSettings = MLMarkerTracker.TrackerSettings.Create(
            true, Type, QrCodeMarkerSize, ArucoDict, ArucoMarkerSize, MLMarkerTracker.Profile.Default);
        _ = MLMarkerTracker.SetSettingsAsync(trackerSettings);

        Debug.Log("Start tracking");

        menuController.onMarkerButtonPress += StartScanning;

    }

    // when the marker is detected...
    private void OnTrackerResultsFound(MLMarkerTracker.MarkerData data)
    {
       if(markerFeature)
        { 
        string id = "";
        float markerSize = .01f;
        switch (data.Type)
        {
            case MLMarkerTracker.MarkerType.Aruco_April:
                id = data.ArucoData.Id.ToString();
                markerSize = ArucoMarkerSize;
                break;
            case MLMarkerTracker.MarkerType.QR:
                id = _asciiEncoder.GetString(data.BinaryData.Data, 0, data.BinaryData.Data.Length);
                markerSize = QrCodeMarkerSize;
                break;
            case MLMarkerTracker.MarkerType.EAN_13:
            case MLMarkerTracker.MarkerType.UPC_A:
                id = _asciiEncoder.GetString(data.BinaryData.Data, 0, data.BinaryData.Data.Length);
                Debug.Log("No pose is given for marker type " + data.Type + " value is " + data.BinaryData.Data);
                break;
        }
        if (!string.IsNullOrEmpty(id))
        {
            if (_markers.ContainsKey(id))
            {
                GameObject marker = _markers[id];
                marker.transform.position = data.Pose.position;
                //marker.transform.rotation = data.Pose.rotation;
            }
            else
            {
                if (id == markerID)
                {
                    trackerObject.transform.localScale = new Vector3(markerSize, markerSize, markerSize);
                    trackerObject.SetActive(true);
                    _markers.Add(id, trackerObject);
                    Debug.Log("Marker Found");
                    Debug.Log("Marker ID data is " + id);
                }
            }
        }
     }
    }

    private void StartScanning(bool activeState)
    {
        Debug.Log("Hey it's John Lemon!!!");
        markerFeature = activeState;
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
