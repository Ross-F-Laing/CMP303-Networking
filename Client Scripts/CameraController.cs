using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public PlayerManager player;
    public float sensitivity = 100f;
    public float clampAngle = 85f;

    private float verticalRotation;
    private float horizontalRotation;

    //====================================================================
    //                              Functions
    //====================================================================

    private void Start()
    {
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = player.transform.eulerAngles.y;
    }

    private void Update()
    {
        // Press escape to toggle cursor locking
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorMode();
        }

        // If cursor is locked to the screen call Look function
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Look();
        }
        Debug.DrawRay(transform.position, transform.forward * 2, Color.red);
    }

    private void Look()
    {
        float _mouseVertical = -Input.GetAxis("Mouse Y");
        float _mouseHorizontal = Input.GetAxis("Mouse X");

        verticalRotation += _mouseVertical * sensitivity * Time.deltaTime;
        horizontalRotation += _mouseHorizontal * sensitivity * Time.deltaTime;

        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        player.transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }

    private void ToggleCursorMode()
    {
        // Make cursor invisible
        Cursor.visible = !Cursor.visible;

        // If cursor not locked to window
        if (Cursor.lockState == CursorLockMode.None)
        {
            // Lock to window
            Cursor.lockState = CursorLockMode.Locked;
        }
        // Otherwise unlock cursor
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
