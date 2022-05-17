using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The system controlling the generation and instantiation of map grids, and their pathfinding data.
/// </summary>
public class MapManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The GameManager script.")]
    [SerializeField]
    private GameManager gameManager;
    [Tooltip("The BattleManager script.")]
    [SerializeField]
    private BattleManager battleManager;

    [Header("Map Data")]
    [Tooltip("The different kinds of tiles that can generate on the map, e.g. grass, forest, mountain, etc.")]
    [SerializeField]
    public TileType[] tileTypes;
    [Tooltip("The number of tiles generated on the map grid's X axis.")]
    [SerializeField]
    public int mapSizeX = 10;
    [Tooltip("The number of tiles generated on the map grid's Z axis.")]
    [SerializeField]
    public int mapSizeZ = 10;
    [Tooltip("A 2D array that represents the number of tiles in the map grid.")]
    [HideInInspector]
    public int[,] tiles;
    [Tooltip(" A 2D array of nodes that represents all of the map grid tiles that a unit can create a path to.")]
    [HideInInspector]
    public Node[,] graph;
    [Tooltip("A list of nodes that represent a path from one map grid position to another.")]
    [NonSerialized]
    private List<Node> currentPath = null;

    [Header("Map Game Objects")]
    [Tooltip("The units that are currently on the map. This is used to set tiles that are currently occupied.")]
    [SerializeField]
    private GameObject mapUnits;
    [Tooltip("A 2D array containing the list of tile game objects on the map.")]
    [HideInInspector]
    public GameObject[,] mapTiles;

    [Header("Selected Unit")]
    [Tooltip("The current unit that has been clicked on.")]
    [HideInInspector]
    public GameObject selectedUnit;
    [Tooltip("A container of nodes representing a selected unit's movement range, based on the unit's move speed and any attackable enemies within this range.")]
    [HideInInspector]
    public HashSet<Node> selectedUnitMoveRange;
    //[SerializeField]
    //private HashSet<Node> selectedUnitMoveRangeTotal;
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

    [Header("Quad UI")]
    [Tooltip("A 2D array of quads that are activated to highlight a unit's movement range and attackable enemies.")]
    [SerializeField]
    private GameObject[,] quadUIUnitRange;
    [Tooltip("A 2D array of quads that are activated to highlight a unit's proposed movement path.")]
    [HideInInspector]
    public GameObject[,] quadUIUnitPath;
    [Tooltip("A 2D array of quads that are activated to highlight a single tile that the cursor is raycasting to.")]
    [HideInInspector]
    public GameObject[,] quadUICursor;

    [Header("Map UI")]
    [Tooltip("The quad prefab that is instantiated to highlight a unit's movement range and attackable enemies.")]
    [SerializeField]
    private GameObject mapUIUnitRange;
    [Tooltip("The quad prefab that is instantiated to highlight a unit's proposed movement path.")]
    [SerializeField]
    private GameObject mapUIUnitPath;
    [Tooltip("The quad prefab that is instantiated to highlight a single tile that the cursor is raycasting to.")]
    [SerializeField]
    private GameObject mapUICursor;

    [Header("UI Materials")]
    //[SerializeField]
    //private Material uIMatGreen;
    [Tooltip("Material applied to quads when displaying a unit's attackable tiles.")]
    [SerializeField]
    private Material uIMatRed;
    [Tooltip("Material applied to quads when displaying a unit's movement range.")]
    [SerializeField]
    private Material uIMatBlue;

    [Header("Containers")]
    [Tooltip("A container for the instantiated map grid tiles, used to keep the hierarchy clean and readable.")]
    [SerializeField]
    private GameObject tileContainer;
    [Tooltip("A container for the instantiated movement and attack range quads, used to keep the hierarchy clean and readable.")]
    [SerializeField]
    private GameObject quadUIUnitRangeContainer;
    [Tooltip("A container for the instantiated movement path quads, used to keep the hierarchy clean and readable.")]
    [SerializeField]
    private GameObject quadUIUnitPathContainer;
    [Tooltip("A container for the instantiated cursor highlight quads, used to keep the hierarchy clean and readable.")]
    [SerializeField]
    private GameObject quadUICursorContainer;

    #endregion


    #region Unity Functions

    private void Start()
    {
        //Set up an array of map grid tiles and assigns an integer value for each index in the array.
        GenerateMap();
        //Calculate the map grid positions of all of the Nodes on the map, and creates a list of their neighbours.
        GeneratePathGraph();
        //Create a map of tile prefabs in the scene.
        InstantiateMap();
        //Check units' tile positions and set their tiles as occupied.
        SetTileOccupied();
    }

    private void Update()
    {
        //On left-mouse click, select units and/or tiles.
        if (Input.GetMouseButtonDown(0))
        {
            Select();
            //Debug.Log("Tile Clicked: " + gameManager.highlightedTile.GetComponent<Tile>().tileX + ", " + gameManager.highlightedTile.GetComponent<Tile>().tileZ);
        }

        //On right-mouse click, deselect units.
        if (Input.GetMouseButtonDown(1))
        {
            //If there is currently a selected unit...
            if (selectedUnit != null)
            {
                //And the unit has not yet finished its turn...
                if (selectedUnit.GetComponent<Unit>().movementQueue.Count == 0
                    && selectedUnit.GetComponent<Unit>().combatQueue.Count == 0
                    && selectedUnit.GetComponent<Unit>().movementState != MovementState.Waiting)
                {
                    //sound.Play();
                    //selectedUnit.GetComponent<Unit>().PlayIdleAnim();

                    //Deselect the unit.
                    DeselectUnit();
                }
                else if (selectedUnit.GetComponent<Unit>().movementQueue.Count == 1)
                    selectedUnit.GetComponent<Unit>().lerpSpeed = 0.5f;
            }
        }
    }

    #endregion


    #region Custom Functions

    #region Map Generation

    /// <summary>
    /// Sets up an array of map grid tiles and assigns an integer value for each index in the array.
    /// </summary>
    private void GenerateMap()
    {
        //Set up the array with the number of map tiles allocated on the X and Z axes.
        tiles = new int[mapSizeX, mapSizeZ];

        //Initialise map tiles.
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                //Set each index in the tiles array to 0 (i.e. grass).
                tiles[x, z] = 0;
            }
        }

        #region Hard-Coded Terrain
        //Obviously this isn't the best way to do this.

        //1 = Forest
        //2 = Mountain
        //3 = Base

        //Forest
        tiles[0, 1] = 1;
        tiles[0, 7] = 1;
        tiles[1, 0] = 1;
        tiles[1, 7] = 1;
        tiles[1, 8] = 1;
        tiles[2, 8] = 1;
        tiles[2, 9] = 1;
        tiles[3, 0] = 1;
        tiles[3, 4] = 1;
        tiles[4, 2] = 1;
        tiles[4, 6] = 1;
        tiles[5, 0] = 1;
        tiles[5, 3] = 1;
        tiles[5, 5] = 1;
        tiles[5, 7] = 1;
        tiles[6, 0] = 1;
        tiles[6, 3] = 1;
        tiles[6, 6] = 1;
        tiles[7, 8] = 1;
        tiles[7, 9] = 1;
        tiles[8, 0] = 1;
        tiles[9, 0] = 1;

        //Mountain Range
        tiles[0, 0] = 2;
        tiles[0, 8] = 2;
        tiles[0, 9] = 2;
        tiles[1, 9] = 2;
        tiles[4, 0] = 2;
        tiles[4, 7] = 2;
        tiles[5, 2] = 2;
        tiles[5, 6] = 2;
        tiles[6, 9] = 2;
        tiles[7, 0] = 2;
        tiles[8, 9] = 2;

        //Base
        tiles[9, 4] = 3;

        #endregion
    }

    /// <summary>
    /// Calculates the map grid positions of all of the Nodes on the map, and creates a list of their neighbours.
    /// </summary>
    private void GeneratePathGraph()
    {
        //Initialise an array of Nodes that is the same size as the map grid.
        graph = new Node[mapSizeX, mapSizeZ];

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                //Initialise a Node into each index of the array, for each grid position on the map.
                graph[x, z] = new Node();
                //Set the map grid position of each Node in the array to be the same as its index in the array.
                graph[x, z].x = x;
                graph[x, z].z = z;
            }
        }

        //Now that all the Nodes have been mapped into the array, calculate their neighbours.
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {

                #region 4-way Connected Nodes

                //Add the left neighbouring node.
                if (x > 0)
                    graph[x, z].neighbours.Add(graph[x - 1, z]);
                //Add the right neighbouring node.
                if (x < mapSizeX - 1)
                    graph[x, z].neighbours.Add(graph[x + 1, z]);
                //Add the downwards neighbouring node.
                if (z > 0)
                    graph[x, z].neighbours.Add(graph[x, z - 1]);
                //Add the upwards neighbouring node.
                if (z < mapSizeZ - 1)
                    graph[x, z].neighbours.Add(graph[x, z + 1]);

                #endregion

                #region 8-way Connected Nodes (Allowing Diagonal Movement)

                //if (x > 0)
                //{
                //    //Left
                //    graph[x, z].neighbours.Add(graph[x - 1, z]);
                //    if (z > 0)
                //        //Diagonal left-down
                //        graph[x, z].neighbours.Add(graph[x - 1, z - 1]);
                //    if (z < mapSizeZ - 1)
                //        //Diagonal left-up
                //        graph[x, z].neighbours.Add(graph[x - 1, z + 1]);
                //}

                //if (x < mapSizeX - 1)
                //{
                //    //Right
                //    graph[x, z].neighbours.Add(graph[x + 1, z]);
                //    if (z > 0)
                //        //Diagonal right-down
                //        graph[x, z].neighbours.Add(graph[x + 1, z - 1]);
                //    if (z < mapSizeZ - 1)
                //        //Diagonal right-up
                //        graph[x, z].neighbours.Add(graph[x + 1, z + 1]);
                //}

                //if (z > 0)
                //    //Down
                //    graph[x, z].neighbours.Add(graph[x, z - 1]);

                //if (z < mapSizeZ - 1)
                //    //Up
                //    graph[x, z].neighbours.Add(graph[x, z + 1]);

                #endregion
            }
        }
    }

    /// <summary>
    /// Creates a map of tile prefabs in the scene.
    /// </summary>
    private void InstantiateMap()
    {
        //Create a 2D array of map tile game objects.
        //Create three 2D arrays of quads that are used to highlight tiles for specific purposes.
        mapTiles = new GameObject[mapSizeX, mapSizeZ];
        quadUIUnitRange = new GameObject[mapSizeX, mapSizeZ];
        quadUIUnitPath = new GameObject[mapSizeX, mapSizeZ];
        quadUICursor = new GameObject[mapSizeX, mapSizeZ];

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                //Find the tile type (grass, forest, etc.) for each tile in the map grid, and copy those types into local variables. 
                TileType tt = tileTypes[tiles[x, z]];

                //Instantiate the associated prefab for each tile type and quad in the map grid.
                //Convert the tile/quad's map grid position into a Vector3 to instantiate it at the correct position in worldspace.
                GameObject newTile = Instantiate(tt.tilePrefab, new Vector3(x, 0, z), Quaternion.identity);
                GameObject gridUI = Instantiate(mapUIUnitRange, new Vector3(x, 0.501f, z), Quaternion.Euler(90f, 0, 0));
                GameObject gridUIUnitMovement = Instantiate(mapUIUnitPath, new Vector3(x, 0.502f, z), Quaternion.Euler(90f, 0, 0));
                GameObject gridUICursor = Instantiate(mapUICursor, new Vector3(x, 0.503f, z), Quaternion.Euler(90f, 0, 0));

                //Get the MonoBehaviour script attached to an instantiated tile.
                Tile clickableTile = newTile.GetComponent<Tile>();
                //For each instantiated tile, assign its map grid position in the 2D array.
                clickableTile.tileX = x;
                clickableTile.tileZ = z;
                //For each instantiated tile, assign the map system that is controlling it.
                clickableTile.map = this;

                //Store the instantiated map tiles and quads in parent game objects, to keep the hierarchy clean.
                newTile.transform.SetParent(tileContainer.transform);
                gridUI.transform.SetParent(quadUIUnitRangeContainer.transform);
                gridUIUnitMovement.transform.SetParent(quadUIUnitPathContainer.transform);
                gridUICursor.transform.SetParent(quadUICursorContainer.transform);

                //Store each instantiated tile and quad with its corresponding position in the 2D array.
                mapTiles[x, z] = newTile;
                quadUIUnitRange[x, z] = gridUI;
                quadUIUnitPath[x, z] = gridUIUnitMovement;
                quadUICursor[x, z] = gridUICursor;
            }
        }
    }

    #endregion


    #region Pathfinding

    /// <summary>
    /// This function represents Dijkstra's algorithm and is used to generate a path from a source tile on the map grid to a target tile.
    /// </summary>
    /// <param name="x">The destination tile's position on the map grid's X axis, that a unit is generating a path to.</param>
    /// <param name="z">The destination tile's position on the map grid's Z axis, that a unit is generating a path to.</param>
    public List<Node> GeneratePathTo(int x, int z)
    {
        //If a unit's tile position is the same as the tile it is generating a path to, there is no need to generate a path.
        if (selectedUnit.GetComponent<Unit>().tileX == x &&
            selectedUnit.GetComponent<Unit>().tileZ == z)
        {
            currentPath = new List<Node>();
            return currentPath;
        }

        //If the unit cannot enter the tile it is generating a path to, exit the function.
        if (UnitCanEnterTile(x, z) == false)
        {
            return null;
        }

        //Clear the unit's previous path.
        //selectedUnit.GetComponent<Unit>().path = null;
        currentPath = null;

        //This dictionary stores all of the nodes on the map grid and their distance (as a float) from the unit's current node position.
        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        //This dictionary stores nodes on the shortest path from the source node to the target node.
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();

        //Create a list of unchecked or unvisited Nodes.
        //This is a priority queue of nodes to be checked, when generating a path in Dijkstra's algorithm.
        List<Node> nodeQueue = new List<Node>();

        //Store the unit's current position on the map grid in a local variable.
        Node source = graph[
            selectedUnit.GetComponent<Unit>().tileX,
            selectedUnit.GetComponent<Unit>().tileZ];
        //Store the unit's destination on the map grid in a local variable.
        Node target = graph[x, z];

        //Set the distance to the unit's current position to 0.
        dist[source] = 0;
        //Set the source node in the path to null.
        prev[source] = null;

        //Every node, other than the unit's current position on the map grid, is initialised to have infinite distance.
        foreach (Node node in graph)
        {
            if (node != source)
            {
                dist[node] = Mathf.Infinity;
                prev[node] = null;
            }

            //Every node on the map grid is added into a queue of unchecked nodes.
            nodeQueue.Add(node);
        }

        //While there are unchecked nodes in the queue...
        while(nodeQueue.Count > 0)
        {
            //Store a temporary node with the smallest distance.
            Node tempNode = null;

            //For each unchecked node in the queue...
            foreach (Node uncheckedNode in nodeQueue)
            {
                //If the temporary node is empty or the distance to the unchecked node is less than the distance to the temporary node...
                if (tempNode == null || dist[uncheckedNode] < dist[tempNode])
                {
                    //Copy the unchecked node into the temporary node.
                    tempNode = uncheckedNode;
                }
            }

            //If the temporary node is the same as the unit's destination node on the map grid...
            if (tempNode == target)
            {
                //Exit the while loop.
                break; 
            }

            //Now that the unchecked node has been checked and it is not the target, remove the temporary node from the list of unchecked nodes.
            nodeQueue.Remove(tempNode);

            //For each node that is neighbouring the temporary node...
            foreach(Node neighbourNode in tempNode.neighbours)
            {
                //float alt = dist[u] + u.DistanceTo(v);

                //Calculate the distance to enter each neighbouring node,
                //based on that neighbouring node's distance from the unit's current node position and the cost to enter that node.
                float tempDist = dist[tempNode] + CostToEnterTile(neighbourNode.x, neighbourNode.z);

                //If the distance to the temporary node is less than the distance to each neighbouring node...
                if (tempDist < dist[neighbourNode])
                {
                    //Record the temporary distance to the neighbouring node into the array.
                    dist[neighbourNode] = tempDist;
                    //Set the temporary node as the node that will be moved to on the unit's path.
                    prev[neighbourNode] = tempNode;
                }
            }
        }

        //At this stage, we either found the shortest route to our target,
        //Or there is no route at all to our target.
        if (prev[target] == null)
        {
            //If there is no route between the source and target, exit the function.
            return null;
        }

        currentPath = new List<Node>();

        //Copy the target node into a local variable.
        Node curr = target;

        //While the current target node is not empty, step through the "prev" chain and add each node to the unit's path.
        while (curr != null)
        {
            currentPath.Add(curr);

            curr = prev[curr];
        }

        //Finally, the unit's current path is inverted.
        //This is because the current path describes a route from the unit's target to its source, and this logically needs to be reversed.
        currentPath.Reverse();

        //Copy the path we calculated into the unit's script.
        return currentPath;
    }

    /// <summary>
    /// Gets the cost for a unit to move from its current map grid position to its neighbouring, target map grid position.
    /// </summary>
    /// <param name="x">The unit's target position on the map grid's X axis.</param>
    /// <param name="z">The unit's target position on the map grid's Z axis.</param>
    /// <returns></returns>
    private float CostToEnterTile(int x, int z)
    {
        //Check if the target tile is walkable.
        if (UnitCanEnterTile(x, z) == false)
            //If it is not, return a cost of infinity and exit the function.
            return Mathf.Infinity;

        //Find the target tile's type (grass, forest, etc.) and copy it into a local variable.
        TileType tile = tileTypes[tiles[x, z]];

        //Get the target tile's cost and copy it into a local variable.
        float cost = tile.movementCost;

        return cost;
    }

    /// <summary>
    /// Checks if a tile on the map grid is walkable.
    /// </summary>
    /// <param name="x">The unit's target position on the map grid's X axis.</param>
    /// <param name="z">The unit's target position on the map grid's Z axis.</param>
    /// <returns></returns>
    public bool UnitCanEnterTile(int x, int z)
    {
        //Check a unit's type against terrain flags here if necessary.

        //If the target tile is currently occupied by an enemy unit...
        if (mapTiles[x, z].GetComponent<Tile>().unitOccupyingTile != null &&
            mapTiles[x, z].GetComponent<Tile>().unitOccupyingTile.GetComponent<Unit>().teamNumber !=
                selectedUnit.GetComponent<Unit>().teamNumber)
            //The unit cannot enter the tile.
            return false;
        //Otherwise, return the target tile's walkability boolean.
        return tileTypes[tiles[x, z]].isWalkable;
    }

    #endregion


    #region Movement Range

    /// <summary>
    /// Calculates the map grid nodes that need to be highlighted when a unit's movement range is displayed.
    /// </summary>
    private void MovementRange()
    {
        //A container of nodes representing the tiles that a unit can move to.
        HashSet<Node> movementRange = new HashSet<Node>();
        //A container of nodes representing the tiles occupied by enemies in the selected unit's movement range.
        HashSet<Node> enemiesInRange = new HashSet<Node>();
        ////A container of nodes representing the tiles occupied by enemies in a unit's movement range.
        //HashSet<Node> enemiesInRange = new HashSet<Node>();

        //Store the selected unit's start position on the map grid in a local variable.
        Node startNode = graph[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ];

        //Store the selected unit's attack range and move speed in local variables.
        int attackRange = selectedUnit.GetComponent<Unit>().attackRange;
        int movespeed = selectedUnit.GetComponent<Unit>().moveSpeed;

        //Calculate the nodes that exist in the selected unit's movement and attack ranges.
        movementRange = GetMovementRange(movementRange, movespeed, startNode);
        enemiesInRange = GetEnemiesInRange(movementRange, enemiesInRange, attackRange, startNode);

        //If the nodes in the selected unit's attack range are occupied...
        //foreach (Node node in attackableEnemies)
        //    if (mapTiles[node.x, node.z].GetComponent<ClickableTile>().unitOccupyingTile != null)
        //    {
        //        GameObject unitOccupyingSelectedTile = mapTiles[node.x, node.z].GetComponent<ClickableTile>().unitOccupyingTile;

        //        //And the units occupying those tiles are not on the current player's team...
        //        if (unitOccupyingSelectedTile.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber)
        //            //Add those nodes to the container of enemies in a unit's movement range.
        //            enemiesInRange.Add(node);
        //    }

        //Finally, highlight the selected unit's movement range and attackable enemies.
        HighlightAttackRange(enemiesInRange);
        HighlightMovementRange(movementRange);

        selectedUnitMoveRange = movementRange;

        //selectedUnitMoveRangeTotal = GetTotalRange(movementRange, attackableEnemies);
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
        //Create a 2D array containing the costs for units to enter all of the tiles on the map grid.
        float[,] cost = new float[mapSizeX, mapSizeZ];

        //A container, and temporary container, of nodes that are highlighted in a selected unit's movement range.
        HashSet<Node> uIHighlight = new HashSet<Node>();
        HashSet<Node> tempUIHighlight = new HashSet<Node>();

        //Add the selected unit's start node into the container of nodes that the unit can move to.
        movementRange.Add(startNode);

        //For each of the start node's neighbours...
        foreach (Node node in startNode.neighbours)
        {
            //Add their costs to the local 2D array.
            cost[node.x, node.z] = CostToEnterTile(node.x, node.z);

            //If the cost to enter the neighbouring nodes is less than or equal to the unit's move speed...
            if (movespeed - cost[node.x, node.z] >= 0)
                //Add those neighbouring nodes to the container of nodes to be highlighted.
                uIHighlight.Add(node);
        }

        //Insert those highlighted nodes into the unit's movement range.
        movementRange.UnionWith(uIHighlight);

        while (uIHighlight.Count != 0)
        {
            //For all of the nodes neighbouring the nodes that have been highlighted...
            foreach (Node node in uIHighlight)
                foreach (Node neighbour in node.neighbours)
                    //If those neighbours have not already been added to the unit's movement range...
                    if (!movementRange.Contains(neighbour))
                    {
                        //Calculate the cost to move from those nodes to their neighbouring nodes.
                        cost[neighbour.x, neighbour.z] = CostToEnterTile(neighbour.x, neighbour.z) + cost[node.x, node.z];

                        //If the cost to enter the neighbouring nodes is less than or equal to the unit's move speed...
                        if (movespeed - cost[neighbour.x, neighbour.z] >= 0)
                            //Add those neighbouring nodes to the container of nodes to be highlighted.
                            tempUIHighlight.Add(neighbour);
                    }

            //Store the hightlighted nodes in the selected unit's movement range.
            uIHighlight = tempUIHighlight;
            movementRange.UnionWith(uIHighlight);
            //Refresh the temporary container of highlighted nodes.
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
        //A container, and temporary container, of nodes that neighbour other nodes.
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash;
        //A container of nodes that represent the enemies that are within the selected unit's movement range.
        HashSet<Node> enemiesInRangeHash = new HashSet<Node>();

        //For all of the nodes in the selected unit's movement range...
        foreach (Node node in movementRange)
        {
            //Add those nodes into the container of neighbouring nodes.
            neighbourHash = new HashSet<Node>();
            neighbourHash.Add(node);

            //For all of the neighbouring nodes in the selected unit's attack range...
            for (int i = 0; i < attackRange; i++)
            {
                foreach (Node neighbourNode in neighbourHash)
                    foreach (Node tempNeighbourNode in neighbourNode.neighbours)
                        tempNeighbourHash.Add(tempNeighbourNode);

                //Store those neighbouring nodes.
                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();

                //Continue to build a container of nodes neighbouring other nodes,
                //Until the for loop is the same as the selected unit's attack range. 
                if (i < attackRange - 1)
                    enemiesInRangeHash.UnionWith(neighbourHash);
            }

            //Remove the enemies in the selected unit's range from the container of neighbouring nodes.
            neighbourHash.ExceptWith(enemiesInRangeHash);
            enemiesInRangeHash = new HashSet<Node>();
            //Add the remaining neighbouring nodes into the hash of enemies within the selected unit's range.
            enemiesInRange.UnionWith(neighbourHash);
        }

        //Remove the selected unit's start node from the container of enemies.
        enemiesInRange.Remove(startNode);

        return enemiesInRange;
    }

    /// <summary>
    /// Returns a container of nodes that represent the attackable enemies in the selected unit's attack range.
    /// </summary>
    /// <returns></returns>
    private HashSet<Node> GetEnemiesAttackable()
    {
        //A container, and temporary container, of nodes that neighbour other nodes.
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        //A container of nodes that have been checked for being within the unit's attack range.
        HashSet<Node> checkedNodes = new HashSet<Node>();

        //Store the selected unit's start position on the map grid in a local variable.
        Node startNode = graph[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ];
        //Store the selected unit's attack range in a local variable.
        int attackRange = selectedUnit.GetComponent<Unit>().attackRange;

        //Add the selected unit's start node into the container of nodes that need to be checked.
        neighbourHash.Add(startNode);

        //For all of the neighbouring nodes in the selected unit's attack range...
        for (int i = 0; i < attackRange; i++)
        {
            foreach (Node neighbourNode in neighbourHash)
                foreach (Node tempNeighbourNode in neighbourNode.neighbours)
                    tempNeighbourHash.Add(tempNeighbourNode);

            //Store those neighbouring nodes.
            neighbourHash = tempNeighbourHash;
            tempNeighbourHash = new HashSet<Node>();

            //Continue to build a container of nodes neighbouring other nodes,
            //Until the for loop is the same as the selected unit's attack range. 
            if (i < attackRange - 1)
                checkedNodes.UnionWith(neighbourHash);
        }
        //Remove the checked nodes in the selected unit's range from the container of neighbouring nodes.
        neighbourHash.ExceptWith(checkedNodes);
        //Remove the selected unit's start node from the container of neighbouring nodes.
        neighbourHash.Remove(startNode);
        return neighbourHash;
    }

    //private HashSet<Node> GetTotalRange(HashSet<Node> movementRange, HashSet<Node> attackableEnemies)
    //{
    //    HashSet<Node> unionTiles = new HashSet<Node>();

    //    unionTiles.UnionWith(movementRange);
    //    unionTiles.UnionWith(attackableEnemies);

    //    return unionTiles;
    //}

    /// <summary>
    /// Returns the selected unit's currently occupied tile as a hashset.
    /// </summary>
    /// <returns></returns>
    private HashSet<Node> GetOccupiedTile()
    {
        HashSet<Node> occupiedTile = new HashSet<Node>();
        //Add the selected unit's X and Z positions on the map grid to a hashset and return it.
        occupiedTile.Add(graph[
            selectedUnit.GetComponent<Unit>().tileX,
            selectedUnit.GetComponent<Unit>().tileZ]);
        return occupiedTile;
    }

    /// <summary>
    /// Turns on the quads in the 2D map grid array that represent the selected unit's movement range.
    /// </summary>
    /// <param name="movementRange">A container of nodes that the selected unit can move to from its current position.</param>
    private void HighlightMovementRange(HashSet<Node> movementRange)
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
    private void HighlightAttackRange(HashSet<Node> attackableEnemies)
    {
        //For each node in the selected unit's attack range, turn on the highlight quads.
        foreach (Node node in attackableEnemies)
        {
            quadUIUnitRange[node.x, node.z].GetComponent<Renderer>().material = uIMatRed;
            quadUIUnitRange[node.x, node.z].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    /// <summary>
    /// Disables all renderers for highlighted tiles (i.e. quads) in a unit's movement and attack ranges, so they can be recalculated and enabled again.
    /// </summary>
    private void DisableQuadUIUnitRange()
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


    #region Unit Movement

    /// <summary>
    /// This function handles all selections made with the left mouse button to any tile or unit.
    /// </summary>
    private void Select()
    {
        //If there was no unit selected...
        if (selectedUnit == null)
        {
            //And if the cursor is currently highlighting a tile that is occupied by a unit...
            if (unitSelected == false
                && gameManager.highlightedTile != null
                && gameManager.highlightedTile.GetComponent<Tile>().unitOccupyingTile != null)
            {
                //Store that unit in a temporary game object.
                GameObject tempSelectedUnit = gameManager.highlightedTile.GetComponent<Tile>().unitOccupyingTile;

                //If that unit is unselected and it is on the current player's team...
                if (tempSelectedUnit.GetComponent<Unit>().movementState == MovementState.Unselected
                    && tempSelectedUnit.GetComponent<Unit>().teamNumber == gameManager.currentTeam)
                {
                    //Turn off any quads that are highlighted.
                    DisableQuadUIUnitRange();

                    //The unit is now selected.
                    selectedUnit = tempSelectedUnit;
                    selectedUnit.GetComponent<Unit>().map = this;
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Selected;
                    unitSelected = true;

                    //selectedUnit.GetComponent<Unit>().animator.SetTrigger("Selected");

                    //Highlight the unit's movement range.
                    MovementRange();
                }
            }
        }
        //If a unit was already selected, and it is on the player's team, we want to set the unit up to move.
        else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected
            && selectedUnit.GetComponent<Unit>().movementQueue.Count == 0
            && CheckTileInMoveRange())
        {
            //Store the unit's previous tile position.
            unitSelectedPrevX = selectedUnit.GetComponent<Unit>().tileX;
            unitSelectedPrevZ = selectedUnit.GetComponent<Unit>().tileZ;
            unitSelectedPrevTile = selectedUnit.GetComponent<Unit>().occupiedTile;

            //sound.Play();
            //selectedUnit.GetComponent<Unit>().PlayWalkingAnim();

            //Move the unit to the next tile in their path.
            selectedUnit.GetComponent<Unit>().AdvanceNextTile();
            StartCoroutine(FinaliseMovement());
        }
        //If a unit has already finished its move, we want to finish the unit's turn.
        else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Moved)
        {
            WaitOrAttack();
        }
    }

    /// <summary>
    /// Deselects a unit that was previously selected, with the right mouse button.
    /// </summary>
    private void DeselectUnit()
    {
        //If a unit is currently selected...
        if (selectedUnit != null)
        {
            //Turn off any quads that are highlighted.
            DisableQuadUIUnitRange();
            DisableQuadUIUnitPath();

            if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected)
            {
                //Reset the unit's movement state to unselected, and deselect it.
                selectedUnit.GetComponent<Unit>().movementState = MovementState.Unselected;
                selectedUnit = null;
                unitSelected = false;
            }
            //Otherwise, if the unit was waiting after moving/attacking...
            else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Waiting)
            {
                //Deselect the unit.
                selectedUnit = null;
                unitSelected = false;
            }
            //In every other instance, return the unit to its previous map grid position.
            else
            {
                //Set the selected unit's current map grid position as unoccupied.
                mapTiles[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ]
                    .GetComponent<Tile>().unitOccupyingTile = null;
                //Set the selected unit's previous map grid position as the occupied tile.
                mapTiles[unitSelectedPrevX, unitSelectedPrevZ].GetComponent<Tile>().unitOccupyingTile = selectedUnit;

                //Return the unit to its previous map grid position.
                selectedUnit.GetComponent<Unit>().tileX = unitSelectedPrevX;
                selectedUnit.GetComponent<Unit>().tileZ = unitSelectedPrevZ;
                selectedUnit.GetComponent<Unit>().occupiedTile = unitSelectedPrevTile;
                selectedUnit.transform.position = GetTileWorldSpace(unitSelectedPrevX, unitSelectedPrevZ);

                //Finally, deselect the unit.
                selectedUnit.GetComponent<Unit>().movementState = MovementState.Unselected;
                selectedUnit = null;
                unitSelected = false;
            }
        }
    }

    /// <summary>
    /// Returns true if the player clicks on a tile that is in the selected unit's movement range.
    /// </summary>
    /// <returns></returns>
    private bool CheckTileInMoveRange()
    {
        //Cast a ray from the cursor's position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            //If the cursor is casting on to a tile...
            if (hit.transform.gameObject.CompareTag("Tile"))
            {
                //Get the clicked tile's X and Z map grid positions. 
                int clickedTileX = hit.transform.GetComponent<Tile>().tileX;
                int clickedTileZ = hit.transform.GetComponent<Tile>().tileZ;
                //Look up the clicked tile's node in the 2D graph array.
                Node clickedNode = graph[clickedTileX, clickedTileZ];

                //If the clicked node is in the selected unit's movement range,
                //And the node's tile is not occupied by a different unit... 
                if (selectedUnitMoveRange.Contains(clickedNode) &&
                    (hit.transform.gameObject.GetComponent<Tile>().unitOccupyingTile == null ||
                        hit.transform.gameObject.GetComponent<Tile>().unitOccupyingTile == selectedUnit))
                {
                    //Start generating a path for the unit and return true.
                    selectedUnit.GetComponent<Unit>().path = GeneratePathTo(clickedTileX, clickedTileZ);
                    return true;
                }
            }
            //If the cursor is casting onto a unit...
            else if (hit.transform.gameObject.CompareTag("Unit"))
            {
                //If the player clicks on a unit from the enemy team, return false.
                if (hit.transform.parent.GetComponent<Unit>().teamNumber !=
                    selectedUnit.GetComponent<Unit>().teamNumber)
                    return false;
                //If the unit is on the player's team, start generating a path for the unit and return true.
                else if (hit.transform.gameObject == selectedUnit)
                {
                    selectedUnit.GetComponent<Unit>().path = GeneratePathTo(selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ);
                    return true;
                }
            }
        }
        //If none of the above conditions are met, return false.
        return false;
    }

    //private void MoveUnit()
    //{
    //    if (selectedUnit != null)
    //        selectedUnit.GetComponent<Unit>().AdvanceNextTile();
    //}

    /// <summary>
    /// Disables highlight quads before allowing the player to choose to attack or wait.
    /// </summary>
    /// <returns></returns>
    private IEnumerator FinaliseMovement()
    {
        DisableQuadUIUnitRange();
        DisableQuadUIUnitPath();

        while (selectedUnit.GetComponent<Unit>().movementQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        FinaliseMovementPos();
        //selectedUnit.GetComponent<Unit>().PlaySelectedAnim();
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

        //Change the unit's movement state to wait. 
        selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;

        //Turn off any highlighted quads.
        DisableQuadUIUnitRange();
        DisableQuadUIUnitPath();

        //Wait a quarter of a second.
        yield return new WaitForSeconds(.25f);

        //Wait for the units to stop attacking.
        while (attacker.GetComponent<Unit>().combatQueue.Count > 0)
            yield return new WaitForEndOfFrame();
        while (defender.GetComponent<Unit>().combatQueue.Count > 0)
            yield return new WaitForEndOfFrame();

        DeselectUnit();
        CheckTeamWaiting();
    }

    /// <summary>
    /// Sets the unit's tile as occupied after moving, and sets them up to attack.
    /// </summary>
    private void FinaliseMovementPos()
    {
        //Set the selected unit's tile as occupied by the selected unit.
        mapTiles[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ]
            .GetComponent<Tile>().unitOccupyingTile = selectedUnit;

        //Set the selected unit's state to moved.
        selectedUnit.GetComponent<Unit>().movementState = MovementState.Moved;

        //Highlight the selected unit's atttackable tiles.
        if (selectedUnit != null)
        {
            HighlightAttackRange(GetEnemiesAttackable());
            HighlightMovementRange(GetOccupiedTile());
        }
    }

    /// <summary>
    /// Sets all tiles that are occupied by units as occupied.
    /// </summary>
    private void SetTileOccupied()
    {
        //For each unit in each team...
        foreach (Transform team in mapUnits.transform)
        {
            foreach (Transform unit in team)
            {
                //Get the unit's X and Z map grid positions.
                int unitTileX = unit.GetComponent<Unit>().tileX;
                int unitTileZ = unit.GetComponent<Unit>().tileZ;

                //Set the unit's occupied tile as their map grid position.
                unit.GetComponent<Unit>().occupiedTile = mapTiles[unitTileX, unitTileZ];

                //Set their unit's map grid position as occupied by the unit.
                mapTiles[unitTileX, unitTileZ].GetComponent<Tile>().unitOccupyingTile = unit.gameObject;
            }
        }
    }

    /// <summary>
    /// This function controls the player's choice after moving a unit, i.e. waiting or attacking.
    /// </summary>
    private void WaitOrAttack()
    {
        //Raycast the cursor's position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        //Create a container of the selected unit's attack range.
        HashSet<Node> attackableTiles = GetEnemiesAttackable();

        if (Physics.Raycast(ray, out hit))
        {
            //If the cursor casts to a tile occupied by a unit...
            if (hit.transform.gameObject.CompareTag("Tile")
                && hit.transform.GetComponent<Tile>().unitOccupyingTile != null)
            {
                //Get the unit occupying that tile and their map grid position.
                GameObject unit = hit.transform.GetComponent<Tile>().unitOccupyingTile;
                int unitX = unit.GetComponent<Unit>().tileX;
                int unitZ = unit.GetComponent<Unit>().tileZ;

                //If that unit is the selected unit...
                if (unit == selectedUnit)
                {
                    //Disable highlight quads showing their movement path.
                    DisableQuadUIUnitPath();

                    //Set the selected unit to wait and deselect the unit.
                    selectedUnit.GetComponent<Unit>().Wait();
                    //selectedUnit.GetComponent<Unit>().PlayIdleAnim();
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;
                    DeselectUnit();
                    CheckTeamWaiting();
                }
                //If that unit is a unit from the enemy team, that unit is attackable and it has remaining health points...
                else if (unit.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber
                    && attackableTiles.Contains(graph[unitX, unitZ])
                    && unit.GetComponent<Unit>().currentHealth > 0)
                {
                    //Commence the selected unit's attack on the enemy unit, and deselect the unit.
                    StartCoroutine(battleManager.StartAttack(selectedUnit, unit));
                    StartCoroutine(DeselectUnitAfterAttack(selectedUnit, unit));
                }
            }
            //If the cursor casts to a unit...
            else if (hit.transform.parent != null
                && hit.transform.parent.gameObject.CompareTag("Unit"))
            {
                //Get the unit's map grid position.
                GameObject unit = hit.transform.parent.gameObject;
                int unitX = unit.GetComponent<Unit>().tileX;
                int unitZ = unit.GetComponent<Unit>().tileZ;

                //If the unit is the selected unit...
                if (unit == selectedUnit)
                {
                    //Disable highlight quads showing their movement path.
                    DisableQuadUIUnitPath();

                    //Set the selected unit to wait and deselect the unit.
                    selectedUnit.GetComponent<Unit>().Wait();
                    //selectedUnit.GetComponent<Unit>().PlayIdleAnim();
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;
                    DeselectUnit();
                    CheckTeamWaiting();
                }
                //If that unit is a unit from the enemy team, that unit is attackable and it has remaining health points...
                else if (unit.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber
                    && attackableTiles.Contains(graph[unitX, unitZ])
                    && unit.GetComponent<Unit>().currentHealth > 0)
                {
                    //Commence the selected unit's attack on the enemy unit, and deselect the unit.
                    StartCoroutine(battleManager.StartAttack(selectedUnit, unit));
                    StartCoroutine(DeselectUnitAfterAttack(selectedUnit, unit));
                }
            }
        }
    }

    /// <summary>
    /// Automatically ends the player's turn, if every unit on the player's team has finished their move.
    /// </summary>
    private void CheckTeamWaiting()
    {
        bool teamWaiting = true;

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

    #endregion


    #region Calculations

    /// <summary>
    /// Converts any map grid position into Unity worldspace.
    /// </summary>
    /// <param name="x">The tile's X grid position to be converted into Unity worldspace.</param>
    /// <param name="z">The tile's Z grid position to be converted into Unity worldspace.</param>
    /// <returns></returns>
    public Vector3 GetTileWorldSpace(int x, int z)
    {
        return new Vector3(x, 0, z);
    }

    #endregion

    #endregion
}