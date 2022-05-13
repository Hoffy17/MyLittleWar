using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]

    [SerializeField]
    private GameManager gameManager;
    [SerializeField]
    private CameraShake cameraShake;

    [NonSerialized]
    private bool isBattling;

    #endregion


    #region Custom Functions

    /// <summary>
    /// Controls the logic for a unit attacking another unit.
    /// </summary>
    /// <param name="attacker">The unit initiating the attack.</param>
    /// <param name="defender">The unit receiving the attack.</param>
    public void Battle(GameObject attacker, GameObject defender)
    {
        isBattling = true;

        Unit attackerUnit = attacker.GetComponent<Unit>();
        Unit defenderUnit = defender.GetComponent<Unit>();

        int attackerDamage = attackerUnit.attackDamage;
        int defenderDamage = defenderUnit.attackDamage;

        if(attackerUnit.attackRange == defenderUnit.attackRange)
        {
            //PlayParticles(defenderUnit);
            defenderUnit.TakeDamage(attackerDamage);

            if (IsUnitDead(defender))
            {
                DefenderDies(attacker, defender, defenderUnit);
                return;
            }

            attackerUnit.TakeDamage(defenderDamage);

            if (IsUnitDead(attacker))
            {
                AttackerDies(attacker, defender, attackerUnit);
                return;
            }
        }
        else
        {
            //PlayParticles(defenderUnit);
            defenderUnit.TakeDamage(attackerDamage);

            if (IsUnitDead(defender))
            {
                DefenderDies(attacker, defender, defenderUnit);
                return;
            }
        }

        isBattling = false;
    }

    /// <summary>
    /// Calculate the direction that an attack is attacking a defender.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <returns></returns>
    public Vector3 GetAttackDirection(GameObject attacker, GameObject defender)
    {
        Vector3 startPos = attacker.transform.position;
        Vector3 endPos = defender.transform.position;
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
    /// Checks if a unit's health has fallen to zero.
    /// </summary>
    /// <param name="unit">The unit whose health is under scrutiny.</param>
    /// <returns></returns>
    private bool IsUnitDead(GameObject unit)
    {
        if (unit.GetComponent<Unit>().currentHealth <= 0)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Executed when a defending unit dies as a result of an attack.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <param name="defenderUnit"></param>
    private void DefenderDies(GameObject attacker, GameObject defender, Unit defenderUnit)
    {
        defender.transform.parent = null;
        defenderUnit.Die();
        isBattling = false;
        StartCoroutine(gameManager.CheckVictor(attacker, defender));
    }

    /// <summary>
    /// Executed when an attacking unit dies as a result of initiating an attack.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <param name="attackerUnit"></param>
    private void AttackerDies(GameObject attacker, GameObject defender, Unit attackerUnit)
    {
        attacker.transform.parent = null;
        attackerUnit.Die();
        isBattling = false;
        StartCoroutine(gameManager.CheckVictor(attacker, defender));
    }

    #endregion


    #region IEnumerators

    public IEnumerator StartAttack(GameObject attacker, GameObject defender)
    {
        isBattling = true;

        float elapsedTime = 0;

        Vector3 startPos = attacker.transform.position;
        Vector3 endPos = defender.transform.position;

        //attacker.GetComponent<Unit>().PlayWalkAnim();

        while (elapsedTime < 0.25f)
        {
            attacker.transform.position = Vector3.Lerp
                (startPos,
                startPos + ((endPos - startPos) / (endPos - startPos).magnitude).normalized * 0.5f,
                elapsedTime / 0.25f);
            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        while (isBattling)
        {
            StartCoroutine(cameraShake.ShakeCamera(0.2f, attacker.GetComponent<Unit>().attackDamage, GetAttackDirection(attacker, defender)));

            if (attacker.GetComponent<Unit>().attackDamage == defender.GetComponent<Unit>().attackDamage &&
                defender.GetComponent<Unit>().currentHealth - attacker.GetComponent<Unit>().attackDamage > 0)
            {
                StartCoroutine(attacker.GetComponent<Unit>().DisplayDamage(defender.GetComponent<Unit>().attackDamage));
                StartCoroutine(defender.GetComponent<Unit>().DisplayDamage(attacker.GetComponent<Unit>().attackDamage));
            }
            else
                StartCoroutine(defender.GetComponent<Unit>().DisplayDamage(attacker.GetComponent<Unit>().attackDamage));

            Battle(attacker, defender);

            yield return new WaitForEndOfFrame();
        }

        if (attacker != null)
            StartCoroutine(FinishAttack(attacker, startPos));
    }

    public IEnumerator FinishAttack(GameObject attacker, Vector3 endPos)
    {
        float elapsedTime = 0;

        while (elapsedTime < 0.3f)
        {
            attacker.transform.position = Vector3.Lerp(attacker.transform.position, endPos, elapsedTime / 0.25f);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        //attacker.GetComponent<Unit>().PlayIdleAnim();
        attacker.GetComponent<Unit>().Wait();
    }

    #endregion
}
