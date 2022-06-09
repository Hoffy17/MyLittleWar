using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeManager : MonoBehaviour
{
    [Header("Audio Mixer Settings")]
    [SerializeField]
    private AudioMixer audioMixer;
    [SerializeField]
    private float volumeMultiplier;

    [Header("Music Volume")]
    [SerializeField]
    private string musicVolume;
    [SerializeField]
    private Slider musicVolumeSlider;
    [SerializeField]
    private Toggle musicMuteToggle;
    [NonSerialized]
    private bool musicSliderMuted;

    [Header("SFX Volume")]
    [SerializeField]
    private string sFXVolume;
    [SerializeField]
    private Slider sFXVolumeSlider;
    [SerializeField]
    private Toggle sFXMuteToggle;
    [NonSerialized]
    private bool sFXSliderMuted;

    private void Awake()
    {
        musicVolumeSlider.onValueChanged.AddListener(MusicVolumeSliderControls);
        musicMuteToggle.onValueChanged.AddListener(MusicMuteToggleControls);

        sFXVolumeSlider.onValueChanged.AddListener(SFXVolumeSliderControls);
        sFXMuteToggle.onValueChanged.AddListener(SFXMuteToggleControls);
    }

    private void Start()
    {
        musicVolumeSlider.value = PlayerPrefs.GetFloat(musicVolume, musicVolumeSlider.value);
        sFXVolumeSlider.value = PlayerPrefs.GetFloat(sFXVolume, sFXVolumeSlider.value);
    }

    private void Update()
    {
        if (musicVolumeSlider.value == 0f)
            musicMuteToggle.isOn = true;

        if (sFXVolumeSlider.value == 0f)
            sFXMuteToggle.isOn = true;
    }

    public void SavePlayerPrefs()
    {
        PlayerPrefs.SetFloat(musicVolume, musicVolumeSlider.value);
        PlayerPrefs.SetFloat(sFXVolume, sFXVolumeSlider.value);
    }

    private void MusicVolumeSliderControls(float volume)
    {
        if (musicVolumeSlider.value > 0.001)
        {
            audioMixer.SetFloat(musicVolume, Mathf.Log10(volume) * volumeMultiplier);
            musicSliderMuted = true;
            musicMuteToggle.isOn = false;
            musicSliderMuted = false;
        }
        else
        {
            audioMixer.SetFloat(musicVolume, -80f);
            musicSliderMuted = true;
            musicMuteToggle.isOn = true;
            musicSliderMuted = false;
        }
    }

    private void MusicMuteToggleControls(bool muted)
    {
        if (musicSliderMuted)
            return;

        if (muted)
            musicVolumeSlider.value = musicVolumeSlider.minValue;
        else
            musicVolumeSlider.value = musicVolumeSlider.maxValue;
    }

    private void SFXVolumeSliderControls(float volume)
    {
        if (sFXVolumeSlider.value > 0.001)
        {
            audioMixer.SetFloat(sFXVolume, Mathf.Log10(volume) * volumeMultiplier);
            sFXSliderMuted = true;
            sFXMuteToggle.isOn = false;
            sFXSliderMuted = false;
        }
        else
        {
            audioMixer.SetFloat(sFXVolume, -80f);
            sFXSliderMuted = true;
            sFXMuteToggle.isOn = true;
            sFXSliderMuted = false;
        }
    }

    private void SFXMuteToggleControls(bool muted)
    {
        if (sFXSliderMuted)
            return;

        if (muted)
            sFXVolumeSlider.value = sFXVolumeSlider.minValue;
        else
            sFXVolumeSlider.value = sFXVolumeSlider.maxValue;
    }
}
