using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SummonSticky : MonoBehaviour
{
    public Button summonButton; // Remember to assign a button to this in the Unity Inspector
    public Button sendBackButton;
    private Vector3 originalPosition;
    private Vector3 headPosition;
    private Quaternion originalRotation;
    private Quaternion headRotation;
    private bool atNewPosition = false;
    public float summonSpeed = 0.5f;
    public float distanceFromEye = 1.0f;

    private void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        summonButton.onClick.AddListener(MoveStickyToNewPosition);
        sendBackButton.onClick.AddListener(MoveStickyToOriginalPosition);
    }

    void OnEnable()
    {
        //PlaceSticky.OnStickyHitEvent += HandleStickyRay;
        //Debug.Log("I am " + this.gameObject.GetInstanceID());
    }

    void OnDisable()
    {
        //PlaceSticky.OnStickyHitEvent -= HandleStickyRay;
    }


    //void HandleStickyRay(int stickyID)
    //{
        
    //    if(this.gameObject.GetInstanceID() == stickyID)
    //    {

    //        if(!atNewPosition)
    //        {
    //            StartCoroutine(LerpStickyToNewPosition());
    //            atNewPosition = true;
    //        }
    //        else
    //        {
    //            StartCoroutine(LerpStickyToOriginalPosition());
    //            atNewPosition = false;  
    //        }

    //    }
    //}

    public void MoveStickyToNewPosition()
    {    
            if (!atNewPosition)
            {
                StartCoroutine(LerpStickyToNewPosition());
                atNewPosition = true;
            }
            else
            {
                StartCoroutine(LerpStickyToOriginalPosition());
                atNewPosition = false;
            }       
    }

    private IEnumerator LerpStickyToNewPosition()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 endPosition = Camera.main.transform.position + Camera.main.transform.forward * distanceFromEye;
        Quaternion endRotation = Camera.main.transform.rotation;

        float elapsedTime = 0f;
        float duration = summonSpeed;

        summonButton.gameObject.SetActive(false);
        sendBackButton.gameObject.SetActive(true);

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsedTime / duration);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;
    }

    private void MoveStickyToOriginalPosition()
    {
        StartCoroutine(LerpStickyToOriginalPosition());
        //this.transform.position = originalPosition;
        //this.transform.rotation = originalRotation;
        //summonButton.gameObject.SetActive(true);
        //sendBackButton.gameObject.SetActive(false);
    }

    private IEnumerator LerpStickyToOriginalPosition()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 endPosition = originalPosition;
        Quaternion endRotation = originalRotation;

        float elapsedTime = 0f;
        float duration = summonSpeed;

        summonButton.gameObject.SetActive(true);
        sendBackButton.gameObject.SetActive(false);

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsedTime / duration);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;
    }


}
