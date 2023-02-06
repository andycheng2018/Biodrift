using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoreVariables : MonoBehaviour
{
    public TMP_InputField seedInputField;
    public TMP_Dropdown biomeDropdown;
    public int seedInt;
    public int biomeInt;

    void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }

    public void SaveVariables()
    {
        int.TryParse(seedInputField.text, out seedInt);
        biomeInt = biomeDropdown.value;
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Singleplayer");
    }
}
