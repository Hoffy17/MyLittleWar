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
    [Tooltip("The UnitMovement script.")]
    [SerializeField]
    private UnitMovement unitMovement;
    [Tooltip("The AudioManager script.")]
    [SerializeField]
    private AudioManager audioManager;

    [Header("Game UI")]
    [Tooltip("The text showing the current turn number.")]
    [SerializeField]
    private TMP_Text textCurrentDay;
    [Tooltip("The text showing the current player's turn.")]
    [SerializeField]
    private TMP_Text textCurrentTeam;
    [Tooltip("The colour of the Blue Team's name.")]
    [SerializeField]
    private Color blueTeamColour;
    [Tooltip("The colour of the Red Team's name.")]
    [SerializeField]
    private Color redTeamColour;

    [Header("Pause UI")]
    [Tooltip("The canvas displayed when the game is paused.")]
    [SerializeField]
    private Canvas canvasPause;
    [Tooltip("The icon displayed on the pause button while the game is playing.")]
    [SerializeField]
    private Image iconPause;
    [Tooltip("The icon displayed on the pause button while the game is paused.")]
    [SerializeField]
    private Image iconPlay;
    [Tooltip("Checks whether the game is paused.")]
    [HideInInspector]
    public bool gamePaused;
    [Tooltip("Checks whether the player is allowed to pause and unpause the game.")]
    [HideInInspector]
    public bool canPause;

    [Header("Game Over UI")]
    [Tooltip("The canvas displayed when a game ends.")]
    [SerializeField]
    private Canvas canvasGameOver;
    [Tooltip("The text showing the winning team.")]
    [SerializeField]
    private TMP_Text textGameOver;

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

    [Header("Player Turn Transition")]
    [Tooltip("The message displayed when a player's turn ends and the other player's turn begins.")]
    [SerializeField]
    private GameObject playerTurnTransition;
    [Tooltip("The animator that controls the player's turn message sliding in and out of the screen.")]
    [NonSerialized]
    private Animator playerTurnAnim;
    [Tooltip("The TextMeshPro that displays the current turn number at the beginning of a player's turn.")]
    [SerializeField]
    private TMP_Text playerTurnDayText;
    [Tooltip("The TextMeshPro that displays the player's team at the beginning of their turn.")]
    [SerializeField]
    private TMP_Text playerTurnTeamText;

    #endregion


    #region Unity Functions

    private void Start()
    {
        // Reset the canvas showing unit information.
        displayingUnitInfo = false;

        // Find the components that display the current player's turn.
        playerTurnAnim = playerTurnTransition.GetComponent<Animator>();

        // Display the current player's turn.
        PrintCurrentTurn();
        PrintCurrentTeam();
        UpdateUITeamHealthBarColour();

        gamePaused = false;
        canPause = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && canPause)
            PauseGame();
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// Pauses and unpauses the game.
    /// </summary>
    public void PauseGame()
    {
        // Pause
        if (!gamePaused)
        {
            canvasPause.enabled = true;
            gamePaused = true;

            // Set the icon for the pause button.
            iconPause.enabled = false;
            iconPlay.enabled = true;

            // If a unit was selected, set its animation to idle and deselect it.
            if (unitMovement.selectedUnit != null)
            {
                unitMovement.selectedUnit.GetComponent<Unit>().SetAnimIdle();
                unitMovement.DeselectUnit();
            }
        }
        // Unpause
        else if (gamePaused)
        {
            canvasPause.enabled = false;
            gamePaused = false;

            // Set the icon for the pause button.
            iconPause.enabled = true;
            iconPlay.enabled = false;
        }
    }

    /// <summary>
    /// When the cursor casts to a unit, display its unit information.
    /// </summary>
    public void UpdateUIUnit()
    {
        // If unit information is not currently displaying, and the cursor is casting to a unit...
        if (!displayingUnitInfo)
        {
            if (gameManager.hit.transform.CompareTag("Unit"))
            {
                // Set the unit to highlighted and display its stats.
                mapUIManager.highlightedUnit = gameManager.hit.transform.parent.gameObject;
                Unit unit = gameManager.hit.transform.parent.gameObject.GetComponent<Unit>();

                PrintUnitInfo(unit);
            }
            // Otherwise if the cursor is casting to a tile that is occupied...
            else if (gameManager.hit.transform.CompareTag("Tile")
                && gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile != null)
            {
                // Set that tile's occupied unit to highlighted and display its stats.
                mapUIManager.highlightedUnit = gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile;
                Unit unit = mapUIManager.highlightedUnit.GetComponent<Unit>();

                PrintUnitInfo(unit);
            }
        }
        // Otherwise if the cusor is casting to a unit that is not the currently highlighted unit...
        else if (gameManager.hit.transform.gameObject.CompareTag("Unit")
            && gameManager.hit.transform.parent.gameObject != mapUIManager.highlightedUnit)
        {
            // Turn off the canvas displaying the previously highlighted unit's stats.
            canvasUnitInfo.enabled = false;
            displayingUnitInfo = false;
        }
        // Or if the cursor is casting to a tile...
        else if (gameManager.hit.transform.gameObject.CompareTag("Tile"))
        {
            // And that tile is not occupied, turn off the canvas displaying units' stats.
            if (gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile == null)
            {
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
            // Or if the tile is occupied by a unit different to the currently highlighted unit,
            // Turn off the canvas displaying units' stats.
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
                    unit.GetComponent<Unit>().healthBar.color = blueTeamColour;
            }
            else
            {
                foreach (Transform unit in team.transform)
                    unit.GetComponent<Unit>().healthBar.color = redTeamColour;
            }
        }
    }

    /// <summary>
    /// Prints the current player's turn to the UI.
    /// </summary>
    public void PrintCurrentTurn()
    {
        gameManager.currentDay++;
        playerTurnDayText.SetText("Day " + gameManager.currentDay);

        // Animate the message communicating the current player's turn.
        if (gameManager.currentTeam == 1)
        {
            playerTurnAnim.SetTrigger("Slide Left");
            playerTurnTeamText.SetText("Red Team");
            playerTurnTeamText.color = redTeamColour;
        }
        else if (gameManager.currentTeam == 0)
        {
            playerTurnAnim.SetTrigger("Slide Right");
            playerTurnTeamText.SetText("Blue Team");
            playerTurnTeamText.color = blueTeamColour;
        }

        audioManager.PlayTeamFanfare();
    }

    /// <summary>
    /// Prints the current player's team to the UI.
    /// </summary>
    public void PrintCurrentTeam()
    {
        if (gameManager.currentTeam == 1)
        {
            textCurrentDay.SetText("Day " + gameManager.currentDay);
            textCurrentTeam.SetText("Red Team");
            textCurrentTeam.color = redTeamColour;
        }
        else if (gameManager.currentTeam == 0)
        {
            textCurrentDay.SetText("Day " + gameManager.currentDay);
            textCurrentTeam.SetText("Blue Team");
            textCurrentTeam.color = blueTeamColour;
        }
    }

    /// <summary>
    /// Print's a highlighted unit's stats to its own canvas.
    /// </summary>
    /// <param name="unit">The unit that the cursor is currently highlighting.</param>
    public void PrintUnitInfo(Unit unit)
    {
        // Turn on the canvas that displays units' stats.
        canvasUnitInfo.enabled = true;
        displayingUnitInfo = true;

        // Pass the units' stats into the canvas' UI elements.
        imageUnitPortrait.sprite = unit.portrait;
        textUnitName.SetText(unit.unitName.ToString());
        textUnitHealth.SetText(unit.currentHealth.ToString());
        textUnitAttackDamage.SetText(unit.attackDamage.ToString());
        textUnitAttackRange.SetText(unit.attackRange.ToString());
        textUnitMoveSpeed.SetText(unit.moveSpeed.ToString());
    }

    /// <summary>
    /// Turns on the endgame UI and passes the winner as a string into the UI.
    /// </summary>
    /// <param name="winner">A string containing the winner.</param>
    public void PrintVictor(GameObject team, string winner)
    {
        gamePaused = true;
        canPause = false;

        canvasGameOver.enabled = true;
        textGameOver.SetText(winner);

        // Set the winning team's colour.
        if (team == gameManager.team1)
            textGameOver.color = blueTeamColour;
        else if (team == gameManager.team2)
            textGameOver.color = redTeamColour;
    }

    #endregion
}
