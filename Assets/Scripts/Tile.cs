using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A tile that makes up the game's map grid.
/// </summary>
public class Tile : MonoBehaviour
{
    #region Declarations

    [Tooltip("The tile's position on the map grid's X axis.")]
    [HideInInspector]
    public int tileX;
    [Tooltip("The tile's position on the map grid's Z axis.")]
    [HideInInspector]
    public int tileZ;
    [Tooltip("The unit that is currently occupying this tile object.")]
    [HideInInspector]
    public GameObject unitOccupyingTile;
    [Tooltip("The map grid on which this tile is placed.")]
    [HideInInspector]
    public MapManager map;

    #endregion
}