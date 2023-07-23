using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class SettingsMenu : NetworkBehaviour
{
    [Header("Gameplay Settings")]
    public CinemachineFreeLook freelookCamera;
    public Slider fovSlider;
    public TMP_Text fovText;
    public Slider sensitivitySlider;
    public TMP_Text sensitivityText;
    public Toggle horizontalInvertToggle;
    public Toggle verticalInvertToggle;
    public Toggle cameraShakeToggle;

    [Header("Graphics Settings")]
    public GameObject postProcessing;
    public Slider renderDistanceSlider;
    public TMP_Text renderDistanceText;
    public Toggle postProcessingToggle;
    public Toggle antiAliasingToggle;

    [Header("Control Settings")]
    public Color normal;
    public Color selected;
    public TMP_Text forward;
    public TMP_Text backward;
    public TMP_Text left;
    public TMP_Text right;
    public TMP_Text jump;
    public TMP_Text sprint;
    public TMP_Text crouch;
    public TMP_Text roll;
    public TMP_Text interact;
    public TMP_Text inventory;

    [Header("Video Settings")]
    public Resolution[] resolutions;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown graphicsDropdown;
    public Toggle fullScreenToggle;
    public Toggle VSyncToggle;

    [Header("Audio Settings")]
    public AudioMixer masterMixer;
    public Slider masterSlider;
    public TMP_Text masterText;
    public AudioMixer musicMixer;
    public Slider musicSlider;
    public TMP_Text musicText;

    public Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();
    private GameObject currentKey;
    private int highestIndex;

    private void Awake()
    {
        //Gameplay
        fovSlider.onValueChanged.AddListener(SetFOV);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        horizontalInvertToggle.onValueChanged.AddListener(SetInvertedMouseH);
        verticalInvertToggle.onValueChanged.AddListener(SetInvertedMouseV);
        cameraShakeToggle.onValueChanged.AddListener(SetCameraShake);

        //Graphics;
        renderDistanceSlider.onValueChanged.AddListener(SetRenderDistance);
        postProcessingToggle.onValueChanged.AddListener(SetPostProcessing);
        antiAliasingToggle.onValueChanged.AddListener(SetAntiAliasing);

        //Controls
        keys.Add("Forward", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Forward", "W")));
        keys.Add("Backward", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Backward", "S")));
        keys.Add("Left", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Left", "A")));
        keys.Add("Right", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Right", "D")));
        keys.Add("Jump", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Jump", "Space")));
        keys.Add("Sprint", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Sprint", "LeftShift")));
        keys.Add("Crouch", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Crouch", "C")));
        keys.Add("Roll", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Roll", "R")));
        keys.Add("Interact", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Interact", "F")));
        keys.Add("Inventory", (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Inventory", "E")));
        forward.text = keys["Forward"].ToString();
        backward.text = keys["Backward"].ToString();
        left.text = keys["Left"].ToString();
        right.text = keys["Right"].ToString();
        jump.text = keys["Jump"].ToString();
        sprint.text = keys["Sprint"].ToString();
        crouch.text = keys["Crouch"].ToString();
        roll.text = keys["Roll"].ToString();
        interact.text = keys["Interact"].ToString();
        inventory.text = keys["Inventory"].ToString();

        //Video
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        graphicsDropdown.onValueChanged.AddListener(SetQuality);
        fullScreenToggle.onValueChanged.AddListener(SetFullScreen);
        VSyncToggle.onValueChanged.AddListener(SetVSync);

        //Resolutions
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        foreach (Resolution resolution in resolutions)
        {
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(resolution.ToString()));
        }
        int currentResolutionIndex = GetCurrentResolutionIndex();
        highestIndex = currentResolutionIndex;
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        //Audio
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVoume);

        LoadData();
    }

    //Gameplay
    private void SetFOV(float fovValue)
    {
        if (freelookCamera != null)
        {
            freelookCamera.m_Lens.FieldOfView = fovValue;
        }
        fovText.text = ((int)fovValue).ToString();
        fovSlider.value = fovValue;
    }

    private void SetSensitivity(float sensitivityValue)
    {
        if (freelookCamera != null)
        {
            freelookCamera.m_XAxis.m_MaxSpeed = 2 * sensitivityValue;
            freelookCamera.m_YAxis.m_MaxSpeed = sensitivityValue / 33;
        }
        sensitivityText.text = ((int)sensitivityValue).ToString();
        sensitivitySlider.value = sensitivityValue;
    }

    private void SetInvertedMouseH(bool horizontalInvert)
    {
        if (freelookCamera != null)
        {
            freelookCamera.m_XAxis.m_InvertInput = horizontalInvert;
        }
        horizontalInvertToggle.isOn = horizontalInvert;
    }

    private void SetInvertedMouseV(bool verticalInvert)
    {
        if (freelookCamera != null)
        {
            freelookCamera.m_YAxis.m_InvertInput = verticalInvert;
        }
        verticalInvertToggle.isOn = verticalInvert;
    }

    private void SetCameraShake(bool enableShake)
    {
        if (freelookCamera != null)
            CinemachineShake.Instance.canShake = enableShake;
        cameraShakeToggle.isOn = enableShake;
    }

    //Graphics
    private void SetRenderDistance(float renderDistance)
    {
        if (freelookCamera != null)
        {
            freelookCamera.m_Lens.FarClipPlane = renderDistance;
        }
        renderDistanceText.text = ((int)renderDistance).ToString();
        renderDistanceSlider.value = renderDistance;
    }

    private void SetPostProcessing(bool enablePostProcessing)
    {
        if (enablePostProcessing)
        {
            if (postProcessing != null)
                postProcessing.GetComponent<Volume>().enabled = true;
            postProcessingToggle.isOn = true;
        }
        else
        {
            if (postProcessing != null)
                postProcessing.GetComponent<Volume>().enabled = false;
            postProcessingToggle.isOn = false;
        }
    }

    private void SetAntiAliasing(bool enableAntiAliasing)
    {
        if (enableAntiAliasing)
        {
            if (freelookCamera != null) 
                freelookCamera.GetComponentInChildren<Camera>().allowMSAA = true;
            antiAliasingToggle.isOn = true;
        }
        else
        {
            if (freelookCamera != null)
                freelookCamera.GetComponentInChildren<Camera>().allowMSAA = false;
            antiAliasingToggle.isOn = false;
        }
    }

    //Controls
    public void ChangeKey(GameObject clicked)
    {
        if (currentKey != null)
        {
            currentKey.GetComponent<Image>().color = normal;
        }

        currentKey = clicked;
        currentKey.GetComponent<Image>().color = selected;
    }

    private void OnGUI()
    {
        if (currentKey != null)
        {
            Event e = Event.current;
            if (e.isKey)
            {
                keys[currentKey.name] = e.keyCode;
                currentKey.GetComponentInChildren<TMP_Text>().text = e.keyCode.ToString();
                currentKey.GetComponent<Image>().color = normal;
                currentKey = null;
            }
        }
    }

    //Video
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        graphicsDropdown.value = qualityIndex;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        resolutionDropdown.value = resolutionIndex;
    }

    private int GetCurrentResolutionIndex()
    {
        Resolution currentResolution = Screen.currentResolution;

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == currentResolution.width &&
                resolutions[i].height == currentResolution.height)
            {
                return i;
            }
        }

        return 0; // Return the first resolution if the current resolution is not found
    }

    public void SetFullScreen(bool enableFullScreen)
    {
        if (enableFullScreen)
        {
            Screen.fullScreen = true;
            fullScreenToggle.isOn = true;
        }
        else
        {
            Screen.fullScreen = false;
            fullScreenToggle.isOn = false;
        }
    }

    public void SetVSync(bool enableVSync)
    {
        if (enableVSync)
        {
            QualitySettings.vSyncCount = 1;
            VSyncToggle.isOn = true;
        } else
        {
            QualitySettings.vSyncCount = 0;
            VSyncToggle.isOn = false;
        }
    }

    //Audio
    public void SetMasterVolume(float volume)
    {
        masterMixer.SetFloat("volume", Mathf.Log10(volume) * 20);
        masterText.text = ((int)volume).ToString();
        masterSlider.value = volume;
    }

    public void SetMusicVoume(float volume)
    {
        musicMixer.SetFloat("volume", Mathf.Log10(volume) * 20);
        musicText.text = ((int)volume).ToString();
        musicSlider.value = volume;
    }

    //Save/Load
    int boolToInt(bool val)
    {
        if (val)
            return 1;
        else
            return 0;
    }

    bool intToBool(int val)
    {
        if (val != 0)
            return true;
        else
            return false;
    }

    public void SaveData()
    {
        //Gameplay
        PlayerPrefs.SetFloat("FOV", fovSlider.value);
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        PlayerPrefs.SetInt("InvertedMouseH", boolToInt(horizontalInvertToggle.isOn));
        PlayerPrefs.SetInt("InvertedMouseV", boolToInt(verticalInvertToggle.isOn));
        PlayerPrefs.SetInt("CameraShake", boolToInt(cameraShakeToggle.isOn));

        //Graphics
        PlayerPrefs.SetFloat("RenderDistance", renderDistanceSlider.value);
        PlayerPrefs.SetInt("PostProcessing", boolToInt(postProcessingToggle.isOn));
        PlayerPrefs.SetInt("AntiAliasing", boolToInt(antiAliasingToggle.isOn));

        //Controls
        foreach (var key in keys)
        {
            PlayerPrefs.SetString(key.Key, key.Value.ToString());
        }

        //Video
        PlayerPrefs.SetInt("Resolution", resolutionDropdown.value);
        PlayerPrefs.SetInt("Graphics", graphicsDropdown.value);
        PlayerPrefs.SetInt("FullScreen", boolToInt(fullScreenToggle.isOn));
        PlayerPrefs.SetInt("VSync", boolToInt(VSyncToggle.isOn));

        //Audio
        PlayerPrefs.SetFloat("MasterVolume", masterSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);

        PlayerPrefs.Save();

        if (SceneManager.GetActiveScene().name == "Game")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            gameObject.GetComponent<Canvas>().enabled = false;
        }
    }

    public void ResetData()
    {
        //Gameplay
        SetFOV(60);
        SetSensitivity(250);
        SetInvertedMouseH(false);
        SetInvertedMouseV(false);
        SetCameraShake(true);

        //Graphics
        SetRenderDistance(250);
        SetPostProcessing(true);
        SetAntiAliasing(true);

        //Controls
        keys["Forward"] = KeyCode.W;
        keys["Backward"] = KeyCode.S;
        keys["Left"] = KeyCode.A;
        keys["Right"] = KeyCode.D;
        keys["Jump"] = KeyCode.Space;
        keys["Sprint"] = KeyCode.LeftShift;
        keys["Crouch"] = KeyCode.C;
        keys["Roll"] = KeyCode.R;
        keys["Interact"] = KeyCode.F;
        keys["Inventory"] = KeyCode.E;
        forward.text = keys["Forward"].ToString();
        backward.text = keys["Backward"].ToString();
        left.text = keys["Left"].ToString();
        right.text = keys["Right"].ToString();
        jump.text = keys["Jump"].ToString();
        sprint.text = keys["Sprint"].ToString();
        crouch.text = keys["Crouch"].ToString();
        roll.text = keys["Roll"].ToString();
        interact.text = keys["Interact"].ToString();
        inventory.text = keys["Inventory"].ToString();

        //Video
        SetResolution(highestIndex);
        SetQuality(2);
        SetFullScreen(true);
        SetVSync(true);

        //Audio
        SetMasterVolume(5);
        SetMusicVoume(5);

        SaveData();
    }

    public void LoadData()
    {
        if (PlayerPrefs.HasKey("FOV"))
        {
            //Gameplay
            SetFOV(PlayerPrefs.GetFloat("FOV"));
            SetSensitivity(PlayerPrefs.GetFloat("Sensitivity"));
            SetInvertedMouseH(intToBool(PlayerPrefs.GetInt("InvertedMouseH", 0)));
            SetInvertedMouseV(intToBool(PlayerPrefs.GetInt("InvertedMouseV", 0)));
            SetCameraShake(intToBool(PlayerPrefs.GetInt("CameraShake", 0)));

            //Graphics
            SetRenderDistance(PlayerPrefs.GetFloat("RenderDistance"));
            SetPostProcessing(intToBool(PlayerPrefs.GetInt("PostProcessing", 0)));
            SetAntiAliasing(intToBool(PlayerPrefs.GetInt("AntiAliasing", 0)));

            //Video
            SetResolution(PlayerPrefs.GetInt("Resolution", 0));
            SetQuality(PlayerPrefs.GetInt("Graphics", 0));
            SetFullScreen(intToBool(PlayerPrefs.GetInt("FullScreen", 0)));
            SetVSync(intToBool(PlayerPrefs.GetInt("VSync", 0)));

            //Audio
            SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume"));
            SetMusicVoume(PlayerPrefs.GetFloat("MusicVolume"));
        } else
        {
            ResetData();   
        }
    }

    public void Menu()
    {
        SceneManager.LoadScene("Menu");
        NetworkManager.Singleton.Shutdown();
        Cleanup();
    }

    public void Cleanup()
    {
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if (LobbySaver.instance != null)
        {
            Destroy(LobbySaver.instance.gameObject);
        }
    }
}