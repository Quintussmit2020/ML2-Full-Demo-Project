using MagicLeap.Examples;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;


public class ArtController : MonoBehaviour
{
    public ARPlaneManager planeManager;
    private GameObject artPieceIndicator;
    private GameObject artPiece;
    //public GameObject meshingManager;

    public MenuController menuController;
    //Get reference to the controller script
    private MagicLeapInputs magicLeapInputs;
    public MagicLeapInputs.ControllerActions controllerActions;

    [SerializeField] private GameObject spatialAnchorsScriptObject;
    private SpatialAnchors spatialAnchorsScript;

    private bool isPlacing = false;
    private bool indicatorActive = false;
    private bool artFeature;
    public GameObject[] artPieces;

    private GameObject randomArtPiece;

    public delegate void CreateObjectAnchorEvent(Pose objectPose, int instanceID, string stickyText, string objectType);
    public static event CreateObjectAnchorEvent OnCreateObjectAnchor;


    private void onDestroy ()
    {
        controllerActions.Trigger.canceled -= Trigger_performed;
    }


    void Start()
    {

        //SpatialAnchors spatialAnchorsScript = GameObject.Find("Session Origin").GetComponent<SpatialAnchors>();
        spatialAnchorsScript = spatialAnchorsScriptObject.GetComponent<SpatialAnchors>();

        magicLeapInputs = new MagicLeapInputs();
        magicLeapInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(magicLeapInputs);

        controllerActions.Trigger.canceled += Trigger_performed;
        controllerActions.Bumper.performed += Bumper_performed;

        if (planeManager == null)
        {
            Debug.LogError("Failed to find ARPlaneManager in scene. Disabling Script");
            enabled = false;
        }
        else
        {
            // Activate the ARPlaneManager component
            planeManager.enabled = true;
            Debug.Log("Plane manager is active");
        }
        menuController.onArtButtonPress += OpenArtViewer;
    }

    private void Bumper_performed(InputAction.CallbackContext obj)
    {
        
       if (artFeature)
        { 
        //Randomly select one of the art paintings
        randomArtPiece = artPieces[UnityEngine.Random.Range(0, artPieces.Length)];
        //set the random selection as the indicator object
        artPieceIndicator = Instantiate(randomArtPiece, transform.position, transform.rotation);
        Pose objectPose = new Pose(artPieceIndicator.transform.position, artPieceIndicator.transform.rotation);
        OnCreateObjectAnchor?.Invoke(objectPose, randomArtPiece.GetInstanceID(), "", randomArtPiece.name); // send the create even to the Menu controller script
        indicatorActive = !isPlacing;
        artPieceIndicator.SetActive(indicatorActive);
        isPlacing = indicatorActive;
        }
        else
        {
            artPieceIndicator.SetActive(false);
        }
    }

    private void OpenArtViewer(bool activeState)
    {
        //artPieceIndicator.SetActive(true);
        artFeature = activeState;
    }

    //private void CloseArt()
    //{
    //    Debug.Log("InstanceId was sent" + gameObject.GetInstanceID());
    //    int instanceID = this.gameObject.GetInstanceID();
    //    Destroy(gameObject);
    //}

    // Update is called once per frame
    void Update()
    {
        if (artFeature)
        {

            if (planeManager.enabled)
            {

                PlanesSubsystem.Extensions.Query = new PlanesSubsystem.Extensions.PlanesQuery
                {
                    Flags = planeManager.requestedDetectionMode.ToMLQueryFlags() | PlanesSubsystem.Extensions.MLPlanesQueryFlags.Polygons | PlanesSubsystem.Extensions.MLPlanesQueryFlags.Semantic_Wall,
                    BoundsCenter = Camera.main.transform.position,
                    BoundsRotation = Camera.main.transform.rotation,
                    BoundsExtents = Vector3.one * 20f,
                    MaxResults = 100,
                    MinPlaneArea = 0.25f
                };
            }

            Ray raycastRay = new Ray(controllerActions.Position.ReadValue<Vector3>(), controllerActions.Rotation.ReadValue<Quaternion>() * Vector3.forward);
            if (isPlacing & Physics.Raycast(raycastRay, out RaycastHit hitInfo, 100, LayerMask.GetMask("Planes")))
            {
                artPieceIndicator.transform.position = hitInfo.point;
                //Quaternion newRotation = Quaternion.LookRotation(-hitInfo.normal);
                //newRotation *= Quaternion.Euler(-90f, 0f, 0f);
                //artPieceIndicator.transform.rotation = newRotation;
                artPieceIndicator.transform.rotation = Quaternion.LookRotation(-hitInfo.normal);
            }
        }
    }

    private void Trigger_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("Trigger pulled");

        if (isPlacing)
        {
            
            //var placedArt = Instantiate(artPiece, artPieceIndicator.transform.position, artPieceIndicator.transform.rotation);
            Pose artPose = new Pose(artPieceIndicator.transform.position, artPieceIndicator.transform.rotation);
            //spatialAnchorsScript.SetAnchor(placedArt, "", artPose);
            isPlacing = false;
            artPieceIndicator.SetActive(false);
        }
    }
}
