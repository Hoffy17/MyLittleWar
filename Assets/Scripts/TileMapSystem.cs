using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The system controlling the generation and instantiation of map grids, and their pathfinding data.
/// </summary>
public class TileMapSystem : MonoBehaviour
{
    #region Declarations

    /// <summary>
    /// The current unit that has been selected to move.
    /// </summary>
    [Tooltip("The current unit that has been selected to move.")]
    [SerializeField]
    public GameObject selectedUnit;

    [Header("Map Data")]

    /// <summary>
    /// The different types of tiles that can generate on the map, e.g. grass, forest, mountain, etc.
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
    /// An array that represents the number of tiles in the map grid.
    /// </summary>
    [NonSerialized]
    private int[,] tiles;

    /// <summary>
    /// An array of <see cref="Node"/>s that represents all of the map grid tiles that a unit can create a path to.
    /// </summary>
    [NonSerialized]
    private Node[,] graph;

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

    #endregion


    #region Custom Functions

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

                //if (x > 0)
                //    graph[x, z].neighbours.Add(graph[x - 1, z]);
                //if (x < mapSizeX - 1)
                //    graph[x, z].neighbours.Add(graph[x + 1, z]);
                //if (z > 0)
                //    graph[x, z].neighbours.Add(graph[x, z - 1]);
                //if (z < mapSizeZ - 1)
                //    graph[x, z].neighbours.Add(graph[x, z + 1]);

                #endregion

                #region 8-way Connected Nodes (Allowing Diagonal Movement)

                if (x > 0)
                {
                    //Left
                    graph[x, z].neighbours.Add(graph[x - 1, z]);
                    if (z > 0)
                        //Diagonal left-up
                        graph[x, z].neighbours.Add(graph[x - 1, z - 1]);
                    if (z < mapSizeZ - 1)
                        //Diagonal left-down
                        graph[x, z].neighbours.Add(graph[x - 1, z + 1]);
                }

                if (x < mapSizeX - 1)
                {
                    //Right
                    graph[x, z].neighbours.Add(graph[x + 1, z]);
                    if (z > 0)
                        //Diagonal right-up
                        graph[x, z].neighbours.Add(graph[x + 1, z - 1]);
                    if (z < mapSizeZ - 1)
                        //Diagonal right-down
                        graph[x, z].neighbours.Add(graph[x + 1, z + 1]);
                }

                if (z > 0)
                    //Up
                    graph[x, z].neighbours.Add(graph[x, z - 1]);

                if (z < mapSizeZ - 1)
                    //Down
                    graph[x, z].neighbours.Add(graph[x, z + 1]);

                #endregion
            }
        }
    }

    /// <summary>
    /// Creates a map of tile prefabs in the scene.
    /// </summary>
    private void InstantiateMap()
    {
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                //Find the tile type (grass, forest, etc) for each tile in the map grid, and copy those types into local variables. 
                TileType tt = tileTypes[tiles[x, z]];
                //Instantiate the associated prefab for each tile type in the map grid.
                //Convert the tile's map grid position into a Vector3 to instantiate it at the correct position in worldspace.
                GameObject go = Instantiate(tt.tilePrefab, new Vector3(x, 0, z), Quaternion.identity);

                //Get the MonoBehaviour script attached to an instantiated tile.
                ClickableTile ct = go.GetComponent<ClickableTile>();
                //For each instantiated tile, assign its map grid position for the purpose of clicking on those tiles.
                ct.tileX = x;
                ct.tileZ = z;
                //For each instantiated tile, assign the map system that is controlling it.
                ct.map = this;
            }
        }
    }

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

    /// <summary>
    /// Checks if a tile on the map grid is walkable.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public bool UnitCanEnterTile(int x, int z)
    {
        //Check a unit's type against terrain flags here if necessary.

        return tileTypes[tiles[x,z]].isWalkable;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x">The target </param>
    /// <param name="z"></param>
    public void GeneratePathTo(int x, int z)
    {
        //Clear the unit's previous path.
        selectedUnit.GetComponent<Unit>().currentPath = null;

        //If the unit cannot enter the tile it is generating a path to, exit the function.
        if (UnitCanEnterTile(x,z) == false)
        {
            return;
        }

        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();

        //List of unchecked/unvisited nodes in priority queue
        List<Node> queue = new List<Node>();

        Node source = graph[
            selectedUnit.GetComponent<Unit>().tileX,
            selectedUnit.GetComponent<Unit>().tileZ];
        Node target = graph[x, z];

        dist[source] = 0;
        prev[source] = null;

        //Every node is initialised to have infinite distance
        //"v" for vertex
        foreach (Node v in graph)
        {
            if (v != source)
            {
                dist[v] = Mathf.Infinity;
                prev[v] = null;
            }

            queue.Add(v);
        }

        while(queue.Count > 0)
        {
            //"u" is going to be the unvisited node with the smallest distance
            Node u = null;

            foreach (Node possibleU in queue)
            {
                if (u == null || dist[possibleU] < dist[u])
                {
                    u = possibleU;
                }
            }

            if (u == target)
            {
                break; //Exit the while loop
            }

            queue.Remove(u);

            foreach(Node v in u.neighbours)
            {
                //float alt = dist[u] + u.DistanceTo(v);
                float alt = dist[u] + CostToEnterTile(u.x, u.z, v.x, v.z);

                if (alt < dist[v])
                {
                    dist[v] = alt;
                    prev[v] = u;
                }
            }
        }

        //We either found the shortest route to our target,
        //Or there is no route at all to our target.
        if (prev[target] == null)
        {
            //No route between source and target
            return;
        }

        List<Node> currentPath = new List<Node>();
        Node curr = target;

        //Step through the "prev" chain and add it to our path
        while (curr != null)
        {
            currentPath.Add(curr);
            curr = prev[curr];
        }

        //currentPath describes a route from our target to our source
        //So we need to invert it
        currentPath.Reverse();

        selectedUnit.GetComponent<Unit>().currentPath = currentPath;
    }

    #endregion
}