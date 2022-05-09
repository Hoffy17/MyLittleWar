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
    [SerializeField]
    private GameManager gameManager;
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
    [NonSerialized]
    private Node[,] graph;

    //public List<Node> currentPath = null;

    [Header("Map Game Objects")]
    [SerializeField]
    private GameObject mapUnits;
    /// <summary>
    /// A 2D array containing the list of tile game objecs on the map.
    /// </summary>
    [Tooltip("A 2D array containing the list of tile game objects on the map.")]
    [SerializeField]
    private GameObject[,] mapTiles;

    [Header("Selected Unit")]
    /// <summary>
    /// The current unit that has been selected to move.
    /// </summary>
    [Tooltip("The current unit that has been selected to move.")]
    [SerializeField]
    public GameObject selectedUnit;
    [SerializeField]
    private HashSet<Node> selectedUnitMoveRange;
    [SerializeField]
    private HashSet<Node> selectedUnitMoveRangeTotal;

    [NonSerialized]
    private bool unitSelected;
    [NonSerialized]
    private int unitSelectedPrevX;
    [NonSerialized]
    private int unitSelectedPrevZ;
    [NonSerialized]
    private GameObject unitSelectedPrevTile;

    [Header("Quad UI")]
    [SerializeField]
    private GameObject[,] quadUI;
    [SerializeField]
    private GameObject[,] quadUIUnitMovement;
    [SerializeField]
    private GameObject[,] quadUICursor;

    [Header("Map UI")]
    [SerializeField]
    private GameObject mapUI;
    [SerializeField]
    private GameObject mapUIMovementRange;
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
        selectedUnit.GetComponent<Unit>().map = this;

        //Set up an array of map grid tiles and assigns an integer value for each index in the array.
        GenerateMap();
        //Calculate the map grid positions of all of the Nodes on the map, and creates a list of their neighbours.
        GeneratePathGraph();
        //Create a map of tile prefabs in the scene.
        InstantiateMap();
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if (selectedUnit == null)
            {
                //SelectUnit();
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

        quadUI = new GameObject[mapSizeX, mapSizeZ];
        quadUIUnitMovement = new GameObject[mapSizeX, mapSizeZ];
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

                GameObject gridUI = Instantiate(mapUI, new Vector3(x, 0.501f, z), Quaternion.Euler(90f, 0, 0));
                GameObject gridUIUnitMovement = Instantiate(mapUIMovementRange, new Vector3(x, 0.502f, z), Quaternion.Euler(90f, 0, 0));
                GameObject gridUICursor = Instantiate(mapUICursor, new Vector3(x, 0.503f, z), Quaternion.Euler(90f, 0, 0));

                gridUI.transform.SetParent(quadUIMovementRangeContainer.transform);
                gridUIUnitMovement.transform.SetParent(quadUIMovementPathContainer.transform);
                gridUICursor.transform.SetParent(quadUICursorContainer.transform);

                quadUI[x, z] = gridUI;
                quadUIUnitMovement[x, z] = gridUIUnitMovement;
                quadUICursor[x, z] = gridUICursor;
            }
        }
    }

    #endregion

    #region Pathfinding & Unit Movement

    /// <summary>
    /// This function represents Dijkstra's algorithm and is used to generate a path from a source tile on the map grid to a target tile.
    /// </summary>
    /// <param name="x">The destination tile's position on the map grid's X axis, that a unit is generating a path to.</param>
    /// <param name="z">The destination tile's position on the map grid's Z axis, that a unit is generating a path to.</param>
    public void GeneratePathTo(int x, int z)
    {
        //Clear the unit's previous path.
        selectedUnit.GetComponent<Unit>().currentPath = null;

        //If the unit cannot enter the tile it is generating a path to, exit the function.
        if (UnitCanEnterTile(x,z) == false)
        {
            return;
        }

        //This dictionary stores all of the nodes on the map grid and their distance (as a float) from the unit's current node position.
        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        //This dictionary stores nodes on the shortest path from the source node to the target node.
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();

        //Create a list of unchecked or unvisited Nodes.
        //This is a priority queue of nodes to be checked, when generating a path in Dijkstra's algorithm.
        List<Node> queue = new List<Node>();

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
        foreach (Node n in graph)
        {
            if (n != source)
            {
                dist[n] = Mathf.Infinity;
                prev[n] = null;
            }

            //Every node on the map grid is added into a queue of unchecked nodes.
            queue.Add(n);
        }

        //While there are unchecked nodes in the queue...
        while(queue.Count > 0)
        {
            //Store a temporary node with the smallest distance.
            Node tempNode = null;

            //For each unchecked node in the queue...
            foreach (Node uncheckedNode in queue)
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
            queue.Remove(tempNode);

            //For each node that is neighbouring the temporary node...
            foreach(Node neighbourNode in tempNode.neighbours)
            {
                //float alt = dist[u] + u.DistanceTo(v);

                //Calculate the distance to enter each neighbouring node,
                //based on that neighbouring node's distance from the unit's current node position and the cost to enter that node.
                float tempDist = dist[tempNode] + CostToEnterTile(tempNode.x, tempNode.z, neighbourNode.x, neighbourNode.z);

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
            return;
        }

        //Create a list of nodes that represent the unit's path to its target node.
        List<Node> currentPath = new List<Node>();
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
        selectedUnit.GetComponent<Unit>().currentPath = currentPath;
    }

    /// <summary>
    /// Calculates the movement cost for a unit to move from its current map grid position to its neighbouring, target map grid position.
    /// </summary>
    /// <param name="sourceX">The unit's current position on the map grid's X axis.</param>
    /// <param name="sourceZ">The unit's current position on the map grid's Z axis.</param>
    /// <param name="targetX">The unit's target position on the map grid's X axis.</param>
    /// <param name="targetZ">The unit's target position on the map grid's Z axis.</param>
    /// <returns></returns>
    public float CostToEnterTile(int sourceX, int sourceZ, int targetX, int targetZ)
    {
        //Find the target tile's type (grass, forest, etc) and copy it into a local variable. 
        TileType targetTile = tileTypes[tiles[targetX, targetZ]];

        //Check if the target tile is walkable.
        if (UnitCanEnterTile(targetX, targetZ) == false)
            //If it is not, return a cost of infinity and exit the function.
            return Mathf.Infinity;

        //Get the target tile's movement cost and copy it into a local variable.
        float cost = targetTile.movementCost;

        //If the unit intends to move diagonally, increase cost slightly to ensure a more direct path is taken.
        if (sourceX != targetX && sourceZ != targetZ)
        {
            cost += 0.001f;
        }

        return cost;
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

        return tileTypes[tiles[x, z]].isWalkable;
    }

    public void MoveUnit()
    {
        if (selectedUnit != null)
            selectedUnit.GetComponent<Unit>().AdvanceNextTile();
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