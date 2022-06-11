using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls full screen and resolution settings.
/// </summary>
public class VideoManager : MonoBehaviour
{
    #region Declarations

    [Header("Video Settings - UI")]
    [Tooltip("Toggles the application's between full screen and windowed mode.")]
    [SerializeField]
    private Toggle fullScreenToggle;
    [Tooltip("Displays a list of resolutions that can be selected.")]
    [SerializeField]
    private TMP_Dropdown resolutionDropdown;
    [Tooltip("A button to apply a resolution setting after it has been selected.")]
    [SerializeField]
    private Button setResolution;

    [Tooltip("An array of resolutions that have been detected, to be displayed in a dropdown menu.")]
    [NonSerialized]
    private Resolution[] resolutions = null;
    [Tooltip("The current index of the resolution that has been selected in the dropdown menu.")]
    [NonSerialized]
    private int resolutionIndex = 0;

    #endregion


    #region Unity Functions

    private void Awake()
    {
        // Clear dropdown.
        resolutionDropdown.ClearOptions();

        // Get an array of all available resolutions, and store it in two different arrays.
        resolutions = Screen.resolutions;
        Resolution[] resolutionsTemp = Screen.resolutions;

        // Create a list of resolutions that are unique to the user's device.
        List<Resolution> uniqueRes = new List<Resolution>();
        // Create a list of strings that are printed to the resolution dropdown menu.
        List<string> options = new List<string>();

        // Cache current resolution for use in comparison.
        Resolution currentResolution = Screen.currentResolution;

        // Generate a list of resolution strings.
        for (int i = 0; i < resolutions.Length; i++)
        {
            // Shorten the string to remove unnecessary info.
            string option = resolutions[i].ToString();
            option = option.Replace("@", "");
            option = option.Substring(0, option.Length - 6);

            // If the list of strings doesn't contain the option, add it.
            if (!options.Contains(option))
            {
                options.Add(option);

                // Additionally, start to build the list of resolutions unique to the user's device.
                uniqueRes.Add(resolutionsTemp[i]);

                // If that unique resolution is the same as the user's resolution, set the index for the dropdown menu.
                if (uniqueRes[i].width == currentResolution.width &&
                    uniqueRes[i].height == currentResolution.height)
                    resolutionIndex = i;
            }
        }

        // Add the list of string to the dropdown menu.
        resolutionDropdown.AddOptions(options);

        // Copy the list of unique resolutions into the array of resolutions.
        resolutions = uniqueRes.ToArray();

        // Set the automatic selection of the dropdown menu and refresh.
        resolutionDropdown.value = resolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Add set resolution index to the dropdown.
        resolutionDropdown.onValueChanged.AddListener(SetResolutionIndex);
        // Add update resolution to the button.
        setResolution.onClick.AddListener(UpdateResolution);

        // Add set fullscreen to the toggle.
        fullScreenToggle.onValueChanged.AddListener(ToggleFullScreen);
        fullScreenToggle.isOn = Screen.fullScreen;
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// Toggles the application between full screen and windowed mode, based on the state of the toggle.
    /// </summary>
    /// <param name="state">The state of the toggle UI element.</param>
    private void ToggleFullScreen(bool state)
    {
        Screen.fullScreen = state;
    }

    /// <summary>
    /// Sets the selected resolution of its dropdown menu.
    /// </summary>
    /// <param name="index">The index of the resolution element in its dropdown menu.</param>
    private void SetResolutionIndex(int index)
    {
        resolutionIndex = index;
    }

    /// <summary>
    /// Applies the selected resolution.
    /// </summary>
    private void UpdateResolution()
    {
        Screen.SetResolution(resolutions[resolutionIndex].width,
            resolutions[resolutionIndex].height,
            fullScreenToggle.isOn);
    }

    #endregion
}
