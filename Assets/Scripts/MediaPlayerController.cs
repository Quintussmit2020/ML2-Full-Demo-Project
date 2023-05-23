using MagicLeap.Core;
using MagicLeap.Examples;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;


public class MediaPlayerController : MonoBehaviour
{
    //Floating settings
    [Header("Float settings")]
    [Tooltip("Drag the main camera in here")]
    public Transform cameraTransform;
    [Tooltip("How far should it float from the user")]
    public float distanceFromCamera = 1f;
    [Tooltip("Update speed of the float movement")]
    public float moveSpeed = 5f;

    private bool floatOn = true;

    private bool isPlacing = false;
    [Header("Media player settings")]
    [Tooltip("Drag the medai player behviour object in here")]
    public MLMediaPlayerBehavior mlMediaPlayer;

    // wall placement objects
    public GameObject targetIndicator;
    public GameObject targetObject;


    [Header("Buttons")]
    public Button dockButton;
    public Button floatButton;
    public Button closeButton;
    public Button dimmerButton;

    public GameObject dimmerObject;

    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    [Header("Script settings")]
    public PlaceFromCamera placeFromCamera;
    private ARPlaneManager planeManager;
    [Tooltip("Drag the Menu Canvas object here")]
    public MenuController menuController;
    private bool mediaFeatureActive = false;
    [Tooltip("Drag the Menu Canvas object here")]
    public GameObject menuRoot;
    //public MeshingSubsystemComponent meshingSubsystemComponent;

    private MagicLeapInputs magicLeapInputs;
    public MagicLeapInputs.ControllerActions controllerActions;

    private bool dimmingState = false;


    private void OnEnable()
    {
        // subscribe to permission events
        permissionCallbacks.OnPermissionGranted += PermissionCallbacks_OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += PermissionCallbacks_OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += PermissionCallbacks_OnPermissionDenied;

        planeManager = FindObjectOfType<ARPlaneManager>();
        if (planeManager == null)
        {
            Debug.LogError("Failed to find ARPlaneManager in scene. Disabling Script");
            enabled = false;
        }
        else
        {
            planeManager.enabled = false;
        }
    }

    void Start()
    {
        PlaceFromCamera placeFromCamera = targetObject.GetComponent<PlaceFromCamera>();

        //set up the controller actions
        magicLeapInputs = new MagicLeapInputs();
        magicLeapInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(magicLeapInputs);
        controllerActions.Trigger.performed += Trigger_performed;
        //set up button presses
        dockButton.onClick.AddListener(DockObject);
        floatButton.onClick.AddListener(FloatObject);
        closeButton.onClick.AddListener(ExitMediaPlayer);
        dimmerButton.onClick.AddListener(GlobalDimmerState);

        //if (meshingSubsystemComponent != null)
        //{           
        //        meshingSubsystemComponent = GetComponent<MeshingSubsystemComponent>();
        //        if (meshingSubsystemComponent != null)
        //        {
        //            meshingSubsystemComponent.enabled = false;
        //        }
        //        else
        //        {
        //            Debug.LogError("Meshing Subsystem component not found");
        //        }
        //}
        //else
        //{
        //    Debug.LogError("MeshingController not set");
        //}

        menuController.onMediaButtonPress += OpenPlayer;
   
        targetIndicator.SetActive(false);
        targetObject.SetActive(false);
        placeFromCamera.enabled = true;
        // request spatial mapping permission for plane detection
        MLPermissions.RequestPermission(MLPermission.SpatialMapping, permissionCallbacks);

    }

    private void Menu_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        menuRoot.SetActive(true);
    }

    private void Trigger_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("Trigger pressed");

        if (targetIndicator.activeSelf)
        {
            isPlacing = false;
            floatOn = false;
            targetObject.SetActive(true);
            targetIndicator.SetActive(false);
            targetObject.transform.position = targetIndicator.transform.position;
            targetObject.transform.rotation = targetIndicator.transform.rotation;
            mlMediaPlayer.Play();

        }

    }


    public void GlobalDimmerState()
    {
        dimmingState = !dimmingState;
        if (dimmingState)
        {
            MLGlobalDimmer.SetValue(1);
        }
        else
        {
            MLGlobalDimmer.SetValue(0);

        }
    }



    public void ExitMediaPlayer()
    {
        Debug.Log("Closing player");
        mlMediaPlayer.Pause();
        targetObject.SetActive(false);
        targetIndicator.SetActive(false);
        isPlacing = false;
        floatOn = true;
    }

    private void FloatObject()
    {
        floatOn = true;
        isPlacing = false;
        targetObject.SetActive(true);
        targetIndicator.SetActive(false);
        placeFromCamera.enabled = true;
        floatButton.gameObject.SetActive(false);
        dockButton.gameObject.SetActive(true);

    }


    private void DockObject()
    {
        floatOn = false;
        isPlacing = true;
        mlMediaPlayer.Pause();
        targetObject.SetActive(false);
        targetIndicator.SetActive(true);
        placeFromCamera.enabled = false;
        dockButton.gameObject.SetActive(false);
        floatButton.gameObject.SetActive(true);
    }

    private void OpenPlayer(bool activeState)
    {
        Debug.Log("Grab the popcorn!!!");
        mediaFeatureActive = activeState;
        targetObject.SetActive(true);
        floatButton.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(mediaFeatureActive)
        {
            //Set plane configuration options
            if (planeManager.enabled)
        {
            PlanesSubsystem.Extensions.Query = new PlanesSubsystem.Extensions.PlanesQuery
            {
                BoundsCenter = Camera.main.transform.position,
                BoundsRotation = Camera.main.transform.rotation,
                BoundsExtents = Vector3.one * 20f,
                MaxResults = 100,
                MinPlaneArea = 0.25f
            };
        }

        //Check if the object should be floating or docked on plane
        if (floatOn)
        {
            placeFromCamera.enabled = true;
            //// Calculate the position of the fixation point in world space
            //Vector3 fixationPoint = cameraTransform.position + cameraTransform.forward * distanceFromCamera;

            //// Move the object to the fixation point smoothly
            //transform.position = Vector3.Lerp(transform.position, fixationPoint, moveSpeed * Time.deltaTime);

            //// Rotate the object to face the camera
            //targetObject.transform.LookAt(cameraTransform);
            //targetObject.transform.Rotate(new Vector3(0f, 180f, 0f));
        }

            // raycast from the controller outward
            Ray raycastRay = new Ray(controllerActions.Position.ReadValue<Vector3>(), controllerActions.Rotation.ReadValue<Quaternion>() * Vector3.forward);

            // if ray hits an object on the Planes layer, position the indicator at the hit point and set it as active
            if (isPlacing & Physics.Raycast(raycastRay, out RaycastHit hitInfo, 100, LayerMask.GetMask("Planes")))
            {
                Debug.Log(hitInfo.transform);
                targetIndicator.transform.position = hitInfo.point;
                targetIndicator.transform.rotation = Quaternion.LookRotation(-hitInfo.normal);
                targetIndicator.SetActive(true);
            }
        }
    }

    // if permission denied, disable plane manager
    private void PermissionCallbacks_OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Planes Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
        planeManager.enabled = false;
    }

    // if permission granted, enable plane manager
    private void PermissionCallbacks_OnPermissionGranted(string permission)
    {
        if (permission == MLPermission.SpatialMapping)
        {
            planeManager.enabled = true;
            Debug.Log("Plane manager is active");
        }
    }
}
