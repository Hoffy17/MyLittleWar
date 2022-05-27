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

    #endregion


    #region Music

    public void PlayTeamFanfare()
    {
        if (gameManager.currentTeam == 1)
            musicTeam2Fanfare.Play();
        else if (gameManager.currentTeam == 0)
            musicTeam1Fanfare.Play();
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

    public void PlayMoveSFX()
    {
        sFXMoveInfantry.Play();
    }

    #endregion
}
