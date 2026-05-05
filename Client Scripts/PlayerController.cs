using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform camTransform;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ClientSend.PlayerShoot(camTransform.forward);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            ClientSend.PlayerThrowItem(camTransform.forward);
        }
    }

    private void FixedUpdate()
    {
        SendInputToServer();
    }

    // Send player inputs to server
    private void SendInputToServer()
    {
        bool[] _inputs = new bool[]
        {
            Input.GetKey(KeyCode.W),        // Forward
            Input.GetKey(KeyCode.S),        // Backward
            Input.GetKey(KeyCode.A),        // Left
            Input.GetKey(KeyCode.D),        // Right
            Input.GetKey(KeyCode.Space)     // Jump
        };

        ClientSend.PlayerMovement(_inputs);
    }
}
