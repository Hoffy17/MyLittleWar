using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    //The unit's map-tile position
    //Not worldspace coordinates
    public int tileX;
    public int tileZ;
    [HideInInspector]
    public TileMapSystem map;

    //Pathfinding info
    //Null when there is no destination
    public List<Node> currentPath = null;

    //How far the unit can move in one turn
    public int moveSpeed;
    private float remainingMoves;
    public float lerpSpeed;

    private void Update()
    {
        if(currentPath != null)
        {
            int currNode = 0;

            while(currNode < currentPath.Count - 1)
            {
                Vector3 start = map.GetTileWorldPos(currentPath[currNode].x, currentPath[currNode].z)
                    + new Vector3(0, 0.51f, 0);
                Vector3 end = map.GetTileWorldPos(currentPath[currNode + 1].x, currentPath[currNode + 1].z)
                    + new Vector3(0, 0.51f, 0);

                Debug.DrawLine(start, end, Color.red);

                currNode++;
            }
        }

        //Checks if the unit has transformed its position close enough to the target tile
        //If so, the unit can continue to find new paths
        if (Vector3.Distance(transform.position, map.GetTileWorldPos(tileX, tileZ)) < 0.1f)
            AdvancePathfinding();

        //Lerp the unit's position to the correct map tile
        transform.position = Vector3.Lerp(transform.position, map.GetTileWorldPos(tileX, tileZ), lerpSpeed * Time.deltaTime);
    }

    //Advance the unit's pathfinding progress by one tile
    private void AdvancePathfinding()
    {
        if (currentPath == null || remainingMoves <= 0)
            return;

        //Ensure that the unit is at the correct map-tile position
        transform.position = map.GetTileWorldPos(tileX, tileZ);

        //Get cost from current map-tile to next map-tile
        remainingMoves -= map.CostToEnterTile(currentPath[0].x, currentPath[0].z, currentPath[1].x, currentPath[1].z);

        //Move the unit to the next node in the sequence
        tileX = currentPath[1].x;
        tileZ = currentPath[1].z;

        //Remove the old "current" node from the path
        currentPath.RemoveAt(0);

        if (currentPath.Count == 1)
        {
            //If there is only one tile left in the path, that is the unit's target
            //Clear our pathfinding data
            currentPath = null;
        }
    }

    public void NextTurn()
    {
        //Ensure that the unit is at the correct map-tile position
        while (currentPath != null && remainingMoves > 0)
        {
            AdvancePathfinding();
        }

        //Reset unit's available moves
        remainingMoves = moveSpeed;
    }
}
