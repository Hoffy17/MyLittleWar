using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileMapSystem : MonoBehaviour
{
    #region Declarations

    public GameObject selectedUnit;

    public TileType[] tileTypes;

    private int[,] tiles;
    private Node[,] graph;

    public int mapSizeX = 10;
    public int mapSizeZ = 10;

    #endregion

    #region Unity Functions
    private void Start()
    {
        //Setup the selected unit's variables
        selectedUnit.GetComponent<Unit>().tileX = (int)selectedUnit.transform.position.x;
        selectedUnit.GetComponent<Unit>().tileZ = (int)selectedUnit.transform.position.z;
        selectedUnit.GetComponent<Unit>().map = this;

        GenerateMap();
        GeneratePathGraph();
        InstantiateMap();
    }
    #endregion

    #region Custom Functions
    private void GenerateMap()
    {
        //Allocate map tiles
        tiles = new int[mapSizeX, mapSizeZ];

        int x, z;

        //Initialise map tiles as grass
        for (x = 0; x < mapSizeX; x++)
        {
            for (z = 0; z < mapSizeX; z++)
            {
                //0=Grass, 1=Forest, 2=Mountain
                tiles[x, z] = 0;
            }
        }

        #region Hard-Coded Terrain
        //Forest
        for (x = 3; x <= 5; x++)
        {
            for (z = 0; z < 4; z++)
            {
                tiles[x, z] = 1;
            }
        }

        //Mountain Range
        tiles[4, 4] = 2;
        tiles[5, 4] = 2;
        tiles[6, 4] = 2;
        tiles[7, 4] = 2;
        tiles[8, 4] = 2;
        tiles[4, 5] = 2;
        tiles[4, 6] = 2;
        tiles[8, 5] = 2;
        tiles[8, 6] = 2;
        #endregion
    }

    public float CostToEnterTile(int sourceX, int sourceZ, int targetX, int targetZ)
    {
        TileType tt = tileTypes[tiles[targetX, targetZ]];

        if (UnitCanEnterTile(targetX, targetZ) == false)
            return Mathf.Infinity;

        float cost = tt.movementCost;

        //If the unit is moving diagonally, increase cost by 1
        if (sourceX != targetX && sourceZ != targetZ)
        {
            cost += 0.001f;
        }

        return cost;
    }

    private void GeneratePathGraph()
    {
        //Initialise the array
        graph = new Node[mapSizeX, mapSizeZ];

        //Initialise a Node for each spot in the array
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                graph[x, z] = new Node();
                graph[x, z].x = x;
                graph[x, z].z = z;
            }
        }

        //Now that all the nodes exist, calculate their neighbours
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                //4-way connection

                //if (x > 0)
                //    graph[x, z].neighbours.Add(graph[x - 1, z]);
                //if (x < mapSizeX - 1)
                //    graph[x, z].neighbours.Add(graph[x + 1, z]);
                //if (z > 0)
                //    graph[x, z].neighbours.Add(graph[x, z - 1]);
                //if (z < mapSizeZ - 1)
                //    graph[x, z].neighbours.Add(graph[x, z + 1]);

                //8-way connection (allowing diagonal movement)

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
            }
        }
    }

    private void InstantiateMap()
    {
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int z = 0; z < mapSizeX; z++)
            {
                TileType tt = tileTypes[tiles[x, z]];
                GameObject go = Instantiate(tt.tilePrefab, new Vector3(x, 0, z), Quaternion.identity);

                ClickableTile ct = go.GetComponent<ClickableTile>();
                ct.tileX = x;
                ct.tileZ = z;

                ct.map = this;
            }
        }
    }

    public Vector3 GetTileWorldPos(int x, int z)
    {
        return new Vector3(x, 0, z);
    }

    public bool UnitCanEnterTile(int x, int z)
    {
        //Check a unit's type against terrain flags here if necessary

        return tileTypes[tiles[x,z]].isWalkable;
    }

    public void GeneratePathTo(int x, int z)
    {
        //Clear out the unit;s old path
        selectedUnit.GetComponent<Unit>().currentPath = null;

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