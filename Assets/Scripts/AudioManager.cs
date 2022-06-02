using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Controls the playing of audio.
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The GameManager script.")]
    [SerializeField]
    private GameManager gameManager;

    [Header("Music")]
    [Tooltip("The music played at the begininng of Team 1's turn.")]
    [SerializeField]
    private AudioSource musicTeam1Fanfare;
    [Tooltip("The music played at the begininng of Team 2's turn.")]
    [SerializeField]
    private AudioSource musicTeam2Fanfare;
    [Tooltip("The music during Team 1's turn.")]
    [SerializeField]
    private AudioSource musicTeam1Turn;
    [Tooltip("The music during Team 2's turn.")]
    [SerializeField]
    private AudioSource musicTeam2Turn;
    [Tooltip("The time between when one team's background music ends and the other begins.")]
    [SerializeField]
    private float bGMTransitionDuration;
    [Tooltip("The music played when a game ends.")]
    [SerializeField]
    private AudioSource musicVictoryFanfare;

    [Header("SFX - UI & Map UI")]
    [Tooltip("The sound effect played when the cursor highlights a tile.")]
    [SerializeField]
    private AudioSource sFXHighlightTile;
    [Tooltip("The sound effect played when a selection is made.")]
    [SerializeField]
    private AudioSource sFXSelect;

    [Header("SFX - Unit")]
    [Tooltip("The sound effect played when an infantry unit moves between tiles.")]
    [SerializeField]
    private AudioSource sFXMoveInfantry;
    [Tooltip("The sound effect played when a tank unit moves between tiles.")]
    [SerializeField]
    private AudioSource sFXMoveTank;
    [Tooltip("The sound effect played when an infantry unit attacks another unit.")]
    [SerializeField]
    private AudioSource sFXAttackInfantry;
    [Tooltip("The sound effect played when a tank unit attacks another unit.")]
    [SerializeField]
    private AudioSource sFXAttackTank;

    #endregion


    #region Music

    public void PlayTeamFanfare()
    {
        if (gameManager.currentTeam == 1)
            musicTeam2Fanfare.Play();
        else if (gameManager.currentTeam == 0)
            musicTeam1Fanfare.Play();
    }

    public IEnumerator PlayTeamTurn()
    {
        musicTeam1Turn.Stop();
        musicTeam2Turn.Stop();

        yield return new WaitForSeconds(bGMTransitionDuration);

        // If it's Team 2's turn and their music isn't playing, play it.
        if (gameManager.currentTeam == 1)
        {
            if (!musicTeam2Turn.isPlaying)
                musicTeam2Turn.Play();
        }
        // If it's Team 1's turn and their music isn't playing, play it.
        else if (gameManager.currentTeam == 0)
        {
            if (!musicTeam1Turn.isPlaying)
                musicTeam1Turn.Play();
        }
    }

    public void PlayVictoryFanfare()
    {
        musicVictoryFanfare.Play();
    }

    #endregion


    #region SFX - UI & Map UI

    public void PlayHighlightTileSFX()
    {
        if (sFXHighlightTile.isPlaying)
            return;
        else
            sFXHighlightTile.Play();
    }

    public void PlaySelectUnitSFX()
    {
        sFXSelect.Play();
    }

    #endregion


    #region SFX - Unit

    public void PlayMoveSFX(string unitType)
    {
        if (unitType == "Infantry")
            sFXMoveInfantry.Play();
        else if (unitType == "Tank")
            sFXMoveTank.Play();
    }

    public void PlayAttackSFX(string unitType)
    {
        if (unitType == "Infantry")
            sFXAttackInfantry.Play();
        else if (unitType == "Tank")
            sFXAttackTank.Play();
    }

    #endregion
}
