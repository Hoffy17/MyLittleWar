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
    [HideInInspector]
    public bool moveCompleted = false;

    #endregion


    #region Unit Functions

    private void Awake()
    {
        // Reset the unit's pathfinding information.
        path = null;
        movementQueue = new Queue<int>();
        combatQueue = new Queue<int>();

        // Convert the unit's position in worldspace to its position on the map grid.
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
            StartCoroutine(MoveOverTime(transform.gameObject, path[path.Count - 1]));
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

    /// <summary>
    /// Rotates the unit as it moves between tiles on its movement path.
    /// </summary>
    /// <param name="startTile">The map grid position of the previous tile in the unit's path. This is being repeatedly updated in the <see cref="MoveOverTime(GameObject, Node)"/> coroutine.</param>
    private void RotateUnitMoving(Vector2 startTile)
    {
        for (int i = 0; i < path.Count; i++)
        {
            // If the next node in the unit's path is not the final node...
            if ((i + 1) != path.Count)
            {
                // Get the previous, current and next tile positions in the path.
                Vector2 prevTile = startTile;
                Vector2 currTile = new Vector2(path[0].x, path[0].z);
                Vector2 nextTile = new Vector2(path[1].x, path[1].z);

                // Calculate the vectors between those positions.
                Vector2 prevToCurrVector = VectorDirection(prevTile, currTile);
                Vector2 currToNextVector = VectorDirection(currTile, nextTile);

                // Rotate the unit.
                if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.right)
                    mesh.transform.rotation = Quaternion.Euler(0, 270, 0);
                else if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.up)
                    mesh.transform.rotation = Quaternion.Euler(0, 180, 0);
                else if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.down)
                    mesh.transform.rotation = Quaternion.Euler(0, 270, 0);
                else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.left)
                    mesh.transform.rotation = Quaternion.Euler(0, 90, 0);
                else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.up)
                    mesh.transform.rotation = Quaternion.Euler(0, 90, 0);
                else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.down)
                    mesh.transform.rotation = Quaternion.Euler(0, 0, 0);
                else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.up)
                    mesh.transform.rotation = Quaternion.Euler(0, 0, 0);
                else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.right)
                    mesh.transform.rotation = Quaternion.Euler(0, 0, 0);
                else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.left)
                    mesh.transform.rotation = Quaternion.Euler(0, 270, 0);
                else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.down)
                    mesh.transform.rotation = Quaternion.Euler(0, 0, 0);
                else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.right)
                    mesh.transform.rotation = Quaternion.Euler(0, 90, 0);
                else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.left)
                    mesh.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            // If the next node in the unit's path is the final node...
            else if ((i + 1) == path.Count)
            {
                // Get the previous and current tile positions in the path.
                Vector2 prevTile = startTile;
                Vector2 currTile = new Vector2(path[0].x, path[0].z);

                // Calculate the vector between those positions.
                Vector2 prevToCurrVector = VectorDirection(prevTile, currTile);

                // Rotate the unit.
                if (prevToCurrVector == Vector2.right)
                    mesh.transform.rotation = Quaternion.Euler(0, 90, 0);
                else if (prevToCurrVector == Vector2.left)
                    mesh.transform.rotation = Quaternion.Euler(0, 270, 0);
                else if (prevToCurrVector == Vector2.up)
                    mesh.transform.rotation = Quaternion.Euler(0, 0, 0);
                else if (prevToCurrVector == Vector2.down)
                    mesh.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }
    }

    /// <summary>
    /// Rotates the unit as it attacks, or is attacked by, another unit.
    /// </summary>
    /// <param name="attackerTile">The map grid position of the attacker.</param>
    /// <param name="defenderTile">The map grid position of the defender.</param>
    public void RotateUnitAttacking(Vector2 attackerTile, Vector2 defenderTile)
    {
        // Calculate the vector between the attacker and defender.
        Vector2 unitDirection = VectorDirection(attackerTile, defenderTile);

        // Rotate the unit.
        if (unitDirection == Vector2.right)
            mesh.transform.rotation = Quaternion.Euler(0, 90, 0);
        else if (unitDirection == Vector2.left)
            mesh.transform.rotation = Quaternion.Euler(0, 270, 0);
        else if (unitDirection == Vector2.up)
            mesh.transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (unitDirection == Vector2.down)
            mesh.transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    private Vector2 VectorDirection(Vector2 currVector, Vector2 nextVector)
    {
        Vector2 vectorDirection = (nextVector - currVector).normalized;

        if (vectorDirection == Vector2.right)
            return Vector2.right;
        else if (vectorDirection == Vector2.left)
            return Vector2.left;
        else if (vectorDirection == Vector2.up)
            return Vector2.up;
        else if (vectorDirection == Vector2.down)
            return Vector2.down;
        else
            return new Vector2();
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

    #endregion


    #region Coroutines

    private IEnumerator MoveOverTime(GameObject unit, Node endNode)
    {
        movementQueue.Enqueue(1);

        Vector2 startTile = new Vector2(path[0].x, path[0].z);

        // Remove the selected unit's first node in its movement path.
        path.RemoveAt(0);

        // While the selected unit's movement path includes more than one node...
        while (path.Count != 0)
        {
            // Get the worldspace position of the next node in the unit's path.
            Vector3 nextNode = map.GetTileWorldSpace(path[0].x, path[0].z);

            // Lerp the unit from its current worldspace position, to the position of the next node in its path.
            unit.transform.position = Vector3.Lerp(transform.position, nextNode, lerpSpeed);
            RotateUnitMoving(startTile);

            startTile = new Vector2(path[0].x, path[0].z);

            // When the unit gets close enough to the next node, remove that node from its path.
            if ((transform.position - nextNode).sqrMagnitude < 0.001)
                path.RemoveAt(0);

            // Wait and return to the top of the loop.
            yield return new WaitForEndOfFrame();
        }

        lerpSpeed = 0.15f;

        // Set the unit's worldspace position as the position of the final node in its path.
        transform.position = map.GetTileWorldSpace(endNode.x, endNode.z);
        tileX = endNode.x;
        tileZ = endNode.z;

        // Reset the unit's currently occupied tile.
        occupiedTile.GetComponent<Tile>().unitOccupyingTile = null;
        occupiedTile = map.mapTiles[tileX, tileZ];

        movementQueue.Dequeue();
    }

    public IEnumerator DisplayDamage(int damage)
    {
        combatQueue.Enqueue(1);

        damageText.SetText(damage.ToString());
        damageCanvas.enabled = true;

        for (float f = 3f; f >= -0.01f; f -= 0.01f)
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