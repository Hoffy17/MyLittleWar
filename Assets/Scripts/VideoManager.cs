using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoManager : MonoBehaviour
{
    [Header("Video Settings - UI")]
    [SerializeField]
    private Toggle fullScreenToggle;
    [SerializeField]
    private TMP_Dropdown resolutionDropdown;
    [SerializeField]
    private Button setResolution;

    [NonSerialized]
    private Resolution[] resolutions;
    [NonSerialized]
    private int resolutionIndex = 0;
    [NonSerialized]
    private List<string> resolutionStrings = new List<string>();

    private void Awake()
    {
        // Get a list of all resolutions.
        resolutions = Screen.resolutions;

        // Cache current resolution for use in comparison.
        Resolution currentResolution = Screen.currentResolution;

        // Generate list of resolution strings.
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].ToString();
            string optionWithoutAt = option.Replace("@", "");
            string optionWithoutHz = optionWithoutAt.Substring(0, optionWithoutAt.Length - 6);
            string optionTrimmed = optionWithoutHz.Trim();

            if (!resolutionStrings.Contains(optionTrimmed))
                resolutionStrings.Add(optionTrimmed);

            // Set the current resolution index because the current iteration is the same as the current resolution.
            if (resolutions[i].width == currentResolution.width &&
                resolutions[i].height == currentResolution.height &&
                resolutions[i].refreshRate == currentResolution.refreshRate)
                resolutionIndex = i;
        }

        // Clear dropdown.
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutionStrings);
        resolutionDropdown.SetValueWithoutNotify(resolutionIndex);

        // Add set resolution index to the dropdown.
        resolutionDropdown.onValueChanged.AddListener(SetResolutionIndex);
        // Add update resolution to the button.
        setResolution.onClick.AddListener(UpdateResolution);

        // Add set fullscreen to the toggle.
        fullScreenToggle.onValueChanged.AddListener(ToggleFullScreen);
        fullScreenToggle.isOn = Screen.fullScreen;
    }

    private void ToggleFullScreen(bool state)
    {
        Screen.fullScreen = state;
    }

    private void SetResolutionIndex(int index)
    {
        resolutionIndex = index;
    }

    private void UpdateResolution()
    {
        Screen.SetResolution(resolutions[resolutionIndex].width,
            resolutions[resolutionIndex].height,
            fullScreenToggle.isOn,
            resolutions[resolutionIndex].refreshRate);
        resolutionIndex = resolutionStrings.IndexOf(Screen.currentResolution.ToString());
    }
}
