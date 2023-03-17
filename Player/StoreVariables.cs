using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoreVariables : MonoBehaviour
{
    public TMP_InputField seedInputField;
    public TMP_Dropdown difficultyDropdown;
    public Slider slider;
    public TMP_Dropdown graphicsDropdown;
    public int seedInt;
    public int difficultyInt;
    public float sliderValue;
    public int graphicsValue;

    void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    public void SaveVariables()
    {
        int.TryParse(seedInputField.text, out seedInt);
        difficultyInt = difficultyDropdown.value;
        sliderValue = slider.value;
        graphicsValue = graphicsDropdown.value;
    }
}
