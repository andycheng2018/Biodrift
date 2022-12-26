using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public TMP_Dropdown resolutionDropdown;
    public Canvas settingsCanvas;
    public Canvas endgameCanvas;
    public static bool isPause;

    Resolution[] resolutions;

    private void Start()
    {
        QualitySettings.SetQualityLevel(2);

        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPause)
            {
                Cursor.lockState = CursorLockMode.None;
                settingsCanvas.enabled = true;
                Time.timeScale = 0;
                isPause = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                settingsCanvas.enabled = false;
                Time.timeScale = 1;
                isPause = false;
            }
        }
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume", Mathf.Log10(volume) * 20);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void Unpause()
    {
        Cursor.lockState = CursorLockMode.Locked;
        settingsCanvas.enabled = false;
        Time.timeScale = 1;
        isPause = false;
    }

    public void Menu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void endGamePause()
    {
        Cursor.lockState = CursorLockMode.None;
        endgameCanvas.enabled = true;
        Time.timeScale = 0;
        isPause = true;
    }

    public void endGameResume()
    {
        Cursor.lockState = CursorLockMode.Locked;
        endgameCanvas.enabled = false;
        Time.timeScale = 1;
        isPause = false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
