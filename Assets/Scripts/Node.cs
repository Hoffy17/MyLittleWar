using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A vertex in a unit's pathfinding data. A node represents one tile on the map grid that the unit has set in its current path.
/// </summary>
public class Node
{
    #region Declarations

    /// <summary>
    /// A list of nodes that are neighbouring another node.
    /// </summary>
    public List<Node> neighbours;
    /// <summary>
    /// The node's position on the map grid's X axis.
    /// </summary>
    public int x;
    /// <summary>
    /// The node's position on the map grid's Z axis.
    /// </summary>
    public int z;

    #endregion


    #region Constructors

    /// <summary>
    /// A constructor creates a new list of neighbouring Nodes for every Node object that is instantiated.
    /// </summary>
    public Node()
    {
        neighbours = new List<Node>();
    }

    #endregion


    #region Functions

    /// <summary>
    /// Takes another Node's X and Z position on the map grid and calculates the distance to it in worldspace.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public float DistanceTo(Node n)
    {
        return Vector2.Distance(
            new Vector2(x, z),
            new Vector2(n.x, n.z));
    }

    #endregion
}
