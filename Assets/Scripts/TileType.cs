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
    [Tooltip("The name of the tile type.")]
    public string name;
    [Tooltip("The GameObject that will be instantiated on the map when this tile type is generated.")]
    public GameObject tilePrefab;
    [Tooltip("If a unit is standing on this tile, this variable is updated to reflect that.")]
    public GameObject unitOccupyingTile;
    [Tooltip("Determines whether a unit is able to move onto this tile type.")]
    public bool isWalkable = true;
    [Tooltip("The amount that a unit must spend to move onto or through this tile type.")]
    public float movementCost = 1;
}