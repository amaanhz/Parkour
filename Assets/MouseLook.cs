using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField]
    private float sensitivity = 5.0f;
    [SerializeField]
    private float smoothing = 2.0f;
    private float rotationSmooth = 6.0f;
    private GameObject character;
    [SerializeField]
    private RigidPlayerMovement rigidPlayer;
    private Vector2 mouseLook;
    private Vector2 smoothV;
    [SerializeField]
    private float wallRunAngle = 5.0f;
    private float angleInc = 1f;
    private Quaternion initialRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        character = this.transform.parent.gameObject;
    }

    private void Update()
    {
        // md is mouse delta
        var md = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        md = Vector2.Scale(md, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        // the interpolated float result between the two float values
        smoothV.x = Mathf.Lerp(smoothV.x, md.x, 1f / smoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, md.y, 1f / smoothing);
        // incrementally add to the camera look
        mouseLook += smoothV;
        if(mouseLook.y > 90)
        {
            mouseLook.y = 90;
        }
        if(mouseLook.y < -90)
        {
            mouseLook.y = -90;
        }
        // vector3.right means the x-axis
        if(mouseLook.y < 90 && mouseLook.y > -90)
        {
            transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);
        }
        character.transform.localRotation = Quaternion.AngleAxis(mouseLook.x, character.transform.up);
        // WallRunCam();
    }

    private void WallRunCam()
    {
        if (rigidPlayer.isWallRunning)
        {
            switch(rigidPlayer.TouchingWall(2.0f, false))
            {
                case RigidPlayerMovement.checkDir.Left:
                    //transform.localRotation = Quaternion.AngleAxis(-10f, Vector3.forward);
                    //Quaternion.Slerp(transform.localRotation, initialRotation * Quaternion.Euler(new Vector3(0f, 0f, -50f)), Time.deltaTime * rotationSmooth);
                    transform.Rotate(new Vector3(0f, 0f, -10f) * Time.deltaTime, Space.Self);
                    Debug.Log("Rotating");
                    break;
                case RigidPlayerMovement.checkDir.Right:
                    //transform.localRotation = Quaternion.AngleAxis(10f, Vector3.forward);
                    Debug.Log("Rotating");
                    break;
            }
        }
        else
        {
            initialRotation = transform.localRotation;
        }
    }
}
// Quaternion.Slerp(transform.localRotation, initialRotation * Quaternion.Euler(new Vector3(0f, 0f, -50f)), Time.deltaTime * rotationSmooth);