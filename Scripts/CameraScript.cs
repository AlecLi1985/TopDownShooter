using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform cameraPivot;
    public Transform targetLookAt;
    public bool isEnabled;
    public float cameraDistance = 10f;
    public float cameraRotateSpeed = 100f;
    public float cameraLerpSpeed = 50f;
    public bool invertX = false;
    public bool invertY = false;
    public float minYClamp;
    public float maxYClamp;

    Vector3 lookDirection;
    Quaternion lookAtRotation;
    Quaternion fromRotation;
    Quaternion toRotation;
    Vector3 toRotationClamped;
    public float xRotation;
    public float yRotation;

    //Vector3 mousePosition = Vector3.zero;

    private void Start()
    {
        isEnabled = true;
        xRotation = yRotation = 0f;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(isEnabled)
        {
            //if(Input.GetMouseButtonDown(1))
            //{
            //    MainGame.instance.SetShipComponentsIgnoreRaycastLayer();
            //}
            //if (Input.GetMouseButtonUp(1))
            //{
            //    MainGame.instance.SetShipComponentsDefaultLayer();
            //}

            if (Input.GetMouseButton(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if(!invertX)
                    xRotation += Input.GetAxis("Mouse X") * (cameraRotateSpeed * Time.deltaTime);
                else
                    xRotation -= Input.GetAxis("Mouse X") * (cameraRotateSpeed * Time.deltaTime);

                if (xRotation > 360f)
                    xRotation -= 360f;
                else if (xRotation < 0f)
                    xRotation += 360f;

                if(!invertY)
                    yRotation += Input.GetAxis("Mouse Y") * (cameraRotateSpeed * Time.deltaTime);
                else
                    yRotation -= Input.GetAxis("Mouse Y") * (cameraRotateSpeed * Time.deltaTime);

                yRotation = Mathf.Clamp(yRotation, minYClamp, maxYClamp);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            lookDirection = targetLookAt.position - transform.position;
            lookAtRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

            toRotation = Quaternion.Euler(yRotation, -xRotation, 0f);

            transform.rotation = lookAtRotation;
            transform.position = Vector3.Lerp(transform.position, targetLookAt.position + (-transform.forward * cameraDistance), cameraLerpSpeed * Time.deltaTime);

            cameraPivot.transform.rotation = Quaternion.Lerp(cameraPivot.transform.rotation, toRotation, cameraLerpSpeed * Time.deltaTime);
        }
    }
}
