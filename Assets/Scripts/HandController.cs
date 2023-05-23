using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandController : MonoBehaviour


{
    // Start is called before the first frame update
    public GameObject handControllerObject;
    [Tooltip("Drag the Menu Canvas object here")]
    public MenuController menuController;
 
  

    void Start()
    {
        menuController.onHandButtonPress += Openhands;
    }

    private void Openhands(bool handState)
    {
        handControllerObject.SetActive(handState);
        Debug.Log("Hand state is " + handState);
    }

}
