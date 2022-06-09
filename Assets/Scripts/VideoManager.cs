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
    private Resolution[] _resolutions = null;

    [NonSerialized]
    private int resolutionIndex = 0;
    //[NonSerialized]
    //private List<string> resolutionStrings = new List<string>();

    private void Awake()
    {
        // Clear dropdown.
        resolutionDropdown.ClearOptions();

        // Get a list of all resolutions.
        _resolutions = Screen.resolutions;

        Resolution[] resolutions = Screen.resolutions;
        List<Resolution> uniqueRes = new List<Resolution>();
        List<string> options = new List<string>();
        int currentResolution = 0;

        // Cache current resolution for use in comparison.
        //Resolution currentResolution = Screen.currentResolution;

        // Generate list of resolution strings.
        for (int i = 0; i < _resolutions.Length; i++)
        {
            string option = _resolutions[i].ToString();
            option = option.Replace("@", "");
            option = option.Substring(0, option.Length - 6);

            if (!options.Contains(option))
            {
                options.Add(option);
                uniqueRes.Add(resolutions[i]);
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolution;
        resolutionDropdown.RefreshShownValue();
        _resolutions = uniqueRes.ToArray();

        //resolutionDropdown.SetValueWithoutNotify(resolutionIndex);

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
        Screen.SetResolution(_resolutions[resolutionIndex].width,
            _resolutions[resolutionIndex].height,
            fullScreenToggle.isOn);
            //_resolutions[resolutionIndex].refreshRate);
        //resolutionIndex = resolutionStrings.IndexOf(Screen.currentResolution.ToString());
    }
}
