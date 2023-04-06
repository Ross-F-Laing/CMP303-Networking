using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //====================================================================
    //                          Global Variables
    //====================================================================

    public static UIManager instance;

    public GameObject startMenu;
    public InputField usernameField;

    //====================================================================
    //                              Functions
    //====================================================================

    // Create 1 single instance of this class
    private void Awake()
    {
        // If there is isn't an instance
        if (instance == null)
        {
            // Set instance to this class
            instance = this;
        }
        // If there is an instance and it's not this class
        else if (instance != this)
        {
            // Debug log message and destroy this class
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    // Connect to the server
    public void ConnectToServer()
    {
        // Disable start menu
        startMenu.SetActive(false);
        usernameField.interactable = false;

        // Call client's connect to server function
        Client.instance.ConnectToServer();
    }
}
