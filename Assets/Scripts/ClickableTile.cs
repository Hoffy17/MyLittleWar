using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A tile that makes up the game's map grid.
/// </summary>
public class ClickableTile : MonoBehaviour
{
    #region Declarations

    /// <summary>
    /// The tile's position on the map grid's X axis.
    /// This is different to its position in worldspace.
    /// </summary>
    [Tooltip("The tile's position on the map grid's X axis.")]
    [HideInInspector]
    public int tileX;
    /// <summary>
    /// The tile's position on the map grid's Z axis.
    /// This is different to its position in worldspace.
    /// </summary>
    [Tooltip("The tile's position on the map grid's Z axis.")]
    [HideInInspector]
    public int tileZ;
    /// <summary>
    /// The unit that is currently occupying this tile object.
    /// </summary>
    [HideInInspector]
    public GameObject unitOccupyingTile;
    /// <summary>
    /// The map grid on which this tile is placed.
    /// </summary>
    [Tooltip("The map grid on which this tile is placed.")]
    [HideInInspector]
    public MapManager map;

    #endregion


    #region Unity Functions

    //private void OnMouseUp()
    //{
    //    if (EventSystem.current.IsPointerOverGameObject())
    //        return;

    //    //When a tile is clicked, call this map function to generate a path to it, based on the tile's map grid position.
    //    map.GeneratePathTo(tileX, tileZ);
    //}

    #endregion
}
