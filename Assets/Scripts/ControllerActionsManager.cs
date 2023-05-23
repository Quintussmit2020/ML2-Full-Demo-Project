using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;

public class ControllerActionsManager : MonoBehaviour
{
    // inputs
    private MagicLeapInputs magicLeapInputs;
    public MagicLeapInputs.ControllerActions controllerActions;

    private void Start()
    {
        magicLeapInputs = new MagicLeapInputs();
        magicLeapInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(magicLeapInputs);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
