using MagicLeap.Examples;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;


public class StickyNoteController2 : MonoBehaviour
{
    public ARPlaneManager planeManager;
    public GameObject stickyIndicator;
    public GameObject stickyNote;
    public GameObject meshingManager;

    public MenuController menuController;
    //Get reference to the controller script
    private MagicLeapInputs magicLeapInputs;
    public MagicLeapInputs.ControllerActions controllerActions;

    [SerializeField] private GameObject spatialAnchorsScriptObject;
    private SpatialAnchors spatialAnchorsScript;

    private bool isPlacing = false;
    private bool indicatorActive = false;
    private bool stickyNoteFeature;



    private void onDestroy ()
    {
        controllerActions.Trigger.canceled -= Trigger_performed;
    }


    void Start()
    {
        //Switch off meshing manager

        if (meshingManager.activeSelf)
        {
            meshingManager.SetActive(false);
        }

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
        menuController.onStickyButtonPress += OpenStickyFeature;

    }

    private void Bumper_performed(InputAction.CallbackContext obj)
    {
        
       if (stickyNoteFeature)
        { 
        indicatorActive = !isPlacing;
        stickyIndicator.SetActive(indicatorActive);
        isPlacing = indicatorActive;
        }
        else
        {
            stickyIndicator.SetActive(false);
        }
    }

    private void OpenStickyFeature(bool activeState)
    {
        Debug.Log("Im free!!!");
        stickyIndicator.SetActive(true);
        stickyNoteFeature = activeState;
    }



    // Update is called once per frame
    void Update()
    {
        if (stickyNoteFeature)
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
                stickyIndicator.transform.position = hitInfo.point;
                stickyIndicator.transform.rotation = Quaternion.LookRotation(-hitInfo.normal);
            }
        }
    }

    private void Trigger_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("Trigger pulled");

        if (isPlacing)
        {
            var newStickyNote = Instantiate(stickyNote, stickyIndicator.transform.position, stickyIndicator.transform.rotation);
            Pose stickyPose = new Pose(stickyIndicator.transform.position, stickyIndicator.transform.rotation);
            isPlacing = false;
            stickyIndicator.SetActive(false);
        }
    }
}
