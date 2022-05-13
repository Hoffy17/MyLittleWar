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
    /// <summary>
    /// The GameManager script.
    /// </summary>
    [Tooltip("The GameManager script.")]
    [SerializeField]
    private GameManager gameManager;
    /// <summary>
    /// The BattleManager script.
    /// </summary>
    [Tooltip("The BattleManager script.")]
    [SerializeField]
    private BattleManager battleManager;

    [Header("Map Data")]
    /// <summary>
    /// A 2D array of the different types of tiles that can generate on the map, e.g. grass, forest, mountain, etc.
    /// </summary>
    [Tooltip("The different kinds of tiles that can generate on the map, e.g. grass, forest, mountain, etc.")]
    [SerializeField]
    public TileType[] tileTypes;
    /// <summary>
    /// The number of tiles generated on the map grid's X axis.
    /// </summary>
    [Tooltip("The number of tiles generated on the map grid's X axis.")]
    [SerializeField]
    public int mapSizeX = 10;
    /// <summary>
    /// The number of tiles generated on the map grid's Z axis.
    /// </summary>
    [Tooltip("The number of tiles generated on the map grid's Z axis.")]
    [SerializeField]
    public int mapSizeZ = 10;
    /// <summary>
    /// A 2D array that represents the number of tiles in the map grid.
    /// </summary>
    [NonSerialized]
    private int[,] tiles;
    /// <summary>
    /// A 2D array of <see cref="Node"/>s that represents all of the map grid tiles that a unit can create a path to.
    /// </summary>
    [HideInInspector]
    public Node[,] graph;
    /// <summary>
    ///A list of <see cref="Node"/>s that represent a path from one map grid position to another.
    /// </summary>
    [NonSerialized]
    private List<Node> currentPath = null;

    [Header("Map Game Objects")]
    /// <summary>
    /// The units that are currently on the map. This is used to set tiles that are currently occupied.
    /// </summary>
    [Tooltip("The units that are currently on the map. This is used to set tiles that are currently occupied.")]
    [SerializeField]
    private GameObject mapUnits;
    /// <summary>
    /// A 2D array containing the list of tile game objects on the map.
    /// </summary>
    [Tooltip("A 2D array containing the list of tile game objects on the map.")]
    [HideInInspector]
    public GameObject[,] mapTiles;

    [Header("Selected Unit")]
    /// <summary>
    /// The current unit that has been clicked on.
    /// </summary>
    [HideInInspector]
    public GameObject selectedUnit;
    /// <summary>
    /// A container of <see cref="Node"/>s in a selected unit's movement range.
    /// Based on its move speed and any attackable enemies within this range.
    /// </summary>
    [HideInInspector]
    public HashSet<Node> selectedUnitMoveRange;
    //[SerializeField]
    //private HashSet<Node> selectedUnitMoveRangeTotal;
    /// <summary>
    /// Checks whether or not the player has selected a unit.
    /// </summary>
    [NonSerialized]
    private bool unitSelected;
    /// <summary>
    /// The selected unit's previous map grid position on the X axis.
    /// </summary>
    [NonSerialized]
    private int unitSelectedPrevX;
    /// <summary>
    /// The selected unit's previous map grid position on the Z axis.
    /// </summary>
    [NonSerialized]
    private int unitSelectedPrevZ;
    /// <summary>
    /// The previous tile that the selected unit was occupying.
    /// </summary>
    [NonSerialized]
    private GameObject unitSelectedPrevTile;

    [Header("Quad UI")]
    /// <summary>
    /// A 2D array of quads that are activated to highlight a unit's movement range and attackable enemies.
    /// </summary> 
    [Tooltip("A 2D array of quads that are activated to highlight a unit's movement range and attackable enemies.")]
    [SerializeField]
    private GameObject[,] quadUIUnitMoveRange;
    /// <summary>
    /// A 2D array of quads that are activated to highlight a unit's proposed movement path.
    /// </summary>
    [Tooltip("A 2D array of quads that are activated to highlight a unit's proposed movement path.")]
    [HideInInspector]
    public GameObject[,] quadUIUnitPath;
    /// <summary>
    /// A 2D array of quads that are activated to highlight a single tile that the cursor is raycasting to.
    /// </summary>
    [HideInInspector]
    [Tooltip("A 2D array of quads that are activated to highlight a single tile that the cursor is raycasting to.")]
    public GameObject[,] quadUICursor;

    [Header("Map UI")]
    /// <summary>
    /// The quad prefab that is instantiated to highlight a unit's movement range and attackable enemies.
    /// </summary>
    [Tooltip("The quad prefab that is instantiated to highlight a unit's movement range and attackable enemies.")]
    [SerializeField]
    private GameObject mapUIUnitMoveRange;
    /// <summary>
    /// The quad prefab that is instantiated to highlight a unit's proposed movement path.
    /// </summary>
    [Tooltip("The quad prefab that is instantiated to highlight a unit's proposed movement path.")]
    [SerializeField]
    private GameObject mapUIUnitPath;
    /// <summary>
    /// The quad prefab that is instantiated to highlight a single tile that the cursor is raycasting to.
    /// </summary>
    [Tooltip("The quad prefab that is instantiated to highlight a single tile that the cursor is raycasting to.")]
    [SerializeField]
    private GameObject mapUICursor;

    [Header("UI Materials")]
    [SerializeField]
    private Material uIMatGreen;
    [SerializeField]
    private Material uIMatRed;
    [SerializeField]
    private Material uIMatBlue;

    [Header("Containers")]
    [SerializeField]
    private GameObject tileContainer;
    [SerializeField]
    private GameObject quadUIMovementRangeContainer;
    [SerializeField]
    private GameObject quadUIMovementPathContainer;
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

        SetTileOccupied();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            SelectUnit();

        if (Input.GetMouseButtonDown(1))
        {
            if (selectedUnit != null)
            {
                if (selectedUnit.GetComponent<Unit>().movementQueue.Count == 0
                    && selectedUnit.GetComponent<Unit>().combatQueue.Count == 0
                    && selectedUnit.GetComponent<Unit>().movementState == MovementState.Waiting)
                {
                    //sound.Play();
                    //selectedUnit.GetComponent<Unit>().PlayIdleAnim();

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
        ////Forest
        //for (x = 3; x <= 5; x++)
        //{
        //    for (z = 0; z < 4; z++)
        //    {
        //        tiles[x, z] = 1;
        //    }
        //}

        ////Mountain Range
        //tiles[4, 4] = 2;
        //tiles[5, 4] = 2;
        //tiles[6, 4] = 2;
        //tiles[7, 4] = 2;
        //tiles[8, 4] = 2;
        //tiles[4, 5] = 2;
        //tiles[4, 6] = 2;
        //tiles[8, 5] = 2;
        //tiles[8, 6] = 2;
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
        mapTiles = new GameObject[mapSizeX, mapSizeZ];

        quadUIUnitMoveRange = new GameObject[mapSizeX, mapSizeZ];
        quadUIUnitPath = new GameObject[mapSizeX, mapSizeZ];
        quadUICursor = new GameObject[mapSizeX, mapSizeZ];

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                //Find the tile type (grass, forest, etc) for each tile in the map grid, and copy those types into local variables. 
                TileType tt = tileTypes[tiles[x, z]];
                //Instantiate the associated prefab for each tile type in the map grid.
                //Convert the tile's map grid position into a Vector3 to instantiate it at the correct position in worldspace.
                GameObject newTile = Instantiate(tt.tilePrefab, new Vector3(x, 0, z), Quaternion.identity);

                //Get the MonoBehaviour script attached to an instantiated tile.
                ClickableTile clickableTile = newTile.GetComponent<ClickableTile>();
                //For each instantiated tile, assign its map grid position for the purpose of clicking on those tiles.
                clickableTile.tileX = x;
                clickableTile.tileZ = z;
                //For each instantiated tile, assign the map system that is controlling it.
                clickableTile.map = this;

                newTile.transform.SetParent(tileContainer.transform);
                mapTiles[x, z] = newTile;

                GameObject gridUI = Instantiate(mapUIUnitMoveRange, new Vector3(x, 0.501f, z), Quaternion.Euler(90f, 0, 0));
                GameObject gridUIUnitMovement = Instantiate(mapUIUnitPath, new Vector3(x, 0.502f, z), Quaternion.Euler(90f, 0, 0));
                GameObject gridUICursor = Instantiate(mapUICursor, new Vector3(x, 0.503f, z), Quaternion.Euler(90f, 0, 0));

                gridUI.transform.SetParent(quadUIMovementRangeContainer.transform);
                gridUIUnitMovement.transform.SetParent(quadUIMovementPathContainer.transform);
                gridUICursor.transform.SetParent(quadUICursorContainer.transform);

                quadUIUnitMoveRange[x, z] = gridUI;
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
    /// Calculates the movement cost for a unit to move from its current map grid position to its neighbouring, target map grid position.
    /// </summary>
    /// <param name="sourceX">The unit's current position on the map grid's X axis.</param>
    /// <param name="sourceZ">The unit's current position on the map grid's Z axis.</param>
    /// <param name="x">The unit's target position on the map grid's X axis.</param>
    /// <param name="z">The unit's target position on the map grid's Z axis.</param>
    /// <returns></returns>
    private float CostToEnterTile(int x, int z)
    {
        ////Find the target tile's type (grass, forest, etc) and copy it into a local variable. 
        //TileType targetTile = tileTypes[tiles[targetX, targetZ]];

        ////Check if the target tile is walkable.
        //if (UnitCanEnterTile(targetX, targetZ) == false)
        //    //If it is not, return a cost of infinity and exit the function.
        //    return Mathf.Infinity;

        ////Get the target tile's movement cost and copy it into a local variable.
        //float cost = targetTile.movementCost;

        ////If the unit intends to move diagonally, increase cost slightly to ensure a more direct path is taken.
        //if (sourceX != targetX && sourceZ != targetZ)
        //{
        //    cost += 0.001f;
        //}

        //return cost;

        if (UnitCanEnterTile(x, z) == false)
            return Mathf.Infinity;

        TileType tile = tileTypes[tiles[x, z]];
        float dist = tile.movementCost;

        return dist;
    }

    /// <summary>
    /// Checks if a tile on the map grid is walkable.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public bool UnitCanEnterTile(int x, int z)
    {
        //Check a unit's type against terrain flags here if necessary.

        if (mapTiles[x, z].GetComponent<ClickableTile>().unitOccupyingTile != null)
        {
            if (mapTiles[x, z].GetComponent<ClickableTile>().unitOccupyingTile.GetComponent<Unit>().teamNumber !=
                selectedUnit.GetComponent<Unit>().teamNumber)
                return false;
        }
        return tileTypes[tiles[x, z]].isWalkable;
    }

    #endregion


    #region Movement Range

    private void MovementRange()
    {
        HashSet<Node> movementRange = new HashSet<Node>();
        HashSet<Node> attackableEnemies = new HashSet<Node>();
        HashSet<Node> enemiesInRange = new HashSet<Node>();

        int attackRange = selectedUnit.GetComponent<Unit>().attackRange;
        int movespeed = selectedUnit.GetComponent<Unit>().moveSpeed;

        Node startNode = graph[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ];

        movementRange = GetMovementRange(movementRange, movespeed, startNode);
        attackableEnemies = GetAttackableTiles(movementRange, attackableEnemies, attackRange, startNode);

        foreach (Node node in attackableEnemies)
            if (mapTiles[node.x, node.z].GetComponent<ClickableTile>().unitOccupyingTile != null)
            {
                GameObject unitOccupyingSelectedTile = mapTiles[node.x, node.z].GetComponent<ClickableTile>().unitOccupyingTile;

                if (unitOccupyingSelectedTile.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber)
                    enemiesInRange.Add(node);
            }

        HighlightMovementRange(movementRange);
        HighlightAttackableEnemies(attackableEnemies);

        selectedUnitMoveRange = movementRange;
        //selectedUnitMoveRangeTotal = GetTotalRange(movementRange, attackableEnemies);
    }

    private HashSet<Node> GetMovementRange(HashSet<Node> movementRange, int movespeed, Node startNode)
    {
        float[,] cost = new float[mapSizeX, mapSizeZ];

        HashSet<Node> uIHighlight = new HashSet<Node>();
        HashSet<Node> tempUIHighlight = new HashSet<Node>();

        movementRange.Add(startNode);

        foreach (Node node in startNode.neighbours)
        {
            cost[node.x, node.z] = CostToEnterTile(node.x, node.z);

            if (movespeed - cost[node.x, node.z] >= 0)
                uIHighlight.Add(node);
        }

        movementRange.UnionWith(uIHighlight);

        while (uIHighlight.Count != 0)
        {
            foreach (Node node in uIHighlight)
                foreach (Node neighbour in node.neighbours)
                    if (!movementRange.Contains(neighbour))
                    {
                        cost[neighbour.x, neighbour.z] = CostToEnterTile(neighbour.x, neighbour.z) + cost[node.x, node.z];

                        if (movespeed - cost[neighbour.x, neighbour.z] >= 0)
                            tempUIHighlight.Add(neighbour);
                    }

            uIHighlight = tempUIHighlight;
            movementRange.UnionWith(uIHighlight);
            tempUIHighlight = new HashSet<Node>();
        }

        return movementRange;
    }

    private HashSet<Node> GetAttackableTiles(HashSet<Node> movementRange, HashSet<Node> attackableTiles, int attackRange, Node startNode)
    {
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        HashSet<Node> seenNodes = new HashSet<Node>();

        foreach (Node node in movementRange)
        {
            neighbourHash = new HashSet<Node>();
            neighbourHash.Add(node);

            for (int i = 0; i < attackRange; i++)
            {
                foreach (Node neighbourNode in neighbourHash)
                    foreach (Node tempNeighbourNode in neighbourNode.neighbours)
                        tempNeighbourHash.Add(tempNeighbourNode);

                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();

                if (i < attackRange - 1)
                    seenNodes.UnionWith(neighbourHash);
            }

            neighbourHash.ExceptWith(seenNodes);
            seenNodes = new HashSet<Node>();
            attackableTiles.UnionWith(neighbourHash);
        }

        attackableTiles.Remove(startNode);

        return attackableTiles;
    }

    private HashSet<Node> GetAttackableNeighbours()
    {
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        HashSet<Node> seenNodes = new HashSet<Node>();

        Node startNode = graph[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ];
        int attackRange = selectedUnit.GetComponent<Unit>().attackRange;

        neighbourHash.Add(startNode);

        for (int i = 0; i < attackRange; i++)
        {
            foreach (Node neighbourNode in neighbourHash)
                foreach (Node tempNeighbourNode in neighbourNode.neighbours)
                    tempNeighbourHash.Add(tempNeighbourNode);

            neighbourHash = tempNeighbourHash;
            tempNeighbourHash = new HashSet<Node>();

            if (i < attackRange - 1)
                seenNodes.UnionWith(neighbourHash);
        }
        neighbourHash.ExceptWith(seenNodes);
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

    private HashSet<Node> GetOccupiedTile()
    {
        HashSet<Node> occupiedTile = new HashSet<Node>();
        occupiedTile.Add(graph[
            selectedUnit.GetComponent<Unit>().tileX,
            selectedUnit.GetComponent<Unit>().tileZ]);
        return occupiedTile;
    }

    private void HighlightMovementRange(HashSet<Node> movementRange)
    {
        foreach (Node node in movementRange)
        {
            quadUIUnitMoveRange[node.x, node.z].GetComponent<Renderer>().material = uIMatBlue;
            quadUIUnitMoveRange[node.x, node.z].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    private void HighlightAttackableEnemies(HashSet<Node> attackableEnemies)
    {
        foreach (Node node in attackableEnemies)
        {
            quadUIUnitMoveRange[node.x, node.z].GetComponent<Renderer>().material = uIMatRed;
            quadUIUnitMoveRange[node.x, node.z].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    private void DisableQuadUI()
    {
        foreach (GameObject quad in quadUIUnitMoveRange)
        {
            if (quad.GetComponent<Renderer>().enabled == true)
                quad.GetComponent<Renderer>().enabled = false;
        }
    }

    public void DisableQuadUIUnitMovement()
    {
        foreach(GameObject quad in quadUIUnitPath)
        {
            if (quad.GetComponent<Renderer>().enabled == true)
                quad.GetComponent<Renderer>().enabled = false;
        }
    }

    #endregion


    #region Unit Movement

    private void SelectUnit()
    {
        if (selectedUnit == null)
        {
            if (unitSelected == false
                && gameManager.highlightedTile != null
                && gameManager.highlightedTile.GetComponent<ClickableTile>().unitOccupyingTile != null)
            {
                GameObject tempSelectedUnit = gameManager.highlightedTile.GetComponent<ClickableTile>().unitOccupyingTile;

                if (tempSelectedUnit.GetComponent<Unit>().movementState == MovementState.Unselected
                    && tempSelectedUnit.GetComponent<Unit>().teamNumber == gameManager.currentTeam)
                {
                    DisableQuadUI();

                    selectedUnit = tempSelectedUnit;
                    selectedUnit.GetComponent<Unit>().map = this;
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Selected;
                    //selectedUnit.GetComponent<Unit>().animator.SetTrigger("Selected");
                    unitSelected = true;

                    MovementRange();
                }
            }
        }
        else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected
            && selectedUnit.GetComponent<Unit>().movementQueue.Count == 0
            && SelectTileToMoveTo())
        {
            //sound.Play();
            unitSelectedPrevX = selectedUnit.GetComponent<Unit>().tileX;
            unitSelectedPrevZ = selectedUnit.GetComponent<Unit>().tileZ;
            unitSelectedPrevTile = selectedUnit.GetComponent<Unit>().occupiedTile;
            //selectedUnit.GetComponent<Unit>().PlayWalkingAnim();
            MoveUnit();

            StartCoroutine(FinaliseMovement());
        }
        else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Moved)
        {
            FinaliseUnitTurn();
        }
    }

    private void DeselectUnit()
    {
        if (selectedUnit != null)
        {
            if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected)
            {
                DisableQuadUI();
                DisableQuadUIUnitMovement();

                selectedUnit.GetComponent<Unit>().movementState = MovementState.Unselected;
                selectedUnit = null;
                unitSelected = false;
            }
            else if (selectedUnit.GetComponent<Unit>().movementState == MovementState.Waiting)
            {
                DisableQuadUI();
                DisableQuadUIUnitMovement();

                selectedUnit = null;
                unitSelected = false;
            }
            else
            {
                DisableQuadUI();
                DisableQuadUIUnitMovement();

                mapTiles[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ]
                    .GetComponent<ClickableTile>().unitOccupyingTile = null;
                mapTiles[unitSelectedPrevX, unitSelectedPrevZ].GetComponent<ClickableTile>().unitOccupyingTile = selectedUnit;

                selectedUnit.GetComponent<Unit>().tileX = unitSelectedPrevX;
                selectedUnit.GetComponent<Unit>().tileZ = unitSelectedPrevZ;
                selectedUnit.GetComponent<Unit>().occupiedTile = unitSelectedPrevTile;
                selectedUnit.transform.position = GetTileWorldSpace(unitSelectedPrevX, unitSelectedPrevZ);

                selectedUnit.GetComponent<Unit>().movementState = MovementState.Unselected;

                selectedUnit = null;
                unitSelected = false;
            }
        }
    }

    private bool SelectTileToMoveTo()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.gameObject.CompareTag("Tile"))
            {
                int clickedTileX = hit.transform.GetComponent<ClickableTile>().tileX;
                int clickedTileZ = hit.transform.GetComponent<ClickableTile>().tileZ;
                Node clickedNode = graph[clickedTileX, clickedTileZ];

                if (selectedUnitMoveRange.Contains(clickedNode))
                {
                    if ((hit.transform.gameObject.GetComponent<ClickableTile>().unitOccupyingTile == null
                        || hit.transform.gameObject.GetComponent<ClickableTile>().unitOccupyingTile == selectedUnit)
                        && selectedUnitMoveRange.Contains(clickedNode))
                    {
                        selectedUnit.GetComponent<Unit>().path = GeneratePathTo(clickedTileX, clickedTileZ);
                        return true;
                    }
                }
            }
            else if (hit.transform.gameObject.CompareTag("Unit"))
            {
                //The user clicked on an enemy.
                if (hit.transform.parent.GetComponent<Unit>().teamNumber !=
                    selectedUnit.GetComponent<Unit>().teamNumber)
                    return false;
                else if (hit.transform.gameObject == selectedUnit)
                {
                    selectedUnit.GetComponent<Unit>().path = GeneratePathTo(selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ);
                    return true;
                }
            }
        }
        return false;
    }

    private void MoveUnit()
    {
        if (selectedUnit != null)
            selectedUnit.GetComponent<Unit>().AdvanceNextTile();
    }

    private IEnumerator FinaliseMovement()
    {
        DisableQuadUI();
        DisableQuadUIUnitMovement();

        while (selectedUnit.GetComponent<Unit>().movementQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        FinaliseMovementPos();
        //selectedUnit.GetComponent<Unit>().PlaySelectedAnim();
    }

    private IEnumerator DeselectUnitAfterTurn(GameObject attacker, GameObject defender)
    {
        //SelectSound.Play();

        selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;

        DisableQuadUI();
        DisableQuadUIUnitMovement();

        yield return new WaitForSeconds(.25f);

        while (attacker.GetComponent<Unit>().combatQueue.Count > 0)
            yield return new WaitForEndOfFrame();
        while (defender.GetComponent<Unit>().combatQueue.Count > 0)
            yield return new WaitForEndOfFrame();

        DeselectUnit();
    }

    private void FinaliseMovementPos()
    {
        mapTiles[selectedUnit.GetComponent<Unit>().tileX, selectedUnit.GetComponent<Unit>().tileZ]
            .GetComponent<ClickableTile>().unitOccupyingTile = selectedUnit;

        selectedUnit.GetComponent<Unit>().movementState = MovementState.Moved;

        if (selectedUnit != null)
        {
            HighlightAttackableEnemies(GetAttackableNeighbours());
            HighlightMovementRange(GetOccupiedTile());
        }
    }

    private void SetTileOccupied()
    {
        foreach (Transform team in mapUnits.transform)
        {
            foreach (Transform unit in team)
            {
                int unitTileX = unit.GetComponent<Unit>().tileX;
                int unitTileZ = unit.GetComponent<Unit>().tileZ;

                unit.GetComponent<Unit>().occupiedTile = mapTiles[unitTileX, unitTileZ];

                mapTiles[unitTileX, unitTileZ].GetComponent<ClickableTile>().unitOccupyingTile = unit.gameObject;
            }
        }
    }

    private void FinaliseUnitTurn()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        HashSet<Node> attackableTiles = GetAttackableNeighbours();

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.gameObject.CompareTag("Tile")
                && hit.transform.GetComponent<ClickableTile>().unitOccupyingTile != null)
            {
                GameObject unit = hit.transform.GetComponent<ClickableTile>().unitOccupyingTile;
                int unitX = unit.GetComponent<Unit>().tileX;
                int unitZ = unit.GetComponent<Unit>().tileZ;

                if (unit == selectedUnit)
                {
                    DisableQuadUIUnitMovement();

                    selectedUnit.GetComponent<Unit>().Wait();
                    //selectedUnit.GetComponent<Unit>().PlayIdleAnim();
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;
                    DeselectUnit();
                }
                else if (unit.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber
                    && attackableTiles.Contains(graph[unitX, unitZ])
                    && unit.GetComponent<Unit>().currentHealth > 0)
                {
                    StartCoroutine(battleManager.StartAttack(selectedUnit, unit));
                    StartCoroutine(DeselectUnitAfterTurn(selectedUnit, unit));
                }
            }
            else if (hit.transform.parent != null
                && hit.transform.parent.gameObject.CompareTag("Unit"))
            {
                GameObject unit = hit.transform.parent.gameObject;
                int unitX = unit.GetComponent<Unit>().tileX;
                int unitZ = unit.GetComponent<Unit>().tileZ;

                if (unit == selectedUnit)
                {
                    DisableQuadUIUnitMovement();

                    selectedUnit.GetComponent<Unit>().Wait();
                    //selectedUnit.GetComponent<Unit>().PlayIdleAnim();
                    selectedUnit.GetComponent<Unit>().movementState = MovementState.Waiting;
                    DeselectUnit();
                }
                else if (unit.GetComponent<Unit>().teamNumber != selectedUnit.GetComponent<Unit>().teamNumber
                    && attackableTiles.Contains(graph[unitX, unitZ])
                    && unit.GetComponent<Unit>().currentHealth > 0)
                {
                    StartCoroutine(battleManager.StartAttack(selectedUnit, unit));
                    StartCoroutine(DeselectUnitAfterTurn(selectedUnit, unit));
                }
            }
        }
    }

    #endregion


    #region Calculations

    /// <summary>
    /// Converts any map grid position into Unity worldspace.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public Vector3 GetTileWorldSpace(int x, int z)
    {
        return new Vector3(x, 0, z);
    }

    #endregion

    #endregion
}