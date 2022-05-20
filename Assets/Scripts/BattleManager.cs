using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Conrtols the dealing of damage between attacking and defending units.
/// </summary>
public class BattleManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The GameManager script.")]
    [SerializeField]
    private GameManager gameManager;
    [Tooltip("The CameraShake script.")]
    [SerializeField]
    private CameraShake cameraShake;

    [Tooltip("Checks whether a battle has taken place and has finished.")]
    [NonSerialized]
    private bool isBattling;

    #endregion


    #region Custom Functions

    /// <summary>
    /// Controls the logic for a unit attacking another unit, including dealing damage and dying.
    /// </summary>
    /// <param name="attacker">The unit initiating the attack.</param>
    /// <param name="defender">The unit receiving the attack.</param>
    public void Battle(GameObject attacker, GameObject defender)
    {
        isBattling = true;

        // Get the attacking unit and defending unit, and their attack damage stats.
        Unit attackerUnit = attacker.GetComponent<Unit>();
        Unit defenderUnit = defender.GetComponent<Unit>();
        int attackerDamage = attackerUnit.attackDamage;
        int defenderDamage = defenderUnit.attackDamage;

        // If the attacking and defending units have the same attack ranges...
        if(attackerUnit.attackRange == defenderUnit.attackRange)
        {
            //PlayParticles(defenderUnit);

            // The defending unit takes damage.
            defenderUnit.TakeDamage(attackerDamage);

            // Check if the defending unit dies.
            if (defenderUnit.CheckUnitDead())
            {
                DefenderDies(attacker, defender, defenderUnit);
                return;
            }

            // The attacking unit takes damage.
            attackerUnit.TakeDamage(defenderDamage);

            //Check if the attacking unit dies.
            if (attackerUnit.CheckUnitDead())
            {
                AttackerDies(attacker, defender, attackerUnit);
                return;
            }
        }
        // Otherwise, only the defending unit takes damage.
        else
        {
            //PlayParticles(defenderUnit);
            defenderUnit.TakeDamage(attackerDamage);

            if (defenderUnit.CheckUnitDead())
            {
                DefenderDies(attacker, defender, defenderUnit);
                return;
            }
        }

        isBattling = false;
    }

    /// <summary>
    /// Calculate the direction that an attack is directed at a defender.
    /// </summary>
    /// <param name="attacker">The unit initiating the attack.</param>
    /// <param name="defender">The unit receiving the attack.</param>
    /// <returns></returns>
    public Vector3 GetAttackDirection(GameObject attacker, GameObject defender)
    {
        // The start position is the attacker's transform position.
        Vector3 startPos = attacker.transform.position;
        // The end position is the defender's transform position.
        Vector3 endPos = defender.transform.position;
        // Calculate the attack direction.
        return ((endPos - startPos) / (endPos - startPos).magnitude).normalized;
    }

    /// <summary>
    /// Plays a particle effect when a unit is damaged.
    /// </summary>
    /// <param name="defenderUnit">The unit on the receiving end of an attack.</param>
    private void PlayParticles(Unit defenderUnit)
    {
        GameObject particles = Instantiate(defenderUnit.GetComponent<Unit>().particleDamage, defenderUnit.transform.position, defenderUnit.transform.rotation);
        Destroy(particles, 2f);
    }

    /// <summary>
    /// Executed when a defending unit dies as a result of an attack.
    /// </summary>
    /// <param name="attacker">The unit that initiated the attack.</param>
    /// <param name="defender">The unit that received the attack.</param>
    /// <param name="defenderUnit">The Unit script component attached to the defender.</param>
    private void DefenderDies(GameObject attacker, GameObject defender, Unit defenderUnit)
    {
        // Destroy the defender unit and its parent game object.
        defender.transform.parent = null;
        defenderUnit.Die();
        isBattling = false;
        // Check if a winner has been determined.
        StartCoroutine(gameManager.CheckVictor(attacker, defender));
    }

    /// <summary>
    /// Executed when an attacking unit dies as a result of initiating an attack.
    /// </summary>
    /// <param name="attacker">The unit that initiated the attack.</param>
    /// <param name="defender">The unit that received the attack.</param>
    /// <param name="attackerUnit">The Unit script component attached to the attacker.</param>
    private void AttackerDies(GameObject attacker, GameObject defender, Unit attackerUnit)
    {
        // Destroy the attacker unit and its parent game object.
        attacker.transform.parent = null;
        attackerUnit.Die();
        isBattling = false;
        // Check if a winner has been determined.
        StartCoroutine(gameManager.CheckVictor(attacker, defender));
    }

    #endregion


    #region Coroutines

    /// <summary>
    /// Called when a unit's attack on another unit is initiated. Controls lerping, camera shake and damage displayed.
    /// </summary>
    /// <param name="attacker">The unit that initiated the attack.</param>
    /// <param name="defender">The unit that received the attack.</param>
    /// <returns></returns>
    public IEnumerator StartAttack(GameObject attacker, GameObject defender)
    {
        isBattling = true;

        float elapsedTime = 0;

        // Get the attacker's and defender's map grid positions.
        Vector2 attackerTile = new Vector2(attacker.GetComponent<Unit>().tileX, attacker.GetComponent<Unit>().tileZ);
        Vector2 defenderTile = new Vector2(defender.GetComponent<Unit>().tileX, defender.GetComponent<Unit>().tileZ);

        // Get the attacker's and defender's transform positions as the start and end points for the attack.
        Vector3 startPos = attacker.transform.position;
        Vector3 endPos = defender.transform.position;

        // Animate the attacker's attack and have the two units face each other.
        attacker.GetComponent<Unit>().SetAnimMoving();
        defender.GetComponent<Unit>().SetAnimSelected();
        attacker.GetComponent<Unit>().RotateUnitAttacking(attackerTile, defenderTile);
        defender.GetComponent<Unit>().RotateUnitAttacking(defenderTile, attackerTile);

        while (elapsedTime < 0.25f)
        {
            // Lerp the attacker's position towards the defender.
            attacker.transform.position = Vector3.Lerp
                (startPos,
                startPos + ((endPos - startPos) / (endPos - startPos).magnitude).normalized * 0.5f,
                elapsedTime / 0.25f);
            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        while (isBattling)
        {
            // Shake the camera.
            StartCoroutine(cameraShake.ShakeCamera(0.2f, attacker.GetComponent<Unit>().attackDamage, GetAttackDirection(attacker, defender)));

            // If the attacking and defending units have the same attack range,
            // And the defender has health remaining after being attacked...
            if (attacker.GetComponent<Unit>().attackRange == defender.GetComponent<Unit>().attackRange &&
                defender.GetComponent<Unit>().currentHealth - attacker.GetComponent<Unit>().attackDamage > 0)
            {
                // Display the amount of damage that both units take as a result of the attack.
                StartCoroutine(attacker.GetComponent<Unit>().DisplayDamage(defender.GetComponent<Unit>().attackDamage));
                StartCoroutine(defender.GetComponent<Unit>().DisplayDamage(attacker.GetComponent<Unit>().attackDamage));
            }
            // Otherwise, display only the amount of damage the defending unit takes as a result of the attack.
            else
                StartCoroutine(defender.GetComponent<Unit>().DisplayDamage(attacker.GetComponent<Unit>().attackDamage));

            // Calculate damage taken and check if units have died.
            Battle(attacker, defender);

            yield return new WaitForEndOfFrame();
        }

        // If the attacking unit is still alive, finish its attack sequence.
        if (attacker != null)
            StartCoroutine(FinishAttack(attacker, defender, startPos));
    }

    /// <summary>
    /// Lerps the attacking unit back to its original transform position after attacking, and sets the unit to wait.
    /// </summary>
    /// <param name="attacker">The unit that initiated the attack.</param>
    /// <param name="returnPos">The attacker's transform position before lerping.</param>
    /// <returns></returns>
    public IEnumerator FinishAttack(GameObject attacker, GameObject defender, Vector3 returnPos)
    {
        float elapsedTime = 0;

        // Lerp the attacker's position back towards where it started.
        while (elapsedTime < 0.3f)
        {
            attacker.transform.position = Vector3.Lerp(attacker.transform.position, returnPos, elapsedTime / 0.25f);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        attacker.GetComponent<Unit>().SetAnimIdle();
        defender.GetComponent<Unit>().SetAnimIdle();
    }

    #endregion
}