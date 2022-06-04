using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls all UI events, including pausing the game and communicating the current player's turn.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The GameManager script.")]
    [SerializeField]
    private GameManager gameManager;
    [Tooltip("The MapManager script.")]
    [SerializeField]
    private MapManager mapManager;
    [Tooltip("The MapUIManager script.")]
    [SerializeField]
    private MapUIManager mapUIManager;
    [Tooltip("The UnitMovement script.")]
    [SerializeField]
    private SelectedUnitManager selectedUnitManager;
    [Tooltip("The AudioManager script.")]
    [SerializeField]
    private AudioManager audioManager;

    [Header("Cameras")]
    [SerializeField]
    private CameraManager mainCamera;

    [Header("Main Menu UI")]
    [Tooltip("The canvas displayed at the beginning of the game.")]
    [SerializeField]
    private Canvas canvasMainMenu;
    [Tooltip("The animator that controls the main menu sliding out of the screen.")]
    [NonSerialized]
    private Animator mainMenuAnim;
    [Tooltip("Checks whether the main menu is active, for the purposes of pausing the game.")]
    [HideInInspector]
    public bool mainMenuActive;

    [Header("Game UI")]
    [Tooltip("The UI elements displayed during gameplay.")]
    [SerializeField]
    private GameObject gameUI;
    [Tooltip("The text showing the current turn number.")]
    [SerializeField]
    private TMP_Text textCurrentDay;
    [Tooltip("The text showing the current player's turn.")]
    [SerializeField]
    private TMP_Text textCurrentTeam;
    [Tooltip("The button a player can press to end their turn.")]
    [SerializeField]
    public Button endTurnButton;
    [Tooltip("The number of seconds that pass before the End Turn button can be pressed, after a turn ends.")]
    [SerializeField]
    private float endTurnResetTime;
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
    [Tooltip("A UI button used to pause the game.")]
    [SerializeField]
    public GameObject pauseButton;
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
    [Tooltip("The animator that controls the game over animation at the end of the game.")]
    [SerializeField]
    private Animator gameOverAnim;

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

    [Header("Unit - Terrain Information")]
    [SerializeField]
    private Image imageTerrain;
    [SerializeField]
    private TMP_Text textTerrainName;
    [SerializeField]
    private GameObject terrainBonus;
    [SerializeField]
    private TMP_Text textTerrainBonus;

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
        // Turn on the main menu.
        canvasMainMenu.enabled = true;
        mainMenuActive = true;

        // Turn off the game UI.
        gameUI.SetActive(false);
        pauseButton.SetActive(false);
        displayingUnitInfo = false;

        // Find the animator components for various UI elements.
        mainMenuAnim = canvasMainMenu.gameObject.GetComponent<Animator>();
        playerTurnAnim = playerTurnTransition.GetComponent<Animator>();

        // Turn off units' health bars while the main menu is active.
        UpdateUIUnitHealthBar();

        // Do not allow the player to pause while the main menu is active.
        gamePaused = false;
        canPause = false;
        TogglePauseButton(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && canPause)
            PauseGame();
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// Sets a bool that lerps the camera from its position on the main menu to its position during gameplay.
    /// </summary>
    public void LerpCamera()
    {
        if (!mainCamera.lerpCam)
        {
            mainCamera.lerpCam = true;

            // Slide the main menu out to the left.
            mainMenuAnim.SetTrigger("Slide Left");
        }
    }

    /// <summary>
    /// Sets up UI for gameplay.
    /// </summary>
    public void StartGame()
    {
        // Turn off the main menu.
        canvasMainMenu.enabled = false;
        mainMenuActive = false;

        // Turn on the game UI and units' health bars.
        gameUI.SetActive(true);
        pauseButton.SetActive(true);
        UpdateUIUnitHealthBar();

        // Notify the current player's turn.
        PrintCurrentTurn();
        PrintCurrentTeam();

        // Disable the End Turn button briefly.
        endTurnButton.interactable = false;
        StartCoroutine(ResetEndTurnButton());

        // Allow the player to pause.
        canPause = true;
        TogglePauseButton(true);
    }

    /// <summary>
    /// Pauses and unpauses the game.
    /// </summary>
    public void PauseGame()
    {
        // The player cannot pause while the main menu is active.
        if (!mainMenuActive)
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
                if (selectedUnitManager.selectedUnit != null)
                {
                    selectedUnitManager.selectedUnit.GetComponent<Unit>().SetAnimIdle();
                    selectedUnitManager.DeselectUnit();
                }

                // Turn off units' health bars.
                UpdateUIUnitHealthBar();
            }
            // Unpause
            else if (gamePaused)
            {
                canvasPause.enabled = false;
                gamePaused = false;

                // Set the icon for the pause button.
                iconPause.enabled = true;
                iconPlay.enabled = false;

                // Turn on units' health bars.
                UpdateUIUnitHealthBar();
            }
        }
    }

    /// <summary>
    /// Toggles the pause UI button's interactability.
    /// </summary>
    /// <param name="state">True if the button is interactable, false if not.</param>
    public void TogglePauseButton(bool state)
    {
        pauseButton.GetComponent<Button>().interactable = state;
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
        // Otherwise if unit info is displaying, and the cursor is casting to a unit that is not the currently highlighted unit...
        else if (gameManager.hit.transform.gameObject.CompareTag("Unit")
            && gameManager.hit.transform.parent.gameObject != mapUIManager.highlightedUnit)
        {
            // Turn off the canvas displaying the previously highlighted unit's stats.
            mapUIManager.highlightedUnit = null;
            canvasUnitInfo.enabled = false;
            displayingUnitInfo = false;
        }
        // Or if unit info is displaying, and the cursor is casting to a tile...
        else if (gameManager.hit.transform.gameObject.CompareTag("Tile"))
        {
            // And that tile is not occupied, turn off the canvas displaying units' stats.
            if (gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile == null)
            {
                mapUIManager.highlightedUnit = null;
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
            // Or if the tile is occupied by a unit different to the currently highlighted unit,
            // Turn off the canvas displaying units' stats.
            else if (gameManager.hit.transform.GetComponent<Tile>().unitOccupyingTile != mapUIManager.highlightedUnit)
            {
                mapUIManager.highlightedUnit = null;
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
        }
        //If the cursor is casting to anything else, turn off the unit info canvas.
        else
        {
            mapUIManager.highlightedUnit = null;
            canvasUnitInfo.enabled = false;
            displayingUnitInfo = false;
        }
    }

    /// <summary>
    /// Sets the colour of units' health bars, depending on the current team, and toggles them on or off.
    /// </summary>
    public void UpdateUIUnitHealthBar()
    {
        for (int i = 0; i < gameManager.numberOfTeams; i++)
        {
            GameObject team = gameManager.GetCurrentTeam(i);

            if (team == gameManager.GetCurrentTeam(0))
            {
                foreach (Transform unit in team.transform)
                {
                    unit.GetComponent<Unit>().healthBar.color = blueTeamColour;
                    unit.GetComponent<Unit>().ToggleHealthBar();
                }
            }
            else if (team == gameManager.GetCurrentTeam(1))
            {
                foreach (Transform unit in team.transform)
                {
                    unit.GetComponent<Unit>().healthBar.color = redTeamColour;
                    unit.GetComponent<Unit>().ToggleHealthBar();
                }
            }
        }
    }

    /// <summary>
    /// Prints the current player's turn to the UI.
    /// </summary>
    public void PrintCurrentTurn()
    {
        // If it's a new day, increase the current day.
        if (gameManager.newDay)
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
        StartCoroutine(audioManager.PlayTeamTurn());

        // Every second time this function is called, it's a new day.
        if (gameManager.newDay)
            gameManager.newDay = false;
        else
            gameManager.newDay = true;
    }

    /// <summary>
    /// Prints the current player's team to the UI.
    /// </summary>
    public void PrintCurrentTeam()
    {
        textCurrentDay.SetText("Day " + gameManager.currentDay);

        if (gameManager.currentTeam == 1)
        {
            textCurrentTeam.SetText("Red Team");
            textCurrentTeam.color = redTeamColour;
        }
        else if (gameManager.currentTeam == 0)
        {
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

        // Display the unit's terrain information.
        imageTerrain.sprite = mapManager.GetTerrainImage(unit.tileX, unit.tileZ);
        textTerrainName.SetText(mapManager.GetTerrainName(unit.tileX, unit.tileZ));
        terrainBonus.SetActive(false);

        // If there is a terrain bonus, display it.
        if (mapManager.CheckTerrainBonus(unit.tileX, unit.tileZ))
        {
            terrainBonus.SetActive(true);
            textTerrainBonus.SetText(mapManager.GetTerrainBonusText(unit.tileX, unit.tileZ));
        }
    }

    /// <summary>
    /// Turns on the endgame UI and passes the winner as a string into the UI.
    /// </summary>
    /// <param name="winner">A string containing the winner.</param>
    public void PrintVictor(GameObject team, string winner)
    {
        gamePaused = true;
        canPause = false;
        TogglePauseButton(false);

        pauseButton.SetActive(false);

        canvasGameOver.enabled = true;
        textGameOver.SetText(winner);

        // Set the winning team's colour.
        if (team == gameManager.team1)
            textGameOver.color = blueTeamColour;
        else if (team == gameManager.team2)
            textGameOver.color = redTeamColour;

        audioManager.PlayVictoryFanfare();
        gameOverAnim.SetTrigger("Victory");
    }

    #endregion


    #region Coroutines

    /// <summary>
    /// Re-enables the End Turn button after a period of time after a turn ends.
    /// </summary>
    /// <returns></returns>
    public IEnumerator ResetEndTurnButton()
    {
        yield return new WaitForSeconds(endTurnResetTime);
        endTurnButton.interactable = true;
    }

    #endregion
}
