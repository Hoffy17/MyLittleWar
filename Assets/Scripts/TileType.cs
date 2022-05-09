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
    public string name;
    /// <summary>
    /// The GameObject that will be instantiated on the map when this tile type is generated.
    /// </summary>
    public GameObject tilePrefab;
    /// <summary>
    /// If a unit is standing on this tile, this variable is updated to reflect that.
    /// </summary>
    public GameObject unitOccupyingTile;
    /// <summary>
    /// Determines whether a unit is able to move onto this tile type.
    /// </summary>
    public bool isWalkable = true;
    /// <summary>
    /// The amount that a unit must spend to move onto or through this tile type.
    /// </summary>
    public float movementCost = 1;
}