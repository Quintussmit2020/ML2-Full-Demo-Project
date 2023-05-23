using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

using UnityEngine.XR.MagicLeap;



public class MeshControllerSample : MonoBehaviour
{

    
    public MeshingSubsystemComponent meshingSubsystemComponent;
    private readonly MLPermissions.Callbacks mlPermissionsCallbacks = new MLPermissions.Callbacks();
    private bool meshActive;
 
   
   

    private void Awake()
    {
        mlPermissionsCallbacks.OnPermissionGranted += MlPermissionsCallbacks_OnPermissionGranted;
        mlPermissionsCallbacks.OnPermissionDenied += MlPermissionsCallbacks_OnPermissionDenied;
        mlPermissionsCallbacks.OnPermissionDeniedAndDontAskAgain += MlPermissionsCallbacks_OnPermissionDenied;
        MLPermissions.RequestPermission(MLPermission.SpatialMapping, mlPermissionsCallbacks);
        meshingSubsystemComponent = GetComponent<MeshingSubsystemComponent>();

        if (meshingSubsystemComponent != null)
        {
            Debug.Log("Meshing - Subsystem component found");
        }
        //if (meshingSubsystemComponent.enabled)
        //{
        //    meshActive = true;
        //}
        //else
        //{
        //    meshActive = false; 
        //}
    }

    void Start()
    {
        // request permission at start
        MLPermissions.RequestPermission(MLPermission.SpatialMapping, mlPermissionsCallbacks);

        // get meshing subsystem
        meshingSubsystemComponent = FindObjectOfType<MeshingSubsystemComponent>();
        meshingSubsystemComponent.enabled = true;
    }



    private void MlPermissionsCallbacks_OnPermissionDenied(string permission)
    {
        meshingSubsystemComponent.enabled = false;
        Debug.Log("Meshing - Meshing not active");
    }

    private void MlPermissionsCallbacks_OnPermissionGranted(string permission)
    {
        meshingSubsystemComponent.enabled = true;
        Debug.Log("Meshing - Meshing active");

    }

}
