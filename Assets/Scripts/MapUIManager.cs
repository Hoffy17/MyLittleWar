using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapUIManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The GameManager script.")]
    [SerializeField]
    private GameManager gameManager;
    [Tooltip("The GameManager script.")]
    [SerializeField]
    private MapManager mapManager;
    [Tooltip("The UnitMovement script.")]
    [SerializeField]
    private UnitMovement unitMovement;

    [Header("Highlighted Tiles")]
    [Tooltip("The tile that is highlighted when the player hovers the cursor over a unit.")]
    [HideInInspector]
    public GameObject highlightedUnit;
    [Tooltip("The tile that is highlighted when the player hovers the cursor over a tile.")]
    [HideInInspector]
    public GameObject highlightedTile;
    [Tooltip("The current tile's X grid position, which the cursor is highlighting.")]
    [NonSerialized]
    private int highlightedTileX;
    [Tooltip("The current tile's Z grid position, which the cursor is highlighting.")]
    [NonSerialized]
    private int highlightedTileZ;

    [Header("Path to Cursor")]
    [Tooltip("The tile on the X axis that the cursor is passing to the MapManager.")]
    [HideInInspector]
    public int cursorX;
    [Tooltip("The tile on the Z axis that the cursor is passing to the MapManager.")]
    [HideInInspector]
    public int cursorZ;
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

    [Header("Quad UI")]
    [Tooltip("A 2D array of quads that are activated to highlight a unit's movement range and attackable enemies.")]
    [HideInInspector]
    public GameObject[,] quadUIUnitRange;
    [Tooltip("A 2D array of quads that are activated to highlight a unit's proposed movement path.")]
    [HideInInspector]
    public GameObject[,] quadUIUnitPath;
    [Tooltip("A 2D array of quads that are activated to highlight a single tile that the cursor is raycasting to.")]
    [HideInInspector]
    public GameObject[,] quadUICursor;

    [Header("Map UI")]
    [Tooltip("The quad prefab that is instantiated to highlight a unit's movement range and attackable enemies.")]
    [SerializeField]
    public GameObject mapUIUnitRange;
    [Tooltip("The quad prefab that is instantiated to highlight a unit's proposed movement path.")]
    [SerializeField]
    public GameObject mapUIUnitPath;
    [Tooltip("The quad prefab that is instantiated to highlight a single tile that the cursor is raycasting to.")]
    [SerializeField]
    public GameObject mapUICursor;

    [Header("Map UI Materials")]
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

    [Header("Map Range Materials")]
    //[SerializeField]
    //private Material uIMatGreen;
    [Tooltip("Material applied to quads when displaying a unit's attackable tiles.")]
    [SerializeField]
    private Material uIMatRed;
    [Tooltip("Material applied to quads when displaying a unit's movement range.")]
    [SerializeField]
    private Material uIMatBlue;

    #endregion


    #region Unity Functions

    private void Start()
    {
        //Reset the list of nodes from a unit to a cursor position.
        currentPathToCursor = new List<Node>();
        currentPathExists = false;
    }

    #endregion


    #region Custom Functions

    #region Calculations

    /// <summary>
    /// Calculates the quad that the cursor is currently casting to.
    /// </summary>
    public void CalculateHighlightTile()
    {
        //If the cursor is casting to a tile, highlight it.
        if (gameManager.hit.transform.CompareTag("Tile"))
        {
            if (highlightedTile == null)
                HighlightTile(gameManager.hit.transform.gameObject);
            //If the highlighted tile is not the same as the tile that the cursor is casting to...
            else if (highlightedTile != gameManager.hit.transform.gameObject)
            {
                //Get the new highlighted tile's position.
                highlightedTileX = highlightedTile.GetComponent<Tile>().tileX;
                highlightedTileZ = highlightedTile.GetComponent<Tile>().tileZ;

                //Turn off the previously highlighted tile.
                quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;

                //Highlight the new tile.
                HighlightTile(gameManager.hit.transform.gameObject);
            }
        }
        //If the cursor is casting to a unit, highlight its tile.
        else if (gameManager.hit.transform.CompareTag("Unit"))
        {
            if (highlightedTile == null)
                HighlightTile(gameManager.hit.transform.parent.gameObject);
            //If the highlighted tile is not the same as the tile that the cursor is casting to...
            else if (highlightedTile != gameManager.hit.transform.gameObject)
            {
                //And unit on that tile has not yet moved...
                if (gameManager.hit.transform.parent.gameObject.GetComponent<Unit>().movementQueue.Count == 0)
                {
                    //Get the new highlighted tile's position.
                    highlightedTileX = highlightedTile.GetComponent<Tile>().tileX;
                    highlightedTileZ = highlightedTile.GetComponent<Tile>().tileZ;

                    //Turn off the previously highlighted tile.
                    quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;

                    //Highlight the new tile.
                    HighlightTile(gameManager.hit.transform.parent.gameObject);
                }
            }
        }
        //Otherwise, turn off the previously highlighted tile.
        else
            quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;
    }

    /// <summary>
    /// Calculates the movement path from a selected unit to the cursor's highlighted tile.
    /// </summary>
    public void CalculateUnitPath()
    {
        //If there is a selected unit,
        //And its movement range contains a node that the cursor is highlighting...
        if (unitMovement.selectedUnit != null &&
            unitMovement.selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected &&
            unitMovement.selectedUnitMoveRange.Contains(mapManager.graph[cursorX, cursorZ]))
        {
            //And if that highlighted tile is not the selected unit's current tile...
            if (cursorX != unitMovement.selectedUnit.GetComponent<Unit>().tileX ||
                cursorZ != unitMovement.selectedUnit.GetComponent<Unit>().tileZ)
            {
                //And if there isn't already a current path to another tile, or another unit that has been selected to move...
                if (!currentPathExists && unitMovement.selectedUnit.GetComponent<Unit>().movementQueue.Count == 0)
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
                                GameObject quad = quadUIUnitPath[nodeX, nodeZ];
                                quad.GetComponent<Renderer>().material = uICursor;
                            }
                            //Draw the movement path to the cursor position.
                            else if (i != 0 && (i + 1) != currentPathToCursor.Count)
                                DrawUnitPath(nodeX, nodeZ, i);
                            //Draw the arrow at the end of the movement path.
                            else if (i == currentPathToCursor.Count - 1)
                                DrawUnitPathArrow(nodeX, nodeZ, i);

                            //And turn on the renderers for each quad in the movement path.
                            quadUIUnitPath[nodeX, nodeZ].GetComponent<Renderer>().enabled = true;
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
                            quadUIUnitPath[nodeX, nodeZ].GetComponent<Renderer>().enabled = false;
                        }
                    }
                    //There is not a path to the cursor.
                    currentPathExists = false;
                }
            }
            //Otherwise, if the cursor's highlighted tile is the same as the selected unit's current tile...
            else if (cursorX == unitMovement.selectedUnit.GetComponent<Unit>().tileX &&
                cursorZ == unitMovement.selectedUnit.GetComponent<Unit>().tileZ)
            {
                //Disable the quads displaying a movement path.
                DisableQuadUIUnitPath();
                currentPathExists = false;
            }
        }
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
            return new Vector2();
    }

    #endregion


    #region Draw Map UI

    /// <summary>
    /// Turns on the quad for the tile that the cursor is currently casting to.
    /// </summary>
    /// <param name="tile">The tile that the cursor is currently casting to.</param>
    private void HighlightTile(GameObject tile)
    {
        //If the cursor is casting to a tile, get that tile's X and Z positions on the map grid.
        if (gameManager.hit.transform.CompareTag("Tile"))
        {
            highlightedTileX = tile.GetComponent<Tile>().tileX;
            highlightedTileZ = tile.GetComponent<Tile>().tileZ;
        }
        //If the cursor is casting to a unit, get that unit's X and Z positions on the map grid.
        else if (gameManager.hit.transform.CompareTag("Unit"))
        {
            highlightedTileX = tile.GetComponent<Unit>().tileX;
            highlightedTileZ = tile.GetComponent<Unit>().tileZ;
        }

        cursorX = highlightedTileX;
        cursorZ = highlightedTileZ;

        //Turn on the renderer for the quad that is being highlighted by the cursor.
        quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = true;

        //Set the tile, or the tile occupied by a unit, that the cursor is casting to as highlighted.
        if (gameManager.hit.transform.CompareTag("Tile"))
            highlightedTile = tile;
        else if (gameManager.hit.transform.CompareTag("Unit"))
            highlightedTile = tile.GetComponent<Unit>().occupiedTile;
    }

    /// <summary>
    /// Turns on the quads in the 2D map grid array that represent the selected unit's movement range.
    /// </summary>
    /// <param name="movementRange">A container of nodes that the selected unit can move to from its current position.</param>
    public void HighlightMovementRange(HashSet<Node> movementRange)
    {
        //For each node in the selected unit's movement range, turn on the highlight quads.
        foreach (Node node in movementRange)
        {
            quadUIUnitRange[node.x, node.z].GetComponent<Renderer>().material = uIMatBlue;
            quadUIUnitRange[node.x, node.z].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    /// <summary>
    /// Turns on the quads in the 2D map grid array that represent the selected unit's attackable nodes.
    /// </summary>
    /// <param name="attackableEnemies"></param>
    public void HighlightAttackRange(HashSet<Node> attackableEnemies)
    {
        //For each node in the selected unit's attack range, turn on the highlight quads.
        foreach (Node node in attackableEnemies)
        {
            quadUIUnitRange[node.x, node.z].GetComponent<Renderer>().material = uIMatRed;
            quadUIUnitRange[node.x, node.z].GetComponent<MeshRenderer>().enabled = true;
        }
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
        GameObject quad = quadUIUnitPath[nodeX, nodeZ];
        quad.GetComponent<Transform>().rotation = Quaternion.Euler(rotX, 0, rotZ);
        quad.GetComponent<Renderer>().material = mat;
        quad.GetComponent<Renderer>().enabled = true;
    }

    #endregion


    #region Disable Map UI

    /// <summary>
    /// Disables all renderers for highlighted tiles (i.e. quads) in a unit's movement and attack ranges, so they can be recalculated and enabled again.
    /// </summary>
    public void DisableQuadUIUnitRange()
    {
        //For each highlighted quad in the unit's movement and attack range, turn it off.
        foreach (GameObject quad in quadUIUnitRange)
        {
            if (quad.GetComponent<Renderer>().enabled == true)
                quad.GetComponent<Renderer>().enabled = false;
        }
    }

    /// <summary>
    /// Disables all renderers for highlighted tiles (i.e. quads) in the unit's movement path, so they can be recalculated and enabled again.
    /// </summary>
    public void DisableQuadUIUnitPath()
    {
        //For each highlighted quad in the unit's movement path, turn it off.
        foreach (GameObject quad in quadUIUnitPath)
        {
            if (quad.GetComponent<Renderer>().enabled == true)
                quad.GetComponent<Renderer>().enabled = false;
        }
    }

    #endregion

    #endregion
}
