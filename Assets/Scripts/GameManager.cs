using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]

    [Tooltip("The MapManager script.")]
    [SerializeField]
    private MapManager mapManager;

    [Header("Units & Teams")]

    [Tooltip("The number of teams participating in a game.")]
    [SerializeField]
    private int numberOfTeams = 2;
    [Tooltip("The current player who's turn it is to move units and attack.")]
    [SerializeField]
    public int currentTeam;

    //[SerializeField]
    //private GameObject unitsOnMap;

    [Tooltip("Player one's team.")]
    [SerializeField]
    private GameObject team1;
    [Tooltip("Player two's team.")]
    [SerializeField]
    private GameObject team2;

    [Tooltip("The tile that is highlighted when the player hovers the cursor over a unit.")]
    [NonSerialized]
    private GameObject highlightedUnit;
    [Tooltip("The tile that is highlighted when the player hovers the cursor over a tile.")]
    [HideInInspector]
    public GameObject highlightedTile;

    [Tooltip("Checks whether a unit's information is being displayed to the canvas.")]
    [NonSerialized]
    private bool displayingUnitInfo;

    [Header("Map")]

    [NonSerialized]
    private Ray ray;
    [NonSerialized]
    private RaycastHit hit;

    [Tooltip("The tile on the X axis that the cursor is passing to the MapManager.")]
    [NonSerialized]
    private int cursorX;
    [Tooltip("The tile on the Z axis that the cursor is passing to the MapManager.")]
    [NonSerialized]
    private int cursorZ;
    [Tooltip("The current tile's X grid position, which the cursor is highlighting.")]
    [NonSerialized]
    private int highlightedTileX;
    [Tooltip("The current tile's Z grid position, which the cursor is highlighting.")]
    [NonSerialized]
    private int highlightedTileZ;

    //[NonSerialized]
    //private List<Node> currentPath;

    [Tooltip("A list of nodes that represents a unit's path across the map grid to the cursor's highlighted tile.")]
    [NonSerialized]
    private List<Node> currentPathToCursor;

    [Tooltip("Checks whether the cursor is highlighting a path from a selected unit's position to a highlighted tile.")]
    [NonSerialized]
    private bool currentPathExists;

    [Tooltip("The map grid position on the X axis, which the cursor is generating a path to.")]
    [NonSerialized]
    private int pathToX;
    [Tooltip("The map grid position on the Z axis, which the cursor is generating a path to.")]
    [NonSerialized]
    private int pathToZ;

    //[NonSerialized]
    //private GameObject quadNeighbouringUnit;

    [Header("Map Materials")]

    [Tooltip("A material applied to a quad, showing a unit's path straight through a tile.")]
    [SerializeField]
    private Material uIUnitPath;
    [Tooltip("A material applied to a quad, showing a unit's path turning through a tile.")]
    [SerializeField]
    private Material uIUnitPathCurve;
    [Tooltip("A material applied to a quad, showing a unit's path to a destination tile.")]
    [SerializeField]
    private Material uIUnitPathArrow;
    [Tooltip("A material applied to a quad, showing the tile that the cursor is highlighting.")]
    [SerializeField]
    private Material uICursor;

    [Header("UI")]

    [Tooltip("The text showing the current player's turn.")]
    [SerializeField]
    private TMP_Text textCurrentPlayer;
    [Tooltip("The canvas displayed when a game ends.")]
    [SerializeField]
    private Canvas canvasGameOver;

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
        //Reset the current team.
        currentTeam = 0;

        //Reset the canvas showing unit information.
        displayingUnitInfo = false;

        //Reset the list of nodes from a unit to a cursor position.
        currentPathToCursor = new List<Node>();
        currentPathExists = false;

        //Find the components that display the current player's turn.
        playerTurnAnim = playerTurnMessage.GetComponent<Animator>();
        playerTurnText = playerTurnMessage.GetComponentInChildren<TextMeshProUGUI>();

        //Display the current player's turn.
        PrintCurrentTeam();
        UpdateUITeamHealthBarColour();
    }

    /// <summary>
    /// Here, Update is controlling the displaying of a movement path from a selected unit to the cursor's highlighted tile.
    /// </summary>
    private void Update()
    {
        //Keep track of the cursor's position.
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out hit))
        {
            //Check if the cursor is highlighting a tile and/or a unit.
            UpdateUICursor();
            UpdateUIUnit();

            //If there is a selected unit,
            //And its movement range contains a node that the cursor is highlighting...
            if (mapManager.selectedUnit != null &&
                mapManager.selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected &&
                mapManager.selectedUnitMoveRange.Contains(mapManager.graph[cursorX, cursorZ]))
            {
                //And if that highlighted tile is not the selected unit's current tile...
                if (cursorX != mapManager.selectedUnit.GetComponent<Unit>().tileX ||
                    cursorZ != mapManager.selectedUnit.GetComponent<Unit>().tileZ)
                {
                    //And if there isn't already a current path to another tile, or another unit that has been selected to move...
                    if (!currentPathExists && mapManager.selectedUnit.GetComponent<Unit>().movementQueue.Count == 0)
                    {
                        //Generate a path to the tile highlighted by the cursor.
                        currentPathToCursor = mapManager.GeneratePathTo(cursorX, cursorZ);
                        pathToX = cursorX;
                        pathToZ = cursorZ;

                        //If there are nodes in the current path to the tile...
                        if (currentPathToCursor.Count != 0)
                        {
                            for (int i = 0; i < currentPathToCursor.Count; i++)
                            {
                                //For all of those nodes, get their position on the map grid.
                                int nodeX = currentPathToCursor[i].x;
                                int nodeZ = currentPathToCursor[i].z;

                                //Set the material for the first node in the movement path.
                                if (i == 0)
                                {
                                    GameObject quad = mapManager.quadUIUnitPath[nodeX, nodeZ];
                                    quad.GetComponent<Renderer>().material = uICursor;
                                }
                                //Draw the movement path to the cursor position.
                                else if (i != 0 && (i + 1) != currentPathToCursor.Count)
                                    DrawUnitPath(nodeX, nodeZ, i);
                                //Draw the arrow at the end of the movement path.
                                else if (i == currentPathToCursor.Count - 1)
                                    DrawUnitPathArrow(nodeX, nodeZ, i);

                                //And turn on the renderers for each quad in the movement path.
                                mapManager.quadUIUnitPath[nodeX, nodeZ].GetComponent<Renderer>().enabled = true;
                            }
                        }
                        //There is currently a path to the cursor.
                        currentPathExists = true;
                    }
                    //Otherwise, if the path to the X or Z map grid position is not the same as the tile that the cursor is highlighting...
                    else if (pathToX != cursorX || pathToZ != cursorZ)
                    {
                        //And if there are nodes in the current path from the selected unit to the cursor...
                        if (currentPathToCursor.Count != 0)
                        {
                            for (int i = 0; i < currentPathToCursor.Count; i++)
                            {
                                //For all of those nodes, get their position on the map grid.
                                int nodeX = currentPathToCursor[i].x;
                                int nodeZ = currentPathToCursor[i].z;

                                //And turn off the renderers for each quad in the movement path.
                                mapManager.quadUIUnitPath[nodeX, nodeZ].GetComponent<Renderer>().enabled = false;
                            }
                        }
                        //There is not a path to the cursor.
                        currentPathExists = false;
                    }
                }
                //Otherwise, if the cursor's highlighted tile is the same as the selected unit's current tile...
                else if (cursorX == mapManager.selectedUnit.GetComponent<Unit>().tileX &&
                    cursorZ == mapManager.selectedUnit.GetComponent<Unit>().tileZ)
                {
                    //Disable the quads displaying a movement path.
                    mapManager.DisableQuadUIUnitPath();
                    currentPathExists = false;
                }
            }
        }
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// Controls the button that the player clicks to confirm the end of their turn.
    /// </summary>
    public void EndTurn()
    {
        //If there is no currently selected unit...
        if (mapManager.selectedUnit == null)
        {
            //Switch to the other player's team.
            SwitchCurrentTeam();

            //Animate the message communicating this change.
            if (currentTeam == 1)
            {
                playerTurnAnim.SetTrigger("Slide Left");
                playerTurnText.SetText("Player Two's Turn");
            }
            else if (currentTeam == 0)
            {
                playerTurnAnim.SetTrigger("Slide Right");
                playerTurnText.SetText("Player One's Turn");
            }

            //UpdateUITeamHealthBarColour();
            PrintCurrentTeam();
        }
    }

    /// <summary>
    /// Returns a game object that contains all of the current team's units.
    /// </summary>
    /// <param name="teamNumber">The index of the current team.</param>
    /// <returns></returns>
    private GameObject GetCurrentTeam(int teamNumber)
    {
        GameObject team = null;

        if (teamNumber == 0)
            team = team1;
        else if (teamNumber == 1)
            team = team2;

        return team;
    }

    /// <summary>
    /// Increments the current team number when a player ends their turn.
    /// </summary>
    private void SwitchCurrentTeam()
    {
        ResetTeam(GetCurrentTeam(currentTeam));
        currentTeam++;

        //If the current team exceeds the number of teams, revert back to zero.
        if (currentTeam == numberOfTeams)
            currentTeam = 0;
    }

    /// <summary>
    /// Re-enables movement for all of the unit's on a given team.
    /// </summary>
    /// <param name="team">The current team whose units are being reset to move again.</param>
    private void ResetTeam(GameObject team)
    {
        //For each unit in a team...
        foreach (Transform unit in team.transform)
        {
            //Reset the unit's movement path.
            unit.GetComponent<Unit>().path = null;
            //Set the unit's movement state to unselected.
            unit.GetComponent<Unit>().movementState = MovementState.Unselected;
            //Reset the unit's movement turn so they can move again.
            unit.GetComponent<Unit>().moveCompleted = false;
            //Reset the unit's material to its default material.
            unit.gameObject.GetComponentInChildren<Renderer>().material = unit.GetComponent<Unit>().unitMat;

            //unit.GetComponent<Unit>().PlayIdleAnim();
        }
    }

    /// <summary>
    /// Highlights the quad that the cursor is currently casting to.
    /// </summary>
    private void UpdateUICursor()
    {
        //If the cursor is casting to a tile, highlight it.
        if (hit.transform.CompareTag("Tile"))
        {
            if (highlightedTile == null)
                HighlightTile(hit.transform.gameObject);
            //If the highlighted tile is not the same as the tile that the cursor is casting to...
            else if (highlightedTile != hit.transform.gameObject)
            {
                //Get the new highlighted tile's position.
                highlightedTileX = highlightedTile.GetComponent<Tile>().tileX;
                highlightedTileZ = highlightedTile.GetComponent<Tile>().tileZ;

                //Turn off the previously highlighted tile.
                mapManager.quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;

                //Highlight the new tile.
                HighlightTile(hit.transform.gameObject);
            }
        }
        //If the cursor is casting to a unit, highlight its tile.
        else if (hit.transform.CompareTag("Unit"))
        {
            if (highlightedTile == null)
                HighlightTile(hit.transform.parent.gameObject);
            //If the highlighted tile is not the same as the tile that the cursor is casting to...
            else if (highlightedTile != hit.transform.gameObject)
            {
                //And unit on that tile has not yet moved...
                if (hit.transform.parent.gameObject.GetComponent<Unit>().movementQueue.Count == 0)
                {
                    //Get the new highlighted tile's position.
                    highlightedTileX = highlightedTile.GetComponent<Tile>().tileX;
                    highlightedTileZ = highlightedTile.GetComponent<Tile>().tileZ;

                    //Turn off the previously highlighted tile.
                    mapManager.quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;

                    //Highlight the new tile.
                    HighlightTile(hit.transform.parent.gameObject);
                }
            }
        }
        //Otherwise, turn off the previously highlighted tile.
        else
            mapManager.quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;
    }

    /// <summary>
    /// When the cursor casts to a unit, display its unit information.
    /// </summary>
    private void UpdateUIUnit()
    {
        //If unit information is not currently displaying, and the cursor is casting to a unit...
        if (!displayingUnitInfo)
        {
            if (hit.transform.CompareTag("Unit"))
            {
                //Set the unit to highlighted and display its stats.
                highlightedUnit = hit.transform.parent.gameObject;
                Unit unit = hit.transform.parent.gameObject.GetComponent<Unit>();

                PrintUnitInfo(unit);
            }
            //Otherwise if the cursor is casting to a tile that is occupied...
            else if (hit.transform.CompareTag("Tile")
                && hit.transform.GetComponent<Tile>().unitOccupyingTile != null)
            {
                //Set that tile's occupied unit to highlighted and display its stats.
                highlightedUnit = hit.transform.GetComponent<Tile>().unitOccupyingTile;
                Unit unit = highlightedUnit.GetComponent<Unit>();

                PrintUnitInfo(unit);
            }
        }
        //Otherwise if the cusor is casting to a unit that is not the currently highlighted unit...
        else if (hit.transform.gameObject.CompareTag("Unit")
            && hit.transform.parent.gameObject != highlightedUnit)
        {
            //Turn off the canvas displaying the previously highlighted unit's stats.
            canvasUnitInfo.enabled = false;
            displayingUnitInfo = false;
        }
        //Or if the cursor is casting to a tile...
        else if (hit.transform.gameObject.CompareTag("Tile"))
        {
            //And that tile is not occupied, turn off the canvas displaying units' stats.
            if (hit.transform.GetComponent<Tile>().unitOccupyingTile == null)
            {
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
            //Or if the tile is occupied by a unit different to the currently highlighted unit,
            //Turn off the canvas displaying units' stats.
            else if (hit.transform.GetComponent<Tile>().unitOccupyingTile != highlightedUnit)
            {
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
        }
    }

    /// <summary>
    /// Switches the colour of units' health bars between red and blue, depending on the current team.
    /// </summary>
    private void UpdateUITeamHealthBarColour()
    {
        for (int i = 0; i < numberOfTeams; i++)
        {
            GameObject team = GetCurrentTeam(i);

            if (team == GetCurrentTeam(currentTeam))
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
    /// Turns on the quad for the tile that the cursor is currently casting to.
    /// </summary>
    /// <param name="tile">The tile that the cursor is currently casting to.</param>
    private void HighlightTile(GameObject tile)
    {
        //If the cursor is casting to a tile, get that tile's X and Z positions on the map grid.
        if (hit.transform.CompareTag("Tile"))
        {
            highlightedTileX = tile.GetComponent<Tile>().tileX;
            highlightedTileZ = tile.GetComponent<Tile>().tileZ;
        }
        //If the cursor is casting to a unit, get that unit's X and Z positions on the map grid.
        else if (hit.transform.CompareTag("Unit"))
        {
            highlightedTileX = tile.GetComponent<Unit>().tileX;
            highlightedTileZ = tile.GetComponent<Unit>().tileZ;
        }

        cursorX = highlightedTileX;
        cursorZ = highlightedTileZ;

        //Turn on the renderer for the quad that is being highlighted by the cursor.
        mapManager.quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = true;

        //Set the tile, or the tile occupied by a unit, that the cursor is casting to as highlighted.
        if (hit.transform.CompareTag("Tile"))
            highlightedTile = tile;
        else if (hit.transform.CompareTag("Unit"))
            highlightedTile = tile.GetComponent<Unit>().occupiedTile;
    }

    /// <summary>
    /// Highlights the quads that form a path from the selected unit's position to the target node that the cursor is highlighting.
    /// </summary>
    /// <param name="nodeX">The position on the map grid's X axis for the next node in the unit's path, which the cursor is highlighting a path to.</param>
    /// <param name="nodeZ">The position on the map grid's Z axis for the next node in the unit's path, which the cursor is highlighting a path to.</param>
    /// <param name="i">An index for a node in the path from the selected unit's position and the last node in its movement path.</param>
    private void DrawUnitPath(int nodeX, int nodeZ, int i)
    {
        //Keep track of the previous, current and next tile in the path to the target node.
        Vector2 prevTile = new Vector2(currentPathToCursor[i - 1].x + 1, currentPathToCursor[i - 1].z + 1);
        Vector2 currTile = new Vector2(currentPathToCursor[i].x + 1, currentPathToCursor[i].z + 1);
        Vector2 nextTile = new Vector2(currentPathToCursor[i + 1].x + 1, currentPathToCursor[i + 1].z + 1);

        //Keep track of any changes of direction that take place in the path from the selected unit's position and its target.
        Vector2 prevToCurrVector = VectorDirection(prevTile, currTile);
        Vector2 currToNextVector = VectorDirection(currTile, nextTile);

        //Draw a quad from one node to another, in the path to the node from the selected unit to the node that the mouse is highlighting.
        //This will turn on a quad in the path, set its material and rotate it depending on the vector direction between the nodes.
        if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.right)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 270, uIUnitPath);
        else if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.up)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 180, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.down)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 270, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.left)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 90, uIUnitPath);
        else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.up)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 90, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.down)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.up)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPath);
        else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.right)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.left)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 270, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.down)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPath);
        else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.right)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 90, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.left)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 180, uIUnitPathCurve);
    }

    /// <summary>
    /// Highlights the final quad in the path from the selected unit's position to the target node that the cursor is highlighting.
    /// </summary>
    /// <param name="nodeX">The position on the map grid's X axis for the target node in the unit's path, which the cursor is highlighting a path to.</param>
    /// <param name="nodeZ">The position on the map grid's Z axis for the target node in the unit's path, which the cursor is highlighting a path to.</param>
    /// <param name="i">The index for the last node in the path from the selected unit's position and the last node in its movement path.</param>
    private void DrawUnitPathArrow(int nodeX, int nodeZ, int i)
    {
        //Keep track of the previous and target tile in the path to the target node.
        Vector2 prevTile = new Vector2(currentPathToCursor[i - 1].x + 1, currentPathToCursor[i - 1].z + 1);
        Vector2 currTile = new Vector2(currentPathToCursor[i].x + 1, currentPathToCursor[i].z + 1);

        //Keep track of the vector direction that takes place at the end of the path from the selected unit's position and its target.
        Vector2 prevToCurrVector = VectorDirection(prevTile, currTile);

        //Draw the final quad in the path to the node from the selected unit to the node that the mouse is highlighting.
        //This turns on the final quad in the path, set its material and rotates it depending on the vector direction from the previous node in the path.
        if (prevToCurrVector == Vector2.right)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 270, uIUnitPathArrow);
        else if (prevToCurrVector == Vector2.left)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 90, uIUnitPathArrow);
        else if (prevToCurrVector == Vector2.up)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPathArrow);
        else if (prevToCurrVector == Vector2.down)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 180, uIUnitPathArrow);
    }

    /// <summary>
    /// Enables a quad in the movement path between a selected unit and a tile that the cursor is highlighting.
    /// </summary>
    /// <param name="nodeX">The position on the map grid's X axis for a node in the unit's path, which the cursor is highlighting a path to.</param>
    /// <param name="nodeZ">The position on the map grid's Z axis for a node in the unit's path, which the cursor is highlighting a path to.</param>
    /// <param name="rotX">The X rotation of a quad in a unit's path.</param>
    /// <param name="rotZ">The Z rotation of a quad in a unit's path.</param>
    /// <param name="mat">The material of a quad in a unit's path.</param>
    private void DrawUnitPathQuad(int nodeX, int nodeZ, int rotX, int rotZ, Material mat)
    {
        GameObject quad = mapManager.quadUIUnitPath[nodeX, nodeZ];
        quad.GetComponent<Transform>().rotation = Quaternion.Euler(rotX, 0, rotZ);
        quad.GetComponent<Renderer>().material = mat;
        quad.GetComponent<Renderer>().enabled = true;
    }

    /// <summary>
    /// Returns a vector direction between two nodes in a selected unit's path.
    /// </summary>
    /// <param name="currVector">The vector direction of a node in the selected unit's path.</param>
    /// <param name="nextVector">The vector direction of the next node in the selected unit's path.</param>
    /// <returns></returns>
    private Vector2 VectorDirection(Vector2 currVector, Vector2 nextVector)
    {
        Vector2 vectorDirection = (nextVector - currVector).normalized;

        if (vectorDirection == Vector2.right)
            return Vector2.right;
        else if (vectorDirection == Vector2.left)
            return Vector2.left;
        else if (vectorDirection == Vector2.up)
            return Vector2.up;
        else if (vectorDirection == Vector2.down)
            return Vector2.down;
        else
        {
            return new Vector2();
        }
    }

    /// <summary>
    /// Prints the current player's turn to the UI.
    /// </summary>
    private void PrintCurrentTeam()
    {
        textCurrentPlayer.SetText("Current Player's Turn: Player " + (currentTeam + 1).ToString());
    }

    /// <summary>
    /// Print's a highlighted unit's stats to its own canvas.
    /// </summary>
    /// <param name="unit">The unit that the cursor is currently highlighting.</param>
    private void PrintUnitInfo(Unit unit)
    {
        //Turn on the canvas that displays units' stats.
        canvasUnitInfo.enabled = true;
        displayingUnitInfo = true;

        //Pass the units' stats into the canvas' UI elements.
        imageUnitPortrait.sprite = unit.portrait;
        textUnitName.SetText(unit.name);
        textUnitHealth.SetText(unit.currentHealth.ToString());
        textUnitAttackDamage.SetText(unit.attackDamage.ToString());
        textUnitAttackRange.SetText(unit.attackRange.ToString());
        textUnitMoveSpeed.SetText(unit.moveSpeed.ToString());
    }

    /// <summary>
    /// Turns on the endgame UI and passes the winner as a string into the UI.
    /// </summary>
    /// <param name="winner">A string containing the winner.</param>
    private void PrintVictor(string winner)
    {
        canvasGameOver.enabled = true;
        canvasGameOver.GetComponentInChildren<TextMeshProUGUI>().SetText(winner);
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
        while (attacker.GetComponent<Unit>().combatQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        while (defender.GetComponent<Unit>().combatQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        if (team1.transform.childCount == 0)
            PrintVictor("Victor: Team Two!");
        else if (team2.transform.childCount == 0)
            PrintVictor("Victor: Team One!");
    }

    #endregion
}
