using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState{
    FreeRoam, Dialog, Battle, Menu // Added Menu state
}

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] Camera mainCamera;
    [SerializeField] Camera battleCamera;
    [SerializeField] Camera menuCamera; // Menu camera
    [SerializeField] GameObject battleCanvas;
    [SerializeField] GameObject menuCanvas; // Reference to your menu canvas
    [SerializeField] LayerMask battleTileLayer; // Layer for battle tiles

    private GameState state;

    // Public property to expose the current state
    public GameState CurrentState {
        get { return state; }
        private set { state = value; }
    }

    private Vector3 lastCheckedPosition; // Track player's last position

    private void Start()
    {
        DialogManager.Instance.OnShowDialog += () =>
        {
            state = GameState.Dialog;
        };
        DialogManager.Instance.OnHideDialog += () =>
        {
            if (state == GameState.Dialog)
            {
                state = GameState.FreeRoam;
            }
        };

        // Initialize cameras and canvas as disabled
        battleCamera.gameObject.SetActive(false);
        battleCanvas.SetActive(false);
        menuCamera.gameObject.SetActive(false); // Initialize menu camera as disabled
        menuCanvas.SetActive(false);
    }

    private void Update()
    {
        if (state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();
            CheckForBattleTile(); // Continuously check for battle tile

            // Check for menu open with Enter key
            if (Input.GetKeyDown(KeyCode.Return))
            {
                OpenMenu();
            }
        }
        else if (state == GameState.Dialog)
        {
            DialogManager.Instance.HandleUpdate();
        }
        else if (state == GameState.Battle)
        {
            CheckForBattleExit();
        }
        else if (state == GameState.Menu)
        {
            // Handle menu inputs
            if (Input.GetKeyDown(KeyCode.Return))
            {
                CloseMenu();
            }
        }
    }

    // Check for battle tile and trigger battle
    private void CheckForBattleTile()
    {
        Vector3 currentPosition = playerController.transform.position;

        // Only check for battle tile if the player has moved
        if (currentPosition != lastCheckedPosition)
        {
            lastCheckedPosition = currentPosition;

            // Raycast downwards from the player's position
            RaycastHit2D hit = Physics2D.Raycast(currentPosition, Vector2.down, 0.1f, battleTileLayer);

            if (hit.collider != null)
            {
                // Debugging: Check if the player hits a battle tile
                Debug.Log("Player stepped on a battle tile!");

                // Generate a random number between 0 and 1 for 12% battle chance
                float randomChance = Random.Range(0f, 2500f);
                Debug.Log("Random chance: " + randomChance);
                if (randomChance <= 2f)
                {
                    EnterBattle();
                }
        
            }
        }
    }

    public void EnterBattle()
    {
        state = GameState.Battle;
        playerController.enabled = false; // Disable player movement
        battleCamera.gameObject.SetActive(true); // Enable battle camera
        battleCanvas.SetActive(true); // Enable battle canvas

        // Debugging: Log when battle starts
        Debug.Log("Battle started!");
    }

    public void EndBattle()
    {
        state = GameState.FreeRoam;
        playerController.enabled = true; // Enable player movement
        battleCamera.gameObject.SetActive(false); // Disable battle camera
        battleCanvas.SetActive(false); // Disable battle canvas

        // Debugging: Log when battle ends
        Debug.Log("Battle ended, returning to FreeRoam.");
    }

    private void CheckForBattleExit()
    {
        if (Input.GetKeyUp(KeyCode.V))
        {
            Debug.Log("V pressed, exiting battle.");
            EndBattle(); // Exit the battle and return to FreeRoam
        }
    }

    // Open the menu, pause the game, and activate the menu camera
    private void OpenMenu()
    {
        state = GameState.Menu;
        playerController.enabled = false; // Disable player movement
        menuCanvas.SetActive(true); // Enable menu UI
        menuCamera.gameObject.SetActive(true); // Enable the menu camera
        AdjustMenuCamera(); // Adjust menu camera to 20% of the screen

        Debug.Log("Menu opened.");
    }

    // Close the menu, resume the game, and deactivate the menu camera
    private void CloseMenu()
    {
        state = GameState.FreeRoam;
        playerController.enabled = true; // Re-enable player movement
        menuCanvas.SetActive(false); // Disable menu UI
        menuCamera.gameObject.SetActive(false); // Disable the menu camera

        Debug.Log("Menu closed.");
    }

    // Adjust the menu camera to take up 20% of the right screen
    private void AdjustMenuCamera()
    {
        menuCamera.rect = new Rect(0.8f, 0f, 0.2f, 1f); // Right 20% of the screen
    }
}
