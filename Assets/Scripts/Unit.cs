using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The public class for each unit on the map, including infantry and tanks.
/// </summary>
public class Unit : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The AudioManager script.")]
    [SerializeField]
    public AudioManager audioManager;

    [Header("Unit Data")]
    [Tooltip("The team that this unit belongs to, where 0 is blue team and 1 is red team.")]
    [SerializeField]
    public int teamNumber;
    [Tooltip("The image displayed when the cursor highlights this unit.")]
    [SerializeField]
    public Sprite portrait;
    [Tooltip("The name displayed when the cursor highlights this unit.")]
    [SerializeField]
    public string unitName;
    [Tooltip("The amount of damage this unit inflicts on an enemy unit during an attack.")]
    [SerializeField]
    public int attackDamage = 1;
    [Tooltip("The range within which this unit can attack an enemy unit.")]
    [SerializeField]
    public int attackRange = 1;
    [Tooltip("This unit's maximum health points.")]
    [SerializeField]
    private int totalHealth = 5;
    [Tooltip("This unit's current health points at any time, which are reduced when this unit is attacked.")]
    [HideInInspector]
    public int currentHealth;
    [Tooltip("A queue used to keep track of whether this unit is currently moving.")]
    [HideInInspector]
    public Queue<int> movementQueue;
    [Tooltip("A queue used to keep track of whether this unit is currently attacking.")]
    [HideInInspector]
    public Queue<int> combatQueue;

    [Header("Movement")]
    [Tooltip("The amount of movement currency that this unit can spend to move in one turn.")]
    [SerializeField]
    public int moveSpeed;
    [Tooltip("The default speed at which this unit will move from one tile to another.")]
    [SerializeField]
    public float lerpSpeed;
    [Tooltip("The current speed at which this unit will move from one tile to another.")]
    [HideInInspector]
    public float lerpSpeedCurrent;
    [Tooltip("The speed at which this unit will move from one tile to another after right-clicking.")]
    [SerializeField]
    public float lerpSpeedFast;
    [Tooltip("This unit's state referring to its movement during one turn, including unselected, selected, moved or waiting.")]
    [HideInInspector]
    public MovementState movementState;

    [Header("Graphics")]
    [Tooltip("The game object that contains the mesh renderer and material of this unit.")]
    [SerializeField]
    private GameObject mesh;
    [Tooltip("The animator component used to animate this unit.")]
    [HideInInspector]
    public Animator animator;
    [Tooltip("This unit's default material, containing a texture with its team colour.")]
    [SerializeField]
    public Material unitMat;
    [Tooltip("The material that is set when this unit at the end of this unit's turn, containing a greyed texture.")]
    [SerializeField]
    private Material unitMatWait;
    [Tooltip("The game object containing a particle system, which plays when this unit attacks an enemy unit.")]
    [SerializeField]
    public GameObject particleDamage;

    [Header("UI")]
    [Tooltip("The canvas object containing this unit's health bar, which should be a child of this game object.")]
    [SerializeField]
    private Canvas healthBarCanvas;
    [Tooltip("The text displayed on this unit's health bar, showing its current health points.")]
    [SerializeField]
    private TMP_Text healthText;
    [Tooltip("The sprite image that is filled according to this unit's current health points.")]
    [SerializeField]
    public Image healthBar;
    [Tooltip("The canvas object containing this unit's damage indicator, which should be a child of this game object.")]
    [SerializeField]
    private Canvas damageCanvas;
    [Tooltip("The text displayed when this unit takes damage, showing how many health points were deducted.")]
    [SerializeField]
    private TMP_Text damageText;
    [Tooltip("The sprite image that is displayed when this unit takes damage.")]
    [SerializeField]
    private Image damageSprite;
    [Tooltip("The background sprite image that is displayed when this unit takes damage.")]
    [SerializeField]
    private Image damageBackground;

    [Header("Map Grid Position")]
    [Tooltip("This unit's position on the map grid's X axis.")]
    [SerializeField]
    public int tileX;
    [Tooltip("This unit's position on the map grid's Z axis.")]
    [SerializeField]
    public int tileZ;
    [Tooltip("The tile game object that this unit is currently occupying.")]
    [HideInInspector]
    public GameObject occupiedTile;
    [Tooltip("The map grid on which this unit is moving.")]
    [HideInInspector]
    public MapManager map;

    [Header("Pathfinding")]
    [Tooltip("The list of pathfinding nodes that the unit will move through to reach its destination.")]
    [HideInInspector]
    public List<Node> path;
    [Tooltip("Checks whether this unit has finished its current movement turn.")]
    [HideInInspector]
    public bool moveCompleted = false;

    #endregion


    #region Unit Functions

    private void Awake()
    {
        // Reset the unit's pathfinding information.
        path = null;

        // Empty the movement and combat queues, as a unit is not currently moving or attacking.
        movementQueue = new Queue<int>();
        combatQueue = new Queue<int>();

        // Get the unit's position on the map grid by converting its position in worldspace.
        tileX = (int)transform.position.x;
        tileZ = (int)transform.position.z;

        // Reset the unit's movement state and current health.
        movementState = MovementState.Unselected;
        currentHealth = totalHealth;
        healthText.SetText(currentHealth.ToString());

        animator = mesh.GetComponent<Animator>();

        // Reset the unit's lerp speed.
        lerpSpeedCurrent = lerpSpeed;
    }

    private void LateUpdate()
    {
        // Have the unit's health and damage canvases face the camera.
        healthBarCanvas.transform.forward = Camera.main.transform.forward;
        damageCanvas.transform.forward = Camera.main.transform.forward;
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// If this unit has nodes to travel to on its movement path, start moving. 
    /// </summary>
    public void Move()
    {
        if (path.Count == 0)
            return;
        else
            StartCoroutine(MoveOverTime(transform.gameObject, path[path.Count - 1]));
    }

    /// <summary>
    /// Deduct damage from this unit's current health.
    /// </summary>
    /// <param name="damage">The amount of damage to deduct from this unit's current health.</param>
    public void TakeDamage(int damage)
    {
        currentHealth -= Mathf.Clamp(damage, 0, currentHealth);
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
    public bool CheckDead()
    {
        if (currentHealth <= 0)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Call coroutines to destroy a unit that has no remaining health points.
    /// </summary>
    public void Die()
    {
        if (mesh.activeSelf)
        {
            //StartCoroutine(FadeOutUnit());
            StartCoroutine(DelayDeath());
        }
    }

    /// <summary>
    /// Rotates the unit as it moves between tiles on its movement path.
    /// </summary>
    /// <param name="startTile">The map grid position of the previous tile in the unit's path. This is being repeatedly updated in the <see cref="MoveOverTime(GameObject, Node)"/> coroutine.</param>
    private void RotateMoving(Vector2 startTile)
    {
        for (int i = 0; i < path.Count; i++)
        {
            // If the next node in the unit's path is not the final node...
            if ((i + 1) != path.Count)
            {
                // Get the previous, current and next tile positions in the unit's path.
                Vector2 prevTile = startTile;
                Vector2 currTile = new Vector2(path[0].x, path[0].z);
                Vector2 nextTile = new Vector2(path[1].x, path[1].z);

                // Calculate the vectors between those tiles.
                Vector2 prevToCurrVector = VectorDirection(prevTile, currTile);
                Vector2 currToNextVector = VectorDirection(currTile, nextTile);

                // Rotate the unit.
                // This is bad code, and should really be done with a 2D lookup table.
                // It's also using floating point equality which is prone to bugs.
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
    public void RotateAttacking(Vector2 attackerTile, Vector2 defenderTile)
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

    /// <summary>
    /// Returns a vector direction between two nodes in this unit's path.
    /// </summary>
    /// <param name="currVector">The vector direction of a node in this unit's path.</param>
    /// <param name="nextVector">The vector direction of the next node in this unit's path.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Toggle the unit's health bar on and off.
    /// </summary>
    public void ToggleHealthBar()
    {
        if (healthBarCanvas.enabled)
            healthBarCanvas.enabled = false;
        else
            healthBarCanvas.enabled = true;
    }

    /// <summary>
    /// Updates this unit's UI health bar to reflect its current health points.
    /// </summary>
    public void UpdateHealthUI()
    {
        healthBar.fillAmount = (float)currentHealth / totalHealth;
        healthText.SetText(currentHealth.ToString());
    }

    #endregion


    #region Animations & Effects

    /// <summary>
    /// Set this unit's animation state to idle.
    /// </summary>
    public void SetAnimIdle()
    {
        animator.SetTrigger("Idle");
    }

    /// <summary>
    /// Set this unit's animation state to selected.
    /// </summary>
    public void SetAnimSelected()
    {
        animator.SetTrigger("Selected");
    }

    /// <summary>
    /// Set this unit's animation state to moving.
    /// </summary>
    public void SetAnimMoving()
    {
        animator.SetTrigger("Moving");
    }

    /// <summary>
    /// Set this unit's animation state to attacking.
    /// </summary>
    public void SetAnimAttacking()
    {
        animator.SetTrigger("Attacking");

        PlayParticlesAttack();
    }

    public void PlayParticlesAttack()
    {
        particleDamage.GetComponent<ParticleSystem>().Play();
    }

    #endregion


    #region Coroutines

    /// <summary>
    /// Controls this unit's position translating between one or more tiles over time.
    /// </summary>
    /// <param name="unit">The unit that is moving on a movement path.</param>
    /// <param name="endNode">The final node in this unit's current movement path.</param>
    /// <returns></returns>
    private IEnumerator MoveOverTime(GameObject unit, Node endNode)
    {
        // Fill the movement queue, to ensure that other actions cannot take place until this unit has finished moving.
        movementQueue.Enqueue(1);

        // Record the map grid position of this unit's current tile, before it is removed from the list of nodes in its movement path.
        Vector2 currTile = new Vector2(path[0].x, path[0].z);

        // Remove this unit's first node in its movement path.
        path.RemoveAt(0);

        // While the selected unit's movement path includes more than one node...
        while (path.Count != 0)
        {
            // Get the worldspace positions of the current and next nodes in the unit's path.
            Vector3 currNode = map.GetTileWorldSpace((int)currTile.x, (int)currTile.y);
            Vector3 nextNode = map.GetTileWorldSpace(path[0].x, path[0].z);

            // If the unit is close enough to its current node, play a sound and rotate the unit.
            if ((transform.position - currNode).sqrMagnitude < 0.001)
            {
                RotateMoving(currTile);
                audioManager.PlayMoveSFX(unitName);
            }

            // Lerp the unit from its current worldspace position, to the position of the next node in its path.
            unit.transform.position = Vector3.Lerp(transform.position, nextNode, lerpSpeedCurrent * Time.deltaTime);

            // Re-record the map grid position of the unit's current tile, before it is removed.
            currTile = new Vector2(path[0].x, path[0].z);

            // When the unit gets close enough to the next node, remove its previous node from its path.
            if ((transform.position - nextNode).sqrMagnitude < 0.001)
                path.RemoveAt(0);

            // Wait and return to the top of the loop.
            yield return new WaitForEndOfFrame();
        }

        lerpSpeedCurrent = lerpSpeed;

        // Set the unit's worldspace position as the position of the final node in its path.
        transform.position = map.GetTileWorldSpace(endNode.x, endNode.z);
        tileX = endNode.x;
        tileZ = endNode.z;

        // Reset the unit's currently occupied tile.
        occupiedTile.GetComponent<Tile>().unitOccupyingTile = null;
        occupiedTile = map.mapTiles[tileX, tileZ];

        // Now that this unit has finished moving, emptying the movement queue.
        movementQueue.Dequeue();
    }

    /// <summary>
    /// Display the amount of damage this unit took after being attacked by an enemy unit.
    /// </summary>
    /// <param name="damage">The amount of damage this unit took.</param>
    /// <returns></returns>
    public IEnumerator DisplayDamage(int damage)
    {
        // Fill the combat queue to ensure the game cannot progress until this coroutine has finished.
        combatQueue.Enqueue(1);

        // Enable this unit's damage canvas and update the amount of damage.
        damageCanvas.enabled = true;
        damageText.SetText(damage.ToString());

        // Over time, fade out the unit's damage taken.
        for (float f = 3f; f >= -0.01f; f -= 3f * Time.deltaTime)
        {
            Color barColour = damageSprite.GetComponent<Image>().color;
            Color textColour = damageText.color;

            barColour.a = f;
            textColour.a = f;

            damageSprite.GetComponent<Image>().color = barColour;
            damageBackground.GetComponent<Image>().color = barColour;
            damageText.color = textColour;

            yield return new WaitForEndOfFrame();
        }

        // Now that the coroutine is finished, empty the combat queue.
        combatQueue.Dequeue();
    }

    /// <summary>
    /// Over time, fade out the unit's renderer upon death.
    /// </summary>
    /// <returns></returns>
    //public IEnumerator FadeOutUnit()
    //{
    //    // This coroutine doesn't work as intended, and so it is not called currently.
    //    combatQueue.Enqueue(1);
    //    Renderer rend = GetComponentInChildren<Renderer>();

    //    for (float f = 1f; f >= .05; f -= 0.01f)
    //    {
    //        Color colour = rend.material.color;
    //        colour.a = f;
    //        rend.material.color = colour;
    //        yield return new WaitForEndOfFrame();
    //    }

    //    combatQueue.Dequeue();
    //}

    /// <summary>
    /// Wait until the combat queue is empty before destroying a dead unit's game object.
    /// </summary>
    /// <returns></returns>
    private IEnumerator DelayDeath()
    {
        // Wait for this unit to stop attacking.
        while (combatQueue.Count > 0)
            yield return new WaitForEndOfFrame();

        Destroy(gameObject);
    }

    #endregion
}

/// <summary>
/// Enumerator states used to keep track of a unit's movement state.
/// </summary>
public enum MovementState
{
    Unselected,
    Selected,
    Moved,
    Waiting
}