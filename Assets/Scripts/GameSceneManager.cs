using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("The VolumeManager script.")]
    [SerializeField]
    private VolumeManager volumeManager;

    public void ReloadLevel()
    {
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        volumeManager.SavePlayerPrefs();
        Application.Quit();
    }
}
