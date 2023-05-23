using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MagicLeapInputs;
using UnityEngine.XR.MagicLeap;
using UnityEngine.Android;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class StickyNoteController : MonoBehaviour
{
    private MagicLeapInputs magicLeapInputs;
    private MagicLeapInputs.ControllerActions controllerActions;
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    //Delegate for the event that will inform a stickynote when a ray hits it. 
    //Not in use at the moment, this was to add higlighting to selected stickies. Will add later if time permits
    public delegate void StickyRayEventHandler(int stickyID);
    public static event StickyRayEventHandler OnStickyHitEvent;

    [Tooltip("How often, in seconds, to check if localization has changed.")]
    //public float SearchInterval = 10;

    //Track the objects we already created to avoid duplicates
    private Dictionary<string, int> _persistentObjectsById = new Dictionary<string, int>();

    private string _localizedSpace;

    //Spatial Anchor properties
    private MLAnchors.Request _spatialAnchorRequest;
    private MLAnchors.Request.Params _anchorRequestParams;
    private Pose publicStickyPose;
    private MLAnchors.Anchor anchor;

    private Transform cameraTransform;

    //[Header("Meshing")]
    //[Tooltip("Add the mesh controller object.")]
    //public GameObject MeshController;

    [Header("Sticky notes properties")]
    [Tooltip("Sticky note prefab.")]
    public GameObject stickyNote;
    [Tooltip("Sticky note indicator prefab.")]
    public GameObject stickyPlacementIndicator;

    public XRRayInteractor rayInteractor;

    //Creat a list to hold the random sticky text
    List<string> myReminders = new List<string>();

    private bool isPlacing;
    private bool stickyRay = false;
    private bool stickyFeatureActive = false;

    //public GameObject meshController;

    public MenuController menuController;


    public delegate void CreateObjectAnchorEvent(Pose objectPose, int instanceID, string stickyText, string objectType);
    public static event CreateObjectAnchorEvent OnCreateObjectAnchor;


    private void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        //CloseSticky.InstanceIDEvent += CloseSticky_InstanceIDEvent;
    }

    private void OnDestroy()
    {
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
        //CloseSticky.InstanceIDEvent -= CloseSticky_InstanceIDEvent;
    }

    void Start()
    {
        //add some items to our sticky note content list
        myReminders.Add("buy milk");
        myReminders.Add("Call Bob from accounting");
        myReminders.Add("Password: #33$4156");
        myReminders.Add("Check email");
        myReminders.Add("Water plants");
        myReminders.Add("Pick up dry cleaning");
        myReminders.Add("Return library books");
        myReminders.Add("Schedule haircut");
        myReminders.Add("Pay electricity bill");
        myReminders.Add("Make grocery list");
        myReminders.Add("Book flight tickets");
        myReminders.Add("Attend team meeting");
        myReminders.Add("Order office supplies");
        myReminders.Add("Renew car insurance");
        myReminders.Add("Pick up dry cleaning");
        myReminders.Add("Feed cat");
        myReminders.Add("Call doctor");
        myReminders.Add("Write blog post");

        cameraTransform = Camera.main.transform;


        magicLeapInputs = new MagicLeapInputs();
        magicLeapInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(magicLeapInputs);

        //subscribe to the controller button events
        controllerActions.Trigger.canceled += Trigger_performed;
        controllerActions.Bumper.performed += Bumper_performed;
        //controllerActions.Menu.performed += Menu_performed;

        //swich on the meshing object
        //meshController.SetActive(true);

        menuController.onStickyButtonPress += OpenStickyFeature;

    }


    private void OpenStickyFeature(bool activeState)
    {
        stickyFeatureActive = activeState;
        isPlacing = true;
        stickyPlacementIndicator.SetActive(activeState);
    }


    // Update is called once per frame
    void Update()
    {
        if (stickyFeatureActive)
        {


            Ray raycastRay = new Ray(controllerActions.Position.ReadValue<Vector3>(), controllerActions.Rotation.ReadValue<Quaternion>() * Vector3.forward);
            Vector3 newStickyPosition = raycastRay.direction * 1f;
            if (isPlacing & Physics.Raycast(raycastRay, out RaycastHit hitInfo, 100, LayerMask.GetMask("Mesh")))
            {
                stickyPlacementIndicator.transform.position = hitInfo.point;
                stickyPlacementIndicator.transform.rotation = Quaternion.LookRotation(-hitInfo.normal);
                Debug.Log("StickyNote - Currently placing");
            }

            if (stickyRay)
            {
                Physics.Raycast(raycastRay, out RaycastHit stickyHitInfo, 100);
                GameObject hitSticky = stickyHitInfo.collider.gameObject;
                Debug.Log("I just hit " + hitSticky.GetInstanceID());
                OnStickyHitEvent?.Invoke(stickyHitInfo.collider.gameObject.GetInstanceID());
                stickyRay = false;
            }    
        }
    }

    private void Bumper_performed(InputAction.CallbackContext obj)
    {
        if (stickyFeatureActive)
        {
            //check if placing is currently false, if yes then start the indicator phase.
            //This is done so I can use the same button for both phases
                if (!isPlacing)
            {
                stickyPlacementIndicator.SetActive(true);                
                //rayInteractor.raycastMask = 11;
                isPlacing = true;
                Debug.Log("StickyNote - Placing should now be active");
            }
        //if no, start the placing phase
            else
            {
                Pose stickyPose = new Pose(stickyPlacementIndicator.transform.position, stickyPlacementIndicator.transform.rotation);

                stickyPlacementIndicator.SetActive(false);
                isPlacing = false;
                              
                //instantiate a new stickynote in the same location as the placement indicator.     
                var persistentObject = Instantiate(stickyNote, stickyPlacementIndicator.transform.position, stickyPlacementIndicator.transform.rotation);
                TextMeshProUGUI stickyTextToSave = persistentObject.GetComponentInChildren<TextMeshProUGUI>();

                //Grab a random text from the sticky note text dictionary
                string randomText = myReminders[UnityEngine.Random.Range(0, myReminders.Count)];
                stickyTextToSave.text = randomText; //Just some placeholder text for now
                //this is just for debugging, can be removed.
                Debug.Log("StickyNote - this is the persistent object ID" + persistentObject.GetInstanceID());

                Pose objectPose = new Pose(persistentObject.transform.position, persistentObject.transform.rotation);
                OnCreateObjectAnchor?.Invoke(objectPose, persistentObject.GetInstanceID(), stickyTextToSave.text, persistentObject.name); // send the create even to the Menu controller script

            }
        }
    }

    private void Trigger_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (stickyFeatureActive)
        {
            //going to add keyboard stuff here
            stickyRay = true;
        }

    }



    private void OnEnable()
    {
       // MenuController.onStickyNoteButtonPress += OnStickyNoteButtonPressHandler;
    }



    private void OnDisable()
    {
        //MenuController.onStickyNoteButtonPress -= OnStickyNoteButtonPressHandler;
    }



    private void OnPermissionGranted(string permission)
    {

    }


    private void OnPermissionDenied(string permission)
    {

    }

}
