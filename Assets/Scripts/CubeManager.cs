using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using static MagicLeapInputs;

public class CubeManager : MonoBehaviour
{
    private MagicLeapInputs mlInputs;
    private MagicLeapInputs.ControllerActions controllerActions;

    public float distanceFromController;
    public GameObject spawnLocation;
    private GameObject objectToSpawn;
    public GameObject[] spawnCubes;

    public MenuController menuController;

    private bool cubeFeature = false;


    void Start()
    {     
      
        // set up controller inputs
        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

        // subscribe to bumper event
        controllerActions.Bumper.performed += HandleOnBumper;
        // subscribe to the button event
        menuController.onCubebButtonPress += StartTheFun;

    }

    private void StartTheFun(bool activeState)
    {
        Debug.Log("Cube madness unleashed!!!");
       cubeFeature = activeState;
    }


    private void HandleOnBumper(InputAction.CallbackContext obj)
    {

        if (cubeFeature)
        {
            // Select a random GameObject from the array
            Debug.Log("You should be seeing cubes");
            int index = Random.Range(0, spawnCubes.Length);

            if (spawnCubes.Length >= 1)
            {
                // Calculate the spawn position 1 meter in front of the spawnLocation
                Vector3 spawnPosition = spawnLocation.transform.position + spawnLocation.transform.forward * distanceFromController;

                GameObject randomObject = spawnCubes[index];
                GameObject spawnedObject = Instantiate(randomObject, spawnPosition, spawnLocation.transform.rotation);
            }
        }
    }
}
