using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TileType
{
    public string name;
    public GameObject tilePrefab;

    public bool isWalkable = true;
    public float movementCost = 1;
}