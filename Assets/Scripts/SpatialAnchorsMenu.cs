using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;
using TMPro;



public class SpatialAnchorsMenu : MonoBehaviour
{
    public GameObject spacesMenu;
    public Button openSpaceButton;
    public Button continueButton;
    public GameObject mainMenu;
    public TextMeshProUGUI menuText;



    
    // Start is called before the first frame update
    void Start()
    {

        openSpaceButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);

        continueButton.onClick.AddListener(ContinueToMain);
        openSpaceButton.onClick.AddListener(OpenSpaces);

        //just a test - delete after use
        TextMeshProUGUI infoText = menuText.GetComponent<TextMeshProUGUI>();
        Debug.Log("infoText is" + infoText.text);

        Debug.Log("Starting localization check");
        //Get the user's current Localization Info
        MLResult mlResult = MLAnchors.GetLocalizationInfo(out MLAnchors.LocalizationInfo info);
        //Debug.Log("Spatial info is: " + info);
        //Debug.Log("IsOk = " + mlResult);
        //If we were able to get the localization info, debug it.
        if (mlResult.IsOk)
        {
            openSpaceButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(true);
            infoText.text = "Localization is set, you may continue";

            // Debugs the current LocalizationInfo.
            Debug.Log(info);
        }
        else
        {

            openSpaceButton.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(false);
            //Set error text
            TextMeshProUGUI infoTextMenu = menuText.GetComponent<TextMeshProUGUI>();
            infoText.text =  "Could not find a space. Click the button below to open the spaces app, select or set a space then open the demo project again";
            
            //activate Button

            //Otherwise print the error
            Debug.Log("GetLocalizationInfo Error " + mlResult);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }




    private void ContinueToMain()
    {
       spacesMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    private void OpenSpaces()
    {
        //open the Sapces application on the Magic Leap 2 headset
        UnityEngine.XR.MagicLeap.SettingsIntentsLauncher.LaunchSystemSettings("com.magicleap.intent.action.SELECT_SPACE");
        Application.Quit();
    }

}
