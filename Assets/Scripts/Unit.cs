using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    #region Declarations

    [Header("Unit Data")]

    [SerializeField]
    public int teamNumber;
    [SerializeField]
    private string unitName;

    [SerializeField]
    public int attackDamage = 1;
    [SerializeField]
    public int attackRange = 1;

    [SerializeField]
    private int totalHealth = 5;
    [SerializeField]
    public int currentHealth;
    //[SerializeField]
    //private GameObject unitPrefab;

    [HideInInspector]
    public Queue<int> movementQueue;
    [HideInInspector]
    public Queue<int> combatQueue;

    [Header("Movement")]

    /// <summary>
    /// The number of tiles that this unit can move in one turn.
    /// </summary>
    [Tooltip("The number of tiles that this unit can move in one turn.")]
    [SerializeField]
    public int moveSpeed;
    /// <summary>
    /// The number of moves that this unit has remaining in any one turn.
    /// </summary>
    [NonSerialized]
    private float remainingMoves;
    /// <summary>
    /// The speed at which this unit will move from one tile to another.
    /// </summary>
    [Tooltip("The speed at which this unit will move from one tile to another.")]
    [SerializeField]
    private float lerpSpeed;

    [NonSerialized]
    private Transform startPoint;
    [NonSerialized]
    private Transform endPoint;
    [NonSerialized]
    private float journeyLength;
    [NonSerialized]
    private bool isTravelling;
    [HideInInspector]
    public MovementState movementState;

    [Header("Animation & Particles")]

    [SerializeField]
    private GameObject mesh;
    [SerializeField]
    public Animator animator;

    [SerializeField]
    public GameObject particleDamage;

    [Header("UI")]

    [SerializeField]
    private Canvas healthBarCanvas;
    [SerializeField]
    private TMP_Text healthText;
    [SerializeField]
    public Image healthBar;

    [SerializeField]
    private Canvas damageCanvas;
    [SerializeField]
    private TMP_Text damageText;
    [SerializeField]
    private Image damageBar;

    [Header("Map Grid Position")]

    /// <summary>
    /// The unit's position on the map grid's X axis.
    /// This is different to its position in worldspace.
    /// </summary>
    [Tooltip("The unit's position on the map grid's X axis.")]
    [SerializeField]
    public int tileX;
    /// <summary>
    /// The unit's position on the map grid's Z axis.
    /// This is different to its position in worldspace.
    /// </summary>
    [Tooltip("The unit's position on the map grid's Z axis.")]
    [SerializeField]
    public int tileZ;

    [HideInInspector]
    public GameObject occupiedTile;

    /// <summary>
    /// The map grid on which this unit is moving.
    /// </summary>
    [Tooltip("The map grid on which this unit is moving.")]
    [HideInInspector]
    public MapManager map;

    [Header("Pathfinding")]

    /// <summary>
    /// The list of pathfinding <see cref="Node"/>s that the unit will move through to reach its destination.
    /// </summary>
    public List<Node> currentPath;
    public List<Node> movementPath;
    public bool moveCompleted = false;

    #endregion


    #region Unit Functions

    private void Awake()
    {
        //Reset the unit's pathfinding information.
        currentPath = null;
        movementPath = null;
        movementQueue = new Queue<int>();
        combatQueue = new Queue<int>();

        //Convert the unit's position in worldspace to its position on the map grid.
        tileX = (int)transform.position.x;
        tileZ = (int)transform.position.z;

        movementState = MovementState.Unselected;
        currentHealth = totalHealth;
        healthText.SetText(currentHealth.ToString());

        //animator = mesh.GetComponent<Animator>();
    }

    private void Update()
    {
        //This if statement draws debug information about the unit's current pathfinding data.
        if(currentPath != null)
        {
            int currNode = 0;

            //While the unit's current Node is less than the amount of Nodes in its current pathfinding data...
            while(currNode < currentPath.Count - 1)
            {
                //Set the start of the DrawLine segment to be the X and Z positions of the unit's current Node.
                Vector3 start = map.GetTileWorldSpace(currentPath[currNode].x, currentPath[currNode].z)
                    //Add to the Y axis so the DrawLine appears above the map.
                    + new Vector3(0, 0.51f, 0);
                //Set the end of the DrawLine segment to be the X and Z positions of the unit's next Node in its current path.
                Vector3 end = map.GetTileWorldSpace(currentPath[currNode + 1].x, currentPath[currNode + 1].z)
                    //Add to the Y axis so the DrawLine appears above the map.
                    + new Vector3(0, 0.51f, 0);

                //Draw the line segment from the unit's current Node to the unit's next Node in its current path.
                Debug.DrawLine(start, end, Color.red);

                //Check the next Node in the unit's current path.
                currNode++;
            }
        }

        //Checks if the unit has transformed its position in worldspace close enough to its current position on the map grid
        if (Vector3.Distance(transform.position, map.GetTileWorldSpace(tileX, tileZ)) < 0.1f)
            //If so, the unit can continue to advance to new tiles and find new paths
            AdvancePathfinding();

        //Lerp the unit's position in worldspace to its current position on the map grid
        transform.position = Vector3.Lerp(transform.position, map.GetTileWorldSpace(tileX, tileZ), lerpSpeed * Time.deltaTime);
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// This function advances a unit's pathfinding progress by one map grid tile.
    /// </summary>
    private void AdvancePathfinding()
    {
        //If the unit has no path or remaining moves, exit the function.
        if (currentPath == null || remainingMoves <= 0)
            return;

        //Ensure that the unit's position in wordspace is the same as its map grid position.
        transform.position = map.GetTileWorldSpace(tileX, tileZ);

        //Calculate the unit's remaining moves, based on its current Node and the cost of moving into the next Node in its current path.
        remainingMoves -= map.CostToEnterTile(currentPath[0].x, currentPath[0].z, currentPath[1].x, currentPath[1].z);

        //Set the unit's map grid position to the next Node in its current path.
        tileX = currentPath[1].x;
        tileZ = currentPath[1].z;

        //Remove the old "current" Node from the unit's path.
        currentPath.RemoveAt(0);

        //If there is only one remaining map grid tile in the unit's current path, that is the unit's current position.
        if (currentPath.Count == 1)
            //Therefore, clear the unit's current pathfinding data.
            currentPath = null;
    }

    public void AdvanceNextTile()
    {
        if (currentPath.Count == 0)
            return;
        //else
            //StartCoroutine(MoveForSeconds());
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        //UpdateHealthUI();
    }

    public void Die()
    {
        if (mesh.activeSelf)
        {
            //StartCoroutine(());
        }
    }

    /// <summary>
    /// This function should be called when the unit is to complete its movement turn.
    /// </summary>
    public void NextTurn()
    {
        //While the unit has a path and remaining moves avaiable...
        while (currentPath != null && remainingMoves > 0)
            //Ensure that the unit is at the correct map grid position.
            AdvancePathfinding();

        //At the end of the unit's turn, reset its remaining moves.
        remainingMoves = moveSpeed;
    }

    #endregion
}

public enum MovementState
{
    Unselected,
    Selected,
    Moved,
    Waiting
}