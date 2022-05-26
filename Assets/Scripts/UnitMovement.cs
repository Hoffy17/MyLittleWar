using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovement : MonoBehaviour
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


    #region Movement Range

    /// <summary>
    /// Calculates the map grid nodes that need to be highlighted when a unit's movement range is displayed.
    /// </summary>
    private void MovementRange()
    {
        // A container of nodes representing the tiles that a unit can move to.
        HashSet<Node> movementRange = new HashSet<Node>();
        // A container of nodes representing the tiles occupied by enemies in the selected unit's movement range.
        HashSet<Node> enemiesInRange = new HashSet<Node>();
        //// A container of nodes representing the tiles occupied by enemies in a unit's movement range.
        //HashSet<Node> enemiesInRange = new HashSet<Node>();

        // Store the selected unit's start position on the map grid in a local variable.
        Node startNode = mapManager.graph[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ];

        // Store the selected unit's attack range and move speed in local variables.
        int attackRange = selectedUnit.GetComponent<Unit>().attackRange;
        int movespeed = selectedUnit.GetComponent<Unit>().moveSpeed;

        // Calculate the nodes that exist in the selected unit's movement and attack ranges.
        movementRange = GetMovementRange(movementRange, movespeed, startNode);
        enemiesInRange = GetEnemiesInRange(movementRange, enemiesInRange, attackRange, startNode);

        // If the nodes in the selected unit's attack range are occupied...
        //foreach (Node node in attackableEnemies)
        //    if (mapTiles[node.x, node.z].GetComponent<ClickableTile>().unitOccupyingTile != null)
        //    {
        //        GameObject unitOccupyingSelectedTile = mapTiles[node.x, node.z].GetComponent<ClickableTile>().unitOccupyingTile;

        //        // And the units occupying those tiles are not on the current player's team...
        //        if (unitOccupyingSelectedTile.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber)
        //            //Add those nodes to the container of enemies in a unit's movement range.
        //            enemiesInRange.Add(node);
        //    }

        // Finally, highlight the selected unit's movement range and attackable enemies.
        mapUIManager.HighlightAttackRange(enemiesInRange);
        mapUIManager.HighlightMovementRange(movementRange);

        selectedUnitMoveRange = movementRange;

        //selectedUnitMoveRangeTotal = GetTotalRange(movementRange, attackableEnemies);
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

    /// <summary>
    /// Returns a container of nodes that the selected unit can move to from its current position.
    /// </summary>
    /// <param name="movementRange">A container of nodes representing the tiles that a unit can move to.</param>
    /// <param name="movespeed">The selected unit's movement range.</param>
    /// <param name="startNode">The selected unit's start position on the map grid.</param>
    /// <returns></returns>
    private HashSet<Node> GetMovementRange(HashSet<Node> movementRange, int movespeed, Node startNode)
    {
        // Create a 2D array containing the costs for units to enter all of the tiles on the map grid.
        float[,] cost = new float[mapManager.mapSizeX, mapManager.mapSizeZ];

        // A container, and temporary container, of nodes that are highlighted in a selected unit's movement range.
        HashSet<Node> uIHighlight = new HashSet<Node>();
        HashSet<Node> tempUIHighlight = new HashSet<Node>();

        // Add the selected unit's start node into the container of nodes that the unit can move to.
        movementRange.Add(startNode);

        // For each of the start node's neighbours...
        foreach (Node node in startNode.neighbours)
        {
            // Add their costs to the local 2D array.
            cost[node.x, node.z] = mapManager.CostToEnterTile(node.x, node.z);

            // If the cost to enter the neighbouring nodes is less than or equal to the unit's move speed...
            if (movespeed - cost[node.x, node.z] >= 0)
                // Add those neighbouring nodes to the container of nodes to be highlighted.
                uIHighlight.Add(node);
        }

        // Insert those highlighted nodes into the unit's movement range.
        movementRange.UnionWith(uIHighlight);

        while (uIHighlight.Count != 0)
        {
            // For all of the nodes neighbouring the nodes that have been highlighted...
            foreach (Node node in uIHighlight)
                foreach (Node neighbour in node.neighbours)
                    // If those neighbours have not already been added to the unit's movement range...
                    if (!movementRange.Contains(neighbour))
                    {
                        // Calculate the cost to move from those nodes to their neighbouring nodes.
                        cost[neighbour.x, neighbour.z] = mapManager.CostToEnterTile(neighbour.x, neighbour.z) + cost[node.x, node.z];

                        // If the cost to enter the neighbouring nodes is less than or equal to the unit's move speed...
                        if (movespeed - cost[neighbour.x, neighbour.z] >= 0)
                            // Add those neighbouring nodes to the container of nodes to be highlighted.
                            tempUIHighlight.Add(neighbour);
                    }

            // Store the hightlighted nodes in the selected unit's movement range.
            uIHighlight = tempUIHighlight;
            movementRange.UnionWith(uIHighlight);
            // Refresh the temporary container of highlighted nodes.
            tempUIHighlight = new HashSet<Node>();
        }

        return movementRange;
    }

    /// <summary>
    /// Returns a container of nodes that represent the enemies standing in the selected unit's movement range.
    /// </summary>
    /// <param name="movementRange">A container of nodes representing the tiles that a unit can move to.</param>
    /// <param name="enemiesInRange">A container of nodes representing the tiles occupied by enemies.</param>
    /// <param name="attackRange">The selected unit's attack range.</param>
    /// <param name="startNode">The selected unit's start position on the map grid.</param>
    /// <returns></returns>
    private HashSet<Node> GetEnemiesInRange(HashSet<Node> movementRange, HashSet<Node> enemiesInRange, int attackRange, Node startNode)
    {
        // A container, and temporary container, of nodes that neighbour other nodes.
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash;
        // A container of nodes that represent the enemies that are within the selected unit's movement range.
        HashSet<Node> enemiesInRangeHash = new HashSet<Node>();

        // For all of the nodes in the selected unit's movement range...
        foreach (Node node in movementRange)
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

                // Continue to build a container of nodes neighbouring other nodes,
                // Until the for loop is the same as the selected unit's attack range. 
                if (i < attackRange - 1)
                    enemiesInRangeHash.UnionWith(neighbourHash);
            }

            // Remove the enemies in the selected unit's range from the container of neighbouring nodes.
            neighbourHash.ExceptWith(enemiesInRangeHash);
            enemiesInRangeHash = new HashSet<Node>();
            // Add the remaining neighbouring nodes into the hash of enemies within the selected unit's range.
            enemiesInRange.UnionWith(neighbourHash);
        }

        // Remove the selected unit's start node from the container of enemies.
        enemiesInRange.Remove(startNode);

        return enemiesInRange;
    }

    /// <summary>
    /// Returns a container of nodes that represent the attackable enemies in the selected unit's attack range.
    /// </summary>
    /// <returns></returns>
    private HashSet<Node> GetEnemiesAttackable()
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
        // If a unit was already selected, and it is on the player's team, we want to set up the unit to move.
        else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected
            && selectedUnit.GetComponent<Unit>().movementQueue.Count == 0
            && CheckTileInMoveRange())
        {
            // Store the unit's previous tile position.
            unitSelectedPrevX = selectedUnit.GetComponent<Unit>().tileX;
            unitSelectedPrevZ = selectedUnit.GetComponent<Unit>().tileZ;
            unitSelectedPrevTile = selectedUnit.GetComponent<Unit>().occupiedTile;

            //sound.Play();
            selectedUnit.GetComponent<Unit>().SetAnimMoving();

            // Move the unit to the next tile in their path.
            uIManager.canPause = false;
            selectedUnit.GetComponent<Unit>().AdvanceNextTile();
            StartCoroutine(FinaliseMovement());
        }
        // If a unit has already finished its move, we want to set up the unit to attack or wait.
        else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Moved)
            AttackOrWait();
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
    /// Disables highlight quads before allowing the player to choose to attack or wait.
    /// </summary>
    /// <returns></returns>
    private IEnumerator FinaliseMovement()
    {
        mapUIManager.DisableQuadUIUnitRange();
        mapUIManager.DisableQuadUIUnitPath();

        while (selectedUnit.GetComponent<Unit>().movementQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        FinaliseMovementPos();
        selectedUnit.GetComponent<Unit>().SetAnimSelected();
    }

    /// <summary>
    /// Sets the unit's tile as occupied after moving, and sets them up to attack.
    /// </summary>
    private void FinaliseMovementPos()
    {
        // Set the selected unit's tile as occupied by the selected unit.
        mapManager.mapTiles[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ]
            .GetComponent<Tile>().unitOccupyingTile = selectedUnit;

        // Set the selected unit's state to moved.
        selectedUnit.GetComponent<Unit>().movementState = MovementState.Moved;

        // Highlight the selected unit's atttackable tiles.
        if (selectedUnit != null)
        {
            mapUIManager.HighlightAttackRange(GetEnemiesAttackable());
            mapUIManager.HighlightMovementRange(GetOccupiedTile());
        }

        uIManager.canPause = true;
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
    /// This function controls the player's choice after moving a unit, i.e. waiting or attacking.
    /// </summary>
    private void AttackOrWait()
    {
        // Raycast the cursor's position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Create a container of the selected unit's attack range.
        HashSet<Node> attackableTiles = GetEnemiesAttackable();

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
                {
                    // Disable highlight quads showing their movement path.
                    mapUIManager.DisableQuadUIUnitPath();

                    //Set the selected unit to wait and deselect the unit.
                    selectedUnit.GetComponent<Unit>().Wait();
                    selectedUnit.GetComponent<Unit>().SetAnimIdle();
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;
                    DeselectUnit();
                    CheckTeamWaiting();
                }
                // If that unit is a unit from the enemy team, that unit is attackable and it has remaining health points...
                else if (unit.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber
                    && attackableTiles.Contains(mapManager.graph[unitX, unitZ])
                    && unit.GetComponent<Unit>().currentHealth > 0)
                {
                    // Commence the selected unit's attack on the enemy unit, and deselect the unit.
                    StartCoroutine(battleManager.StartAttack(selectedUnit, unit));
                    StartCoroutine(DeselectUnitAfterAttack(selectedUnit, unit));
                }
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
                {
                    // Disable highlight quads showing their movement path.
                    mapUIManager.DisableQuadUIUnitPath();

                    // Set the selected unit to wait and deselect the unit.
                    selectedUnit.GetComponent<Unit>().Wait();
                    selectedUnit.GetComponent<Unit>().SetAnimIdle();
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;
                    DeselectUnit();
                    CheckTeamWaiting();
                }
                // If that unit is a unit from the enemy team, that unit is attackable and it has remaining health points...
                else if (unit.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber
                    && attackableTiles.Contains(mapManager.graph[unitX, unitZ])
                    && unit.GetComponent<Unit>().currentHealth > 0)
                {
                    // Commence the selected unit's attack on the enemy unit, and deselect the unit.
                    StartCoroutine(battleManager.StartAttack(selectedUnit, unit));
                    StartCoroutine(DeselectUnitAfterAttack(selectedUnit, unit));
                }
            }
        }
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
        selectedUnit.GetComponent<Unit>().Wait();
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
}
