using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the game's teams, including ending a turn and switching the current team to the other player.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The MapManager script.")]
    [SerializeField]
    private MapManager mapManager;
    [Tooltip("The UIManager script.")]
    [SerializeField]
    private UIManager uIManager;
    [Tooltip("The UIManager script.")]
    [SerializeField]
    private MapUIManager mapUIManager;
    [Tooltip("The UnitMovement script.")]
    [SerializeField]
    private SelectedUnitManager selectedUnitManager;

    [Header("Teams")]
    [Tooltip("Player one's team.")]
    [SerializeField]
    public GameObject team1;
    [Tooltip("Player two's team.")]
    [SerializeField]
    public GameObject team2;
    [Tooltip("The number of teams participating in a game.")]
    [HideInInspector]
    public int numberOfTeams = 2;
    [Tooltip("The current player who's turn it is to move units and attack.")]
    [HideInInspector]
    public int currentTeam;
    [Tooltip("The current turn number.")]
    [HideInInspector]
    public int currentDay;

    [Header("Map")]
    [HideInInspector]
    public Ray ray;
    [HideInInspector]
    public RaycastHit hit;

    #endregion


    #region Unity Functions

    private void Start()
    {
        // Reset the current team and day.
        currentTeam = 0;
        currentDay = 0;
    }

    private void Update()
    {
        // Keep track of the cursor's position.
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out hit))
        {
            if (uIManager.gamePaused == true)
                return;

            // Check if the cursor is highlighting a tile and/or a unit.
            mapUIManager.CalculateHighlightTile();
            uIManager.UpdateUIUnit();
            // If a unit is selected, calculate its path to the tile highlighted by the cursor.
            mapUIManager.CalculateUnitPath();
        }
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// Controls the button that the player clicks to confirm the end of their turn.
    /// </summary>
    public void EndTurn()
    {
        // If there is no currently selected unit...
        if (selectedUnitManager.selectedUnit == null)
        {
            // Switch to the other player's team.
            SwitchCurrentTeam();

            uIManager.PrintCurrentTurn();
            //UpdateUITeamHealthBarColour();
            uIManager.PrintCurrentTeam();
        }
    }

    /// <summary>
    /// Returns a game object that contains all of the current team's units.
    /// </summary>
    /// <param name="teamNumber">The index of the current team.</param>
    /// <returns></returns>
    public GameObject GetCurrentTeam(int teamNumber)
    {
        GameObject team = null;

        if (teamNumber == 0)
            team = team1;
        else if (teamNumber == 1)
            team = team2;

        return team;
    }

    /// <summary>
    /// Returns a game object that contains all of the enemy team's units.
    /// </summary>
    /// <param name="teamNumber">The index of the current team.</param>
    /// <returns></returns>
    public GameObject GetEnemyTeam(int teamNumber)
    {
        GameObject team = null;

        if (teamNumber == 0)
            team = team2;
        else if (teamNumber == 1)
            team = team1;

        return team;
    }

    /// <summary>
    /// Increments the current team number when a player ends their turn.
    /// </summary>
    private void SwitchCurrentTeam()
    {
        ResetTeam(GetCurrentTeam(currentTeam));
        currentTeam++;

        // If the current team exceeds the number of teams, revert back to zero.
        if (currentTeam == numberOfTeams)
            currentTeam = 0;
    }

    /// <summary>
    /// Re-enables movement for all of the unit's on a given team.
    /// </summary>
    /// <param name="team">The current team whose units are being reset to move again.</param>
    private void ResetTeam(GameObject team)
    {
        // For each unit in a team...
        foreach (Transform unit in team.transform)
        {
            // Reset the unit's movement path and set its state to unselected.
            unit.GetComponent<Unit>().path = null;
            unit.GetComponent<Unit>().movementState = MovementState.Unselected;
            unit.GetComponent<Unit>().moveCompleted = false;

            // Reset the unit's material to its default material.
            unit.gameObject.GetComponentInChildren<Renderer>().material = unit.GetComponent<Unit>().unitMat;

            // Set the unit's animation to idle.
            unit.GetComponent<Unit>().SetAnimIdle();
        }
    }

    #endregion


    #region Coroutines

    /// <summary>
    /// Waits for all of the IEnumerators to finish before ending the game.
    /// </summary>
    /// <param name="attacker">The unit that initiated the final attack.</param>
    /// <param name="defender">The unit that defended against the final attack.</param>
    /// <returns></returns>
    public IEnumerator CheckVictor(GameObject attacker, GameObject defender)
    {
        // Wait for the units to stop attacking.
        while (attacker.GetComponent<Unit>().combatQueue.Count != 0)
            yield return new WaitForEndOfFrame();
        while (defender.GetComponent<Unit>().combatQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        if (team1.transform.childCount == 0)
            uIManager.PrintVictor(team2, "Red Team");
        else if (team2.transform.childCount == 0)
            uIManager.PrintVictor(team1, "Blue Team");
    }

    #endregion
}