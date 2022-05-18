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

    #endregion


    #region Custom Functions

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

    /// <summary>
    /// Returns true if a unit's health has fallen to zero.
    /// </summary>
    /// <returns></returns>
    public bool CheckUnitDead()
    {
        if (currentHealth <= 0)
            return true;
        else
            return false;
    }

    public void Die()
    {
        if (mesh.activeSelf)
        {
            StartCoroutine(FadeOutUnit());
            StartCoroutine(DelayDeath());
        }
    }

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