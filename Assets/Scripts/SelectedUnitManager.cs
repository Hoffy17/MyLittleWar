using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages any single selected unit, including calculating its movement and attack ranges, attacking and waiting after moving.
/// </summary>
public class SelectedUnitManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The GameManager script.")]
    [SerializeField]
    private GameManager gameManager;
    [Tooltip("The BattleManager script.")]
    [SerializeField]
    private BattleManager battleManager;
    [Tooltip("The MapManager script.")]
    [SerializeField]
    private MapManager mapManager;
    [Tooltip("The UIManager script.")]
    [SerializeField]
    private UIManager uIManager;
    [Tooltip("The MapUIManager script.")]
    [SerializeField]
    private MapUIManager mapUIManager;
    [Tooltip("The AudioManager script.")]
    [SerializeField]
    private AudioManager audioManager;

    [Header("Selected Unit")]
    [Tooltip("The current unit that has been clicked on.")]
    [HideInInspector]
    public GameObject selectedUnit;
    [Tooltip("A container of nodes representing a selected unit's movement range, based on the unit's move speed and any attackable enemies within this range.")]
    [HideInInspector]
    public HashSet<Node> selectedUnitMoveRange;
    [Tooltip("Checks whether or not the player has selected a unit.")]
    [NonSerialized]
    private bool unitSelected;
    [Tooltip("The selected unit's previous map grid position on the X axis.")]
    [NonSerialized]
    private int unitSelectedPrevX;
    [Tooltip("The selected unit's previous map grid position on the Z axis.")]
    [NonSerialized]
    private int unitSelectedPrevZ;
    [Tooltip("The previous tile that the selected unit was occupying.")]
    [NonSerialized]
    private GameObject unitSelectedPrevTile;

    #endregion


    #region Movement & Attack Range

    /// <summary>
    /// Calculates the map grid nodes that need to be highlighted when a unit's movement range is displayed.
    /// </summary>
    private void MovementRange()
    {
        // A container of nodes representing the tiles that the selected unit can move to.
        HashSet<Node> movementNodesInRange = new HashSet<Node>();
        // A container of nodes representing the tiles that the selected unit can attack.
        HashSet<Node> attackNodesInRange = new HashSet<Node>();

        // Store the selected unit's start position on the map grid in a local variable.
        Node startNode = mapManager.graph[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ];

        // Store the selected unit's attack range and move speed in local variables.
        int attackRange = selectedUnit.GetComponent<Unit>().attackRange;
        int movespeed = selectedUnit.GetComponent<Unit>().moveSpeed;

        // Calculate the nodes that exist in the selected unit's movement and attack ranges.
        movementNodesInRange = GetMovementRange(movementNodesInRange, movespeed, startNode);
        attackNodesInRange = GetAttackRange(movementNodesInRange, attackNodesInRange, attackRange, startNode);

        // Finally, highlight the selected unit's movement range and attackable enemies.
        mapUIManager.HighlightAttackRange(attackNodesInRange);
        mapUIManager.HighlightMovementRange(movementNodesInRange);

        selectedUnitMoveRange = movementNodesInRange;
    }

    /// <summary>
    /// Returns a container of nodes that the selected unit could potentially move to from its current position.
    /// </summary>
    /// <param name="movementNodesInRange">A container of nodes representing the tiles that the selected unit can move to.</param>
    /// <param name="movespeed">The selected unit's movement range.</param>
    /// <param name="startNode">The selected unit's start position on the map grid.</param>
    /// <returns></returns>
    private HashSet<Node> GetMovementRange(HashSet<Node> movementNodesInRange, int movespeed, Node startNode)
    {
        // Create a 2D array containing the costs for units to enter all of the tiles on the map grid.
        float[,] cost = new float[mapManager.mapSizeX, mapManager.mapSizeZ];

        // A container, and temporary container, of nodes that are highlighted in a selected unit's movement range.
        HashSet<Node> uIHighlight = new HashSet<Node>();
        HashSet<Node> tempUIHighlight = new HashSet<Node>();

        // Add the selected unit's start node into the container of nodes that the unit can move to.
        movementNodesInRange.Add(startNode);

        // For each of the start node's neighbours...
        foreach (Node node in startNode.neighbours)
        {
            // Add their costs to the local 2D array.
            cost[node.x, node.z] = mapManager.CostToEnterTile(node.x, node.z);

            // If the unit can spend enough move speed to enter the neighbouring node...
            if (movespeed - cost[node.x, node.z] >= 0)
                // Add those neighbouring nodes to the container of nodes to be highlighted.
                uIHighlight.Add(node);
        }

        // Insert those highlighted nodes into the unit's movement range.
        movementNodesInRange.UnionWith(uIHighlight);

        while (uIHighlight.Count != 0)
        {
            // For all of the nodes neighbouring the nodes that have been highlighted...
            foreach (Node node in uIHighlight)
                foreach (Node neighbour in node.neighbours)
                    // If those neighbours have not already been added to the unit's movement range...
                    if (!movementNodesInRange.Contains(neighbour))
                    {
                        // Calculate the cost to move from those nodes to their neighbouring nodes.
                        cost[neighbour.x, neighbour.z] = mapManager.CostToEnterTile(neighbour.x, neighbour.z) + cost[node.x, node.z];

                        // If the unit can spend enough move speed to enter the neighbouring node...
                        if (movespeed - cost[neighbour.x, neighbour.z] >= 0)
                            // Add those neighbouring nodes to the container of nodes to be highlighted.
                            tempUIHighlight.Add(neighbour);
                    }

            // Store the hightlighted nodes in the selected unit's movement range.
            uIHighlight = tempUIHighlight;
            movementNodesInRange.UnionWith(uIHighlight);
            // Refresh the temporary container of highlighted nodes.
            tempUIHighlight = new HashSet<Node>();
        }

        return movementNodesInRange;
    }

    /// <summary>
    /// Returns a container of nodes that represent the tiles that the selected unit could potentially attack, displayed before the unit moves.
    /// </summary>
    /// <param name="movementNodesInRange">A container of nodes representing the tiles that the selected unit can move to.</param>
    /// <param name="attackNodesInRange">A container of nodes representing the tiles that the selected unit can attack.</param>
    /// <param name="attackRange">The selected unit's attack range.</param>
    /// <param name="startNode">The selected unit's start position on the map grid.</param>
    /// <returns></returns>
    private HashSet<Node> GetAttackRange(HashSet<Node> movementNodesInRange, HashSet<Node> attackNodesInRange, int attackRange, Node startNode)
    {
        // A container, and temporary container, of nodes that neighbour other nodes.
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash;
        // A temporary container of nodes that represent the tiles that the selected unit can attack.
        HashSet<Node> tempAttackNodesInRange = new HashSet<Node>();

        // For all of the nodes in the selected unit's movement range...
        foreach (Node node in movementNodesInRange)
        {
            // Add those nodes into the container of neighbouring nodes.
            neighbourHash = new HashSet<Node>();
            neighbourHash.Add(node);

            // For all of the neighbouring nodes in the selected unit's attack range...
            for (int i = 0; i < attackRange; i++)
            {
                foreach (Node neighbourNode in neighbourHash)
                    foreach (Node tempNeighbourNode in neighbourNode.neighbours)
                        tempNeighbourHash.Add(tempNeighbourNode);

                // Store those neighbouring nodes.
                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();

                // Continue to build a temporary container of attackable nodes,
                // Until the for loop is the same as the selected unit's attack range. 
                if (i < attackRange - 1)
                    tempAttackNodesInRange.UnionWith(neighbourHash);
            }

            // Remove the nodes in the selected unit's attack range from the container of neighbouring nodes.
            neighbourHash.ExceptWith(tempAttackNodesInRange);
            tempAttackNodesInRange = new HashSet<Node>();
            // Add the remaining neighbouring nodes into the hash of attackable nodes within the selected unit's range.
            attackNodesInRange.UnionWith(neighbourHash);
        }

        // Remove the selected unit's start node from the container of attackable nodes.
        attackNodesInRange.Remove(startNode);

        return attackNodesInRange;
    }

    /// <summary>
    /// Returns a container of nodes that represent the tiles that the selected unit can attack, displayed after the unit moves.
    /// </summary>
    /// <returns></returns>
    private HashSet<Node> GetAttackRangeAfterMoving()
    {
        // A container, and temporary container, of nodes that neighbour other nodes.
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        // A container of nodes that have been checked for being within the unit's attack range.
        HashSet<Node> checkedNodes = new HashSet<Node>();

        // Store the selected unit's start position on the map grid in a local variable.
        Node startNode = mapManager.graph[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ];
        // Store the selected unit's attack range in a local variable.
        int attackRange = selectedUnit.GetComponent<Unit>().attackRange;

        // Add the selected unit's start node into the container of nodes that need to be checked.
        neighbourHash.Add(startNode);

        // For all of the neighbouring nodes in the selected unit's attack range...
        for (int i = 0; i < attackRange; i++)
        {
            foreach (Node neighbourNode in neighbourHash)
                foreach (Node tempNeighbourNode in neighbourNode.neighbours)
                    tempNeighbourHash.Add(tempNeighbourNode);

            // Store those neighbouring nodes.
            neighbourHash = tempNeighbourHash;
            tempNeighbourHash = new HashSet<Node>();

            // Continue to build a container of nodes neighbouring other nodes,
            // Until the for loop is the same as the selected unit's attack range. 
            if (i < attackRange - 1)
                checkedNodes.UnionWith(neighbourHash);
        }
        // Remove the checked nodes in the selected unit's range from the container of neighbouring nodes.
        neighbourHash.ExceptWith(checkedNodes);
        // Remove the selected unit's start node from the container of neighbouring nodes.
        neighbourHash.Remove(startNode);
        return neighbourHash;
    }

    /// <summary>
    /// Returns the selected unit's currently occupied tile as a hashset.
    /// </summary>
    /// <returns></returns>
    private HashSet<Node> GetOccupiedTile()
    {
        HashSet<Node> occupiedTile = new HashSet<Node>();
        // Add the selected unit's X and Z positions on the map grid to a hashset and return it.
        occupiedTile.Add(mapManager.graph[
            selectedUnit.GetComponent<Unit>().tileX,
            selectedUnit.GetComponent<Unit>().tileZ]);
        return occupiedTile;
    }

    /// <summary>
    /// Returns true if the player clicks on a tile that is in the selected unit's movement range.
    /// </summary>
    /// <returns></returns>
    private bool CheckTileInMoveRange()
    {
        // Cast a ray from the cursor's position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // If the cursor is casting on to a tile...
            if (hit.transform.gameObject.CompareTag("Tile"))
            {
                // Get the clicked tile's X and Z map grid positions. 
                int clickedTileX = hit.transform.GetComponent<Tile>().tileX;
                int clickedTileZ = hit.transform.GetComponent<Tile>().tileZ;
                // Look up the clicked tile's node in the 2D graph array.
                Node clickedNode = mapManager.graph[clickedTileX, clickedTileZ];

                // If the clicked node is in the selected unit's movement range,
                // And the node's tile is not occupied by a different unit... 
                if (selectedUnitMoveRange.Contains(clickedNode) &&
                    (hit.transform.gameObject.GetComponent<Tile>().unitOccupyingTile == null ||
                        hit.transform.gameObject.GetComponent<Tile>().unitOccupyingTile == selectedUnit))
                {
                    // Start generating a path for the unit and return true.
                    selectedUnit.GetComponent<Unit>().path = mapManager.GeneratePathTo(clickedTileX, clickedTileZ);
                    return true;
                }
            }
            // If the cursor is casting onto a unit...
            else if (hit.transform.gameObject.CompareTag("Unit"))
            {
                // If the player clicks on a unit from the enemy team, return false.
                if (hit.transform.parent.GetComponent<Unit>().teamNumber !=
                    selectedUnit.GetComponent<Unit>().teamNumber)
                    return false;
                // If the unit is on the player's team, start generating a path for the unit and return true.
                else if (hit.transform.gameObject == selectedUnit)
                {
                    selectedUnit.GetComponent<Unit>().path = mapManager.GeneratePathTo(selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ);
                    return true;
                }
            }
        }
        // If none of the above conditions are met, return false.
        return false;
    }

    #endregion


    #region Unit Movement

    /// <summary>
    /// Handles all selections made with the left mouse button to any tile or unit.
    /// </summary>
    public void SelectUnit()
    {
        // If there was no unit selected...
        if (selectedUnit == null)
        {
            // And if the cursor is currently highlighting a tile that is occupied by a unit...
            if (unitSelected == false
                && mapUIManager.highlightedTile != null
                && mapUIManager.highlightedTile.GetComponent<Tile>().unitOccupyingTile != null)
            {
                // Store that unit in a temporary game object.
                GameObject tempSelectedUnit = mapUIManager.highlightedTile.GetComponent<Tile>().unitOccupyingTile;

                // If that unit is unselected and it is on the current player's team...
                if (tempSelectedUnit.GetComponent<Unit>().movementState == MovementState.Unselected
                    && tempSelectedUnit.GetComponent<Unit>().teamNumber == gameManager.currentTeam)
                {
                    // Turn off any quads that are highlighted.
                    mapUIManager.DisableQuadUIUnitRange();

                    // The unit is now selected.
                    selectedUnit = tempSelectedUnit;
                    selectedUnit.GetComponent<Unit>().map = mapManager;
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Selected;
                    unitSelected = true;

                    selectedUnit.GetComponent<Unit>().SetAnimSelected();
                    audioManager.PlaySelectUnitSFX();

                    // Highlight the unit's movement range.
                    MovementRange();
                }
            }
        }
        // If a unit was already selected, it's not yet moving and it is on the player's team, we want to set up the unit to move.
        else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected
            && selectedUnit.GetComponent<Unit>().movementQueue.Count == 0
            && CheckTileInMoveRange())
        {
            // Store the unit's previous tile position.
            unitSelectedPrevX = selectedUnit.GetComponent<Unit>().tileX;
            unitSelectedPrevZ = selectedUnit.GetComponent<Unit>().tileZ;
            unitSelectedPrevTile = selectedUnit.GetComponent<Unit>().occupiedTile;

            selectedUnit.GetComponent<Unit>().SetAnimMoving();

            // Disable the pause functionality temporarily.
            uIManager.canPause = false;
            uIManager.TogglePauseButton(false);

            // Move the unit on its movement path.
            selectedUnit.GetComponent<Unit>().Move();
            StartCoroutine(FinaliseMovement());
        }
        // If a unit has already finished its move, we want to set up the unit to attack or wait.
        else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Moved)
            AttackWaitSelection();
    }

    /// <summary>
    /// Deselects a unit that was previously selected, with the right mouse button.
    /// </summary>
    public void DeselectUnit()
    {
        // If a unit is currently selected...
        if (selectedUnit != null)
        {
            // Turn off any quads that are highlighted.
            mapUIManager.DisableQuadUIUnitRange();
            mapUIManager.DisableQuadUIUnitPath();

            if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected)
            {
                // Reset the unit's movement state to unselected, and deselect it.
                selectedUnit.GetComponent<Unit>().movementState = MovementState.Unselected;
                selectedUnit = null;
                unitSelected = false;
            }
            // Otherwise, if the unit was waiting after moving/attacking...
            else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Waiting)
            {
                // Deselect the unit.
                selectedUnit = null;
                unitSelected = false;
            }
            // In every other instance, return the unit to its previous map grid position.
            else
            {
                // Set the selected unit's current map grid position as unoccupied.
                mapManager.mapTiles[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ]
                    .GetComponent<Tile>().unitOccupyingTile = null;
                // Set the selected unit's previous map grid position as the occupied tile.
                mapManager.mapTiles[unitSelectedPrevX, unitSelectedPrevZ].GetComponent<Tile>().unitOccupyingTile = selectedUnit;

                // Return the unit to its previous map grid position.
                selectedUnit.GetComponent<Unit>().tileX = unitSelectedPrevX;
                selectedUnit.GetComponent<Unit>().tileZ = unitSelectedPrevZ;
                selectedUnit.GetComponent<Unit>().occupiedTile = unitSelectedPrevTile;
                selectedUnit.transform.position = mapManager.GetTileWorldSpace(unitSelectedPrevX, unitSelectedPrevZ);

                // Finally, deselect the unit.
                selectedUnit.GetComponent<Unit>().movementState = MovementState.Unselected;
                selectedUnit = null;
                unitSelected = false;
            }
        }
    }

    /// <summary>
    /// Sets all tiles that are occupied by units as occupied.
    /// </summary>
    public void SetTileOccupied()
    {
        // For each unit in each team...
        foreach (Transform team in mapManager.mapUnits.transform)
        {
            foreach (Transform unit in team)
            {
                // Get the unit's X and Z map grid positions.
                int unitTileX = unit.GetComponent<Unit>().tileX;
                int unitTileZ = unit.GetComponent<Unit>().tileZ;

                // Set the unit's occupied tile as their map grid position.
                unit.GetComponent<Unit>().occupiedTile = mapManager.mapTiles[unitTileX, unitTileZ];

                // Set their unit's map grid position as occupied by the unit.
                mapManager.mapTiles[unitTileX, unitTileZ].GetComponent<Tile>().unitOccupyingTile = unit.gameObject;
            }
        }
    }

    /// <summary>
    /// Sets the unit's tile as occupied after moving, and sets them up to attack or wait.
    /// </summary>
    public void HighlightAttackWaitRange()
    {
        // Set the selected unit's tile as occupied by the selected unit.
        mapManager.mapTiles[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ]
            .GetComponent<Tile>().unitOccupyingTile = selectedUnit;

        // Set the selected unit's state to moved.
        selectedUnit.GetComponent<Unit>().movementState = MovementState.Moved;

        // Now that the unit has finished moving, the player can pause.
        uIManager.canPause = true;
        uIManager.TogglePauseButton(true);

        // Highlight the selected unit's atttackable tiles, and occupied tile.
        if (selectedUnit != null)
        {
            mapUIManager.HighlightAttackRange(GetAttackRangeAfterMoving());
            mapUIManager.HighlightMovementRange(GetOccupiedTile());
        }
    }

    /// <summary>
    /// This function controls the player's choice after moving a unit, i.e. waiting or attacking.
    /// </summary>
    private void AttackWaitSelection()
    {
        // Raycast the cursor's position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Create a container of the selected unit's attack range.
        HashSet<Node> attackableTiles = GetAttackRangeAfterMoving();

        if (Physics.Raycast(ray, out hit))
        {
            // If the cursor casts to a tile occupied by a unit...
            if (hit.transform.gameObject.CompareTag("Tile")
                && hit.transform.GetComponent<Tile>().unitOccupyingTile != null)
            {
                // Get the unit occupying that tile and their map grid position.
                GameObject unit = hit.transform.GetComponent<Tile>().unitOccupyingTile;
                int unitX = unit.GetComponent<Unit>().tileX;
                int unitZ = unit.GetComponent<Unit>().tileZ;

                // If that unit is the selected unit...
                if (unit == selectedUnit)
                    Wait();
                // If that unit is a unit from the enemy team, that unit is attackable and it has remaining health points...
                else if (unit.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber
                    && attackableTiles.Contains(mapManager.graph[unitX, unitZ])
                    && unit.GetComponent<Unit>().currentHealth > 0)
                    Attack(unit);
            }
            // If the cursor casts to a unit...
            else if (hit.transform.parent != null
                && hit.transform.parent.gameObject.CompareTag("Unit"))
            {
                // Get the unit's map grid position.
                GameObject unit = hit.transform.parent.gameObject;
                int unitX = unit.GetComponent<Unit>().tileX;
                int unitZ = unit.GetComponent<Unit>().tileZ;

                // If the unit is the selected unit...
                if (unit == selectedUnit)
                    Wait();
                // If that unit is a unit from the enemy team, that unit is attackable and it has remaining health points...
                else if (unit.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber
                    && attackableTiles.Contains(mapManager.graph[unitX, unitZ])
                    && unit.GetComponent<Unit>().currentHealth > 0)
                    Attack(unit);
            }
        }
    }

    /// <summary>
    /// Sets the selected unit to attack another unit.
    /// </summary>
    /// <param name="unit">The unit that the selected unit is attacking.</param>
    private void Attack(GameObject unit)
    {
        // Commence the selected unit's attack on the enemy unit, and deselect the unit.
        StartCoroutine(battleManager.StartAttack(selectedUnit, unit));
        StartCoroutine(DeselectUnitAfterAttack(selectedUnit, unit));
    }

    /// <summary>
    /// Sets the selected unit to wait.
    /// </summary>
    public void Wait()
    {
        // Disable highlight quads showing their movement path.
        mapUIManager.DisableQuadUIUnitPath();

        //Set the selected unit to wait and deselect the unit.
        selectedUnit.GetComponent<Unit>().ChangeMatWait();
        selectedUnit.GetComponent<Unit>().SetAnimIdle();
        selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;
        DeselectUnit();
        CheckTeamWaiting();
    }

    /// <summary>
    /// Automatically ends the player's turn, if every unit on the player's team has finished their move.
    /// </summary>
    private void CheckTeamWaiting()
    {
        // If the enemy team has remaining units...
        if (gameManager.GetEnemyTeam(gameManager.currentTeam).transform.childCount != 0)
        {
            bool teamWaiting = true;

            // Check if all of the units on the current team are waiting.
            foreach (Transform unit in gameManager.GetCurrentTeam(gameManager.currentTeam).transform)
            {
                if (unit.GetComponent<Unit>().movementState != MovementState.Waiting)
                {
                    teamWaiting = false;
                    break;
                }
            }

            if (teamWaiting)
                gameManager.EndTurn();
        }
    }

    #endregion


    #region Coroutines

    /// <summary>
    /// Disables highlight quads before allowing the player to choose to attack or wait.
    /// </summary>
    /// <returns></returns>
    private IEnumerator FinaliseMovement()
    {
        mapUIManager.DisableQuadUIUnitRange();
        mapUIManager.DisableQuadUIUnitPath();

        // While the selected unit is moving, wait.
        while (selectedUnit.GetComponent<Unit>().movementQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        HighlightAttackWaitRange();

        selectedUnit.GetComponent<Unit>().SetAnimSelected();
    }

    /// <summary>
    /// Sets a selected unit to wait, after the unit has finished its attack.
    /// </summary>
    /// <param name="attacker">The unit on the current team, which has attacked a unit from the enemy team.</param>
    /// <param name="defender">The unit not on the current team, which has defended against an attack from the current team.</param>
    /// <returns></returns>
    private IEnumerator DeselectUnitAfterAttack(GameObject attacker, GameObject defender)
    {
        //SelectSound.Play();

        // Turn off any highlighted quads.
        mapUIManager.DisableQuadUIUnitRange();
        mapUIManager.DisableQuadUIUnitPath();

        // Wait a quarter of a second.
        yield return new WaitForSeconds(.25f);

        // Wait for the units to stop attacking.
        while (attacker.GetComponent<Unit>().combatQueue.Count > 0)
            yield return new WaitForEndOfFrame();
        while (defender.GetComponent<Unit>().combatQueue.Count > 0)
            yield return new WaitForEndOfFrame();

        // Change the unit's movement state to wait.
        selectedUnit.GetComponent<Unit>().ChangeMatWait();
        selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;

        DeselectUnit();
        CheckTeamWaiting();
    }

    #endregion
}
