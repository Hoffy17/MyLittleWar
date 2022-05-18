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
    public Sprite portrait;
    [SerializeField]
    public string unitName;
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

    [Tooltip("The number of tiles that this unit can move in one turn.")]
    [SerializeField]
    public int moveSpeed;
    [Tooltip("The number of moves that this unit has remaining in any one turn.")]
    [NonSerialized]
    private float remainingMoves;
    [Tooltip("The speed at which this unit will move from one tile to another.")]
    [SerializeField]
    public float lerpSpeed;

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

    [Header("Graphics")]

    [SerializeField]
    private GameObject mesh;
    [HideInInspector]
    public Animator animator;

    [SerializeField]
    public Material unitMat;
    [SerializeField]
    private Material unitMatWait;

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

    [Tooltip("The unit's position on the map grid's X axis.")]
    [SerializeField]
    public int tileX;
    [Tooltip("The unit's position on the map grid's Z axis.")]
    [SerializeField]
    public int tileZ;

    [HideInInspector]
    public GameObject occupiedTile;

    [Tooltip("The map grid on which this unit is moving.")]
    [HideInInspector]
    public MapManager map;

    [Header("Pathfinding")]

    [Tooltip("The list of pathfinding nodes that the unit will move through to reach its destination.")]
    public List<Node> path;
    public List<Node> movementPath;
    [HideInInspector]
    public bool moveCompleted = false;

    #endregion


    #region Unit Functions

    private void Awake()
    {
        //Reset the unit's pathfinding information.
        path = null;
        movementPath = null;
        movementQueue = new Queue<int>();
        combatQueue = new Queue<int>();

        //Convert the unit's position in worldspace to its position on the map grid.
        tileX = (int)transform.position.x;
        tileZ = (int)transform.position.z;

        movementState = MovementState.Unselected;
        currentHealth = totalHealth;
        healthText.SetText(currentHealth.ToString());

        animator = mesh.GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        healthBarCanvas.transform.forward = Camera.main.transform.forward;
        damageCanvas.transform.forward = Camera.main.transform.forward;
    }

    //private void Update()
    //{
    //    //This if statement draws debug information about the unit's current pathfinding data.
    //    if(currentPath != null)
    //    {
    //        int currNode = 0;

    //        //While the unit's current Node is less than the amount of Nodes in its current pathfinding data...
    //        while(currNode < currentPath.Count - 1)
    //        {
    //            //Set the start of the DrawLine segment to be the X and Z positions of the unit's current Node.
    //            Vector3 start = map.GetTileWorldSpace(currentPath[currNode].x, currentPath[currNode].z)
    //                //Add to the Y axis so the DrawLine appears above the map.
    //                + new Vector3(0, 0.51f, 0);
    //            //Set the end of the DrawLine segment to be the X and Z positions of the unit's next Node in its current path.
    //            Vector3 end = map.GetTileWorldSpace(currentPath[currNode + 1].x, currentPath[currNode + 1].z)
    //                //Add to the Y axis so the DrawLine appears above the map.
    //                + new Vector3(0, 0.51f, 0);

    //            //Draw the line segment from the unit's current Node to the unit's next Node in its current path.
    //            Debug.DrawLine(start, end, Color.red);

    //            //Check the next Node in the unit's current path.
    //            currNode++;
    //        }
    //    }

    //    //Checks if the unit has transformed its position in worldspace close enough to its current position on the map grid
    //    if (Vector3.Distance(transform.position, map.GetTileWorldSpace(tileX, tileZ)) < 0.1f)
    //        //If so, the unit can continue to advance to new tiles and find new paths
    //        AdvancePathfinding();

    //    //Lerp the unit's position in worldspace to its current position on the map grid
    //    transform.position = Vector3.Lerp(transform.position, map.GetTileWorldSpace(tileX, tileZ), lerpSpeed * Time.deltaTime);
    //}

    #endregion


    #region Custom Functions

    /// <summary>
    /// This function advances a unit's pathfinding progress by one map grid tile.
    /// </summary>
    //private void AdvancePathfinding()
    //{
    //    //If the unit has no path or remaining moves, exit the function.
    //    if (currentPath == null || remainingMoves <= 0)
    //        return;

    //    //Ensure that the unit's position in wordspace is the same as its map grid position.
    //    transform.position = map.GetTileWorldSpace(tileX, tileZ);

    //    //Calculate the unit's remaining moves, based on its current Node and the cost of moving into the next Node in its current path.
    //    remainingMoves -= map.CostToEnterTile(currentPath[0].x, currentPath[0].z, currentPath[1].x, currentPath[1].z);

    //    //Set the unit's map grid position to the next Node in its current path.
    //    tileX = currentPath[1].x;
    //    tileZ = currentPath[1].z;

    //    //Remove the old "current" Node from the unit's path.
    //    currentPath.RemoveAt(0);

    //    //If there is only one remaining map grid tile in the unit's current path, that is the unit's current position.
    //    if (currentPath.Count == 1)
    //        //Therefore, clear the unit's current pathfinding data.
    //        currentPath = null;
    //}

    public void AdvanceNextTile()
    {
        if (path.Count == 0)
            return;
        else
            StartCoroutine(MoveOverTime(transform.gameObject, path[path.Count -1]));
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHealthUI();
    }

    /// <summary>
    /// Changes the unit's material, to show that it has finished its turn.
    /// </summary>
    public void Wait()
    {
        gameObject.GetComponentInChildren<Renderer>().material = unitMatWait;
    }

    public void Die()
    {
        if (mesh.activeSelf)
        {
            StartCoroutine(FadeOutUnit());
            StartCoroutine(DelayDeath());
        }
    }

    /// <summary>
    /// This function should be called when the unit is to complete its movement turn.
    /// </summary>
    //public void NextTurn()
    //{
    //    //While the unit has a path and remaining moves avaiable...
    //    while (currentPath != null && remainingMoves > 0)
    //        //Ensure that the unit is at the correct map grid position.
    //        AdvancePathfinding();

    //    //At the end of the unit's turn, reset its remaining moves.
    //    remainingMoves = moveSpeed;
    //}

    public void UpdateHealthUI()
    {
        healthBar.fillAmount = (float)currentHealth / totalHealth;
        healthText.SetText(currentHealth.ToString());
    }

    #endregion


    #region Animations

    public void SetAnimIdle()
    {
        animator.SetTrigger("Idle");
    }

    public void SetAnimSelected()
    {
        animator.SetTrigger("Selected");
    }

    public void SetAnimMoving()
    {
        animator.SetTrigger("Moving");
    }

    public void SetAnimAttacking()
    {
        animator.SetTrigger("Attacking");
    }

    public void SetAnimWaiting()
    {
        animator.SetTrigger("Waiting");
    }

    #endregion


    #region IEnumerators

    private IEnumerator MoveOverTime(GameObject unit, Node endNode)
    {
        movementQueue.Enqueue(1);

        //Remove the tile that the unit is currently occupying.
        path.RemoveAt(0);

        while (path.Count != 0)
        {
            Vector3 endPos = map.GetTileWorldSpace(path[0].x, path[0].z);

            unit.transform.position = Vector3.Lerp(transform.position, endPos, lerpSpeed);

            if ((transform.position - endPos).sqrMagnitude < 0.001)
            {
                path.RemoveAt(0);
            }

            yield return new WaitForEndOfFrame();
        }

        lerpSpeed = 0.15f;
        transform.position = map.GetTileWorldSpace(endNode.x, endNode.z);

        tileX = endNode.x;
        tileZ = endNode.z;

        occupiedTile.GetComponent<Tile>().unitOccupyingTile = null;
        occupiedTile = map.mapTiles[tileX, tileZ];

        movementQueue.Dequeue();
    }

    public IEnumerator DisplayDamage(int damage)
    {
        combatQueue.Enqueue(1);

        damageText.SetText(damage.ToString());
        damageCanvas.enabled = true;

        for (float f = 2f; f >= -0.01f; f -= 0.01f)
        {
            Color barColour = damageBar.GetComponent<Image>().color;
            Color textColour = damageText.color;

            barColour.a = f;
            textColour.a = f;

            damageBar.GetComponent<Image>().color = barColour;
            damageText.color = textColour;

            yield return new WaitForEndOfFrame();
        }

        combatQueue.Dequeue();
    }

    public IEnumerator FadeOutUnit()
    {
        combatQueue.Enqueue(1);
        Renderer rend = GetComponentInChildren<Renderer>();

        for (float f = 1f; f >= .05; f -= 0.01f)
        {
            Color colour = rend.material.color;
            colour.a = f;
            rend.material.color = colour;
            yield return new WaitForEndOfFrame();
        }

        combatQueue.Dequeue();
    }

    private IEnumerator DelayDeath()
    {
        while (combatQueue.Count > 0)
            yield return new WaitForEndOfFrame();

        Destroy(gameObject);
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