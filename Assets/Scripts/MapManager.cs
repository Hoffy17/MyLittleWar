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
    [Tooltip("The MapUIManager script.")]
    [SerializeField]
    private MapUIManager mapUIManager;
    [Tooltip("The UnitMovement script.")]
    [SerializeField]
    private UnitMovement unitMovement;

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
    public GameObject mapUnits;
    [Tooltip("A 2D array containing the list of tile game objects on the map.")]
    [HideInInspector]
    public GameObject[,] mapTiles;

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
        unitMovement.SetTileOccupied();
    }

    private void Update()
    {
        //On left-mouse click, select units and/or tiles.
        if (Input.GetMouseButtonDown(0))
        {
            unitMovement.SelectUnit();
            //Debug.Log("Tile Clicked: " + gameManager.highlightedTile.GetComponent<Tile>().tileX + ", " + gameManager.highlightedTile.GetComponent<Tile>().tileZ);
        }

        //On right-mouse click, deselect units.
        if (Input.GetMouseButtonDown(1))
        {
            //If there is currently a selected unit...
            if (unitMovement.selectedUnit != null)
            {
                //And the unit has not yet finished its turn...
                if (unitMovement.selectedUnit.GetComponent<Unit>().movementQueue.Count == 0
                    && unitMovement.selectedUnit.GetComponent<Unit>().combatQueue.Count == 0
                    && unitMovement.selectedUnit.GetComponent<Unit>().movementState != MovementState.Waiting)
                {
                    //sound.Play();
                    unitMovement.selectedUnit.GetComponent<Unit>().SetAnimIdle();

                    //Deselect the unit.
                    unitMovement.DeselectUnit();
                }
                else if (unitMovement.selectedUnit.GetComponent<Unit>().movementQueue.Count == 1)
                    unitMovement.selectedUnit.GetComponent<Unit>().lerpSpeed = 0.5f;
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
        mapUIManager.quadUIUnitRange = new GameObject[mapSizeX, mapSizeZ];
        mapUIManager.quadUIUnitPath = new GameObject[mapSizeX, mapSizeZ];
        mapUIManager.quadUICursor = new GameObject[mapSizeX, mapSizeZ];

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                //Find the tile type (grass, forest, etc.) for each tile in the map grid, and copy those types into local variables. 
                TileType tt = tileTypes[tiles[x, z]];

                //Instantiate the associated prefab for each tile type and quad in the map grid.
                //Convert the tile/quad's map grid position into a Vector3 to instantiate it at the correct position in worldspace.
                GameObject newTile = Instantiate(tt.tilePrefab, new Vector3(x, 0, z), Quaternion.identity);
                GameObject gridUI = Instantiate(mapUIManager.mapUIUnitRange, new Vector3(x, 0.501f, z), Quaternion.Euler(90f, 0, 0));
                GameObject gridUIUnitMovement = Instantiate(mapUIManager.mapUIUnitPath, new Vector3(x, 0.502f, z), Quaternion.Euler(90f, 0, 0));
                GameObject gridUICursor = Instantiate(mapUIManager.mapUICursor, new Vector3(x, 0.503f, z), Quaternion.Euler(90f, 0, 0));

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
                mapUIManager.quadUIUnitRange[x, z] = gridUI;
                mapUIManager.quadUIUnitPath[x, z] = gridUIUnitMovement;
                mapUIManager.quadUICursor[x, z] = gridUICursor;
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
        if (unitMovement.selectedUnit.GetComponent<Unit>().tileX == x &&
            unitMovement.selectedUnit.GetComponent<Unit>().tileZ == z)
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
            unitMovement.selectedUnit.GetComponent<Unit>().tileX,
            unitMovement.selectedUnit.GetComponent<Unit>().tileZ];
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
    public float CostToEnterTile(int x, int z)
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
                unitMovement.selectedUnit.GetComponent<Unit>().teamNumber)
            //The unit cannot enter the tile.
            return false;
        //Otherwise, return the target tile's walkability boolean.
        return tileTypes[tiles[x, z]].isWalkable;
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