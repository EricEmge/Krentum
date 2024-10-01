using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public Text[] menuOptions; // Array of menu options (Text UI components)
    public Color defaultColor = Color.black;
    public Color highlightColor = Color.green;

    private int currentIndex = 0; // Index of the currently selected option
    private bool menuActive = true; // Controls whether the menu is active or not

    void Start()
    {
        HighlightOption();
    }

    void Update()
    {
        if (menuActive)
        {
            HandleInput();
        }
    }

    // Handles the input for navigating the menu and selecting options
    void HandleInput()
    {
        // Navigate down
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % menuOptions.Length; // Loop back to the top
            HighlightOption();
        }

        // Navigate up
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = menuOptions.Length - 1; // Loop back to the bottom
            HighlightOption();
        }

        // Select an option with Z key
        if (Input.GetKeyDown(KeyCode.Z))
        {
            SelectOption();
        }

        // Exit the menu with X or Enter
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Return))
        {
            CloseMenu();
        }
    }

    // Highlights the currently selected option
    void HighlightOption()
    {
        // Reset all options to default color
        for (int i = 0; i < menuOptions.Length; i++)
        {
            menuOptions[i].color = defaultColor;
        }

        // Highlight the selected option
        menuOptions[currentIndex].color = highlightColor;
    }

    // Handles the selection of the current option
    void SelectOption()
    {
        string selectedOption = menuOptions[currentIndex].text;

        // Perform actions based on the selected option
        switch (selectedOption)
        {
            case "Recondek":
                Debug.Log("Recondek selected");
                // Add code to handle Recondek logic
                break;
            case "Krentums":
                Debug.Log("Krentums selected");
                // Add code to handle Krentums logic
                break;
            case "Bag":
                Debug.Log("Bag selected");
                // Add code to handle Bag logic
                break;
            case "ID":
                Debug.Log("ID selected");
                // Add code to handle ID logic
                break;
            case "Save":
                Debug.Log("Save selected");
                // Add code to handle Save logic
                break;
            case "Options":
                Debug.Log("Options selected");
                // Add code to handle Options logic
                break;
            case "Stats":
                Debug.Log("Stats selected");
                // Add code to handle Stats logic
                break;
            case "Quit":
                Debug.Log("Quit selected");
                // Add code to handle Quit logic
                Application.Quit(); // Quit the game
                break;
        }
    }

    // Closes the menu and returns to gameplay or the previous state
    void CloseMenu()
    {
        menuActive = false;
        Debug.Log("Menu closed");
        // Add logic to close the menu, disable canvas, etc.
        gameObject.SetActive(false); // Deactivate the menu GameObject
    }
}

