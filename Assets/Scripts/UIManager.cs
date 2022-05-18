using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]

    [Tooltip("The GameManager script.")]
    [SerializeField]
    private GameManager gameManager;
    [Tooltip("The MapUIManager script.")]
    [SerializeField]
    private MapUIManager mapUIManager;

    [Header("UI")]

    [Tooltip("The text showing the current player's turn.")]
    [SerializeField]
    private TMP_Text textCurrentPlayer;
    [Tooltip("The canvas displayed when a game ends.")]
    [SerializeField]
    private Canvas canvasGameOver;

    [Header("Unit Information")]

    [Tooltip("The canvas displayed when a player highlights a unit.")]
    [SerializeField]
    private Canvas canvasUnitInfo;
    [Tooltip("The portrait of a unit displayed in their unit information canvas.")]
    [SerializeField]
    private Image imageUnitPortrait;
    [Tooltip("The name of a unit displayed in their unit information canvas.")]
    [SerializeField]
    private TMP_Text textUnitName;
    [Tooltip("The total health of a unit displayed in their unit information canvas.")]
    [SerializeField]
    private TMP_Text textUnitHealth;
    [Tooltip("The attack damage of a unit displayed in their unit information canvas.")]
    [SerializeField]
    private TMP_Text textUnitAttackDamage;
    [Tooltip("The attacl ramge of a unit displayed in their unit information canvas.")]
    [SerializeField]
    private TMP_Text textUnitAttackRange;
    [Tooltip("The movement speed of a unit displayed in their unit information canvas.")]
    [SerializeField]
    private TMP_Text textUnitMoveSpeed;
    [Tooltip("Checks whether a unit's information is being displayed to the canvas.")]
    [NonSerialized]
    private bool displayingUnitInfo;

    [Header("Player Turn")]

    [Tooltip("The message displayed when a player's turn ends and the other player's turn begins.")]
    [SerializeField]
    private GameObject playerTurnMessage;
    [Tooltip("The animator that controls the player's turn message sliding in and out of the screen.")]
    [NonSerialized]
    private Animator playerTurnAnim;
    [Tooltip("The TextMeshPro that displays the player's turn message at the beginning of their turn.")]
    [NonSerialized]
    private TMP_Text playerTurnText;

    #endregion


    #region Unity Functions

    private void Start()
    {
        //Reset the canvas showing unit information.
        displayingUnitInfo = false;

        //Find the components that display the current player's turn.
        playerTurnAnim = playerTurnMessage.GetComponent<Animator>();
        playerTurnText = playerTurnMessage.GetComponentInChildren<TextMeshProUGUI>();

        //Display the current player's turn.
        PrintCurrentTeam();
        UpdateUITeamHealthBarColour();
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// When the cursor casts to a unit, display its unit information.
    /// </summary>
    public void UpdateUIUnit()
    {
        //If unit information is not currently displaying, and the cursor is casting to a unit...
        if (!displayingUnitInfo)
        {
            if (gameManager.hit.transform.CompareTag("Unit"))
            {
                //Set the unit to highlighted and display its stats.
                mapUIManager.highlightedUnit = gameManager.hit.transform.parent.gameObject;
                Unit unit = gameManager.hit.transform.parent.gameObject.GetComponent<Unit>();

                PrintUnitInfo(unit);
            }
            //Otherwise if the cursor is casting to a tile that is occupied...
            else if (gameManager.hit.transform.CompareTag("Tile")
                && gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile != null)
            {
                //Set that tile's occupied unit to highlighted and display its stats.
                mapUIManager.highlightedUnit = gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile;
                Unit unit = mapUIManager.highlightedUnit.GetComponent<Unit>();

                PrintUnitInfo(unit);
            }
        }
        //Otherwise if the cusor is casting to a unit that is not the currently highlighted unit...
        else if (gameManager.hit.transform.gameObject.CompareTag("Unit")
            && gameManager.hit.transform.parent.gameObject != mapUIManager.highlightedUnit)
        {
            //Turn off the canvas displaying the previously highlighted unit's stats.
            canvasUnitInfo.enabled = false;
            displayingUnitInfo = false;
        }
        //Or if the cursor is casting to a tile...
        else if (gameManager.hit.transform.gameObject.CompareTag("Tile"))
        {
            //And that tile is not occupied, turn off the canvas displaying units' stats.
            if (gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile == null)
            {
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
            //Or if the tile is occupied by a unit different to the currently highlighted unit,
            //Turn off the canvas displaying units' stats.
            else if (gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile != mapUIManager.highlightedUnit)
            {
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
        }
    }

    /// <summary>
    /// Switches the colour of units' health bars between red and blue, depending on the current team.
    /// </summary>
    public void UpdateUITeamHealthBarColour()
    {
        for (int i = 0; i < gameManager.numberOfTeams; i++)
        {
            GameObject team = gameManager.GetCurrentTeam(i);

            if (team == gameManager.GetCurrentTeam(gameManager.currentTeam))
            {
                foreach (Transform unit in team.transform)
                    unit.GetComponent<Unit>().healthBar.color = Color.blue;
            }
            else
            {
                foreach (Transform unit in team.transform)
                    unit.GetComponent<Unit>().healthBar.color = Color.red;
            }
        }
    }

    /// <summary>
    /// Prints the current player's turn to the UI.
    /// </summary>
    public void PrintCurrentTurn()
    {
        //Animate the message communicating this change.
        if (gameManager.currentTeam == 1)
        {
            playerTurnAnim.SetTrigger("Slide Left");
            playerTurnText.SetText("Player Two's Turn");
        }
        else if (gameManager.currentTeam == 0)
        {
            playerTurnAnim.SetTrigger("Slide Right");
            playerTurnText.SetText("Player One's Turn");
        }
    }

    /// <summary>
    /// Prints the current player's team to the UI.
    /// </summary>
    public void PrintCurrentTeam()
    {
        textCurrentPlayer.SetText("Current Player's Turn: Player " + (gameManager.currentTeam + 1).ToString());
    }

    /// <summary>
    /// Print's a highlighted unit's stats to its own canvas.
    /// </summary>
    /// <param name="unit">The unit that the cursor is currently highlighting.</param>
    public void PrintUnitInfo(Unit unit)
    {
        //Turn on the canvas that displays units' stats.
        canvasUnitInfo.enabled = true;
        displayingUnitInfo = true;

        //Pass the units' stats into the canvas' UI elements.
        imageUnitPortrait.sprite = unit.portrait;
        textUnitName.SetText(unit.unitName.ToString());
        textUnitHealth.SetText("HP: " + unit.currentHealth.ToString());
        textUnitAttackDamage.SetText("Atk: " + unit.attackDamage.ToString());
        textUnitAttackRange.SetText("Range: " + unit.attackRange.ToString());
        textUnitMoveSpeed.SetText("Mov: " + unit.moveSpeed.ToString());
    }

    /// <summary>
    /// Turns on the endgame UI and passes the winner as a string into the UI.
    /// </summary>
    /// <param name="winner">A string containing the winner.</param>
    public void PrintVictor(string winner)
    {
        canvasGameOver.enabled = true;
        canvasGameOver.GetComponentInChildren<TextMeshProUGUI>().SetText(winner);
    }

    #endregion
}
