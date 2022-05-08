using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class defining different kinds of tiles, e.g. grass, forest, mountain, etc.
/// </summary>
[Serializable]
public class TileType
{
    /// <summary>
    /// The name of the tile type.
    /// </summary>
    [Tooltip("The name of the tile type.")]
    public string name;
    /// <summary>
    /// The GameObject that will be instantiated on the map when this tile type is generated.
    /// </summary>
    [Tooltip("The GameObject that will be instantiated on the map when this tile type is generated.")]
    public GameObject tilePrefab;
    /// <summary>
    /// Determines whether a unit is able to move onto this tile type.
    /// </summary>
    [Tooltip("Determines whether a unit is able to move onto this tile type.")]
    public bool isWalkable = true;
    /// <summary>
    /// The amount that a unit must spend to move onto or through this tile type.
    /// </summary>
    [Tooltip("The amount that a unit must spend to move onto or through this tile type.")]
    public float movementCost = 1;
}