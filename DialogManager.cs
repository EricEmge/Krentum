using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [SerializeField] GameObject dialogBox;
    [SerializeField] Text dialogText;

    public event Action OnShowDialog;
    public event Action OnHideDialog;

    public static DialogManager Instance { get; private set; }

    private GameController gameController; // Reference to the GameController
    private Dialog dialog;
    private int currentLine = 0;
    private bool isTyping;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Assuming the GameController is attached to the same GameObject or you can find it in another way
        gameController = FindObjectOfType<GameController>();
    }

public void HandleUpdate()
{
    if (Input.GetKeyUp(KeyCode.Z) && !isTyping)
    {
        ++currentLine;
        if (currentLine < dialog.Lines.Count)
        {
            StartCoroutine(TypeDialog(dialog.Lines[currentLine]));
        }
        else
        {
            dialogBox.SetActive(false);
            currentLine = 0;
            OnHideDialog?.Invoke();
        }
    }

    // Check if the player presses V and the game is in Battle state
    if (Input.GetKeyUp(KeyCode.V) && gameController.CurrentState == GameState.Battle)
    {
        ExitBattle();
    }
}


    public IEnumerator ShowDialog(Dialog dialog)
    {
        yield return new WaitForEndOfFrame();
        OnShowDialog?.Invoke();

        this.dialog = dialog;
        dialogBox.SetActive(true);
        StartCoroutine(TypeDialog(dialog.Lines[0]));
    }

    public IEnumerator TypeDialog(string line)
    {
        isTyping = true;
        dialogText.text = "";
        foreach (var letter in line.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(1f / 30);
        }
        isTyping = false;
    }

    // Method to exit the battle mode and return to FreeRoam
    private void ExitBattle()
    {
        // Call a method in the GameController to switch states
        gameController.EndBattle();
    }
}
