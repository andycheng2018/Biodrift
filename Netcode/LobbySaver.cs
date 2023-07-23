using Steamworks.Data;
using TMPro;
using UnityEngine;

public class LobbySaver : MonoBehaviour
{
    public Lobby? currentLobby;
    public static LobbySaver instance;
    public TMP_InputField seedInputField;
    public TMP_Dropdown difficultyDropdown;
    public int seedInt;
    public int difficultyInt;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }

    public void SaveVariables()
    {
        int.TryParse(seedInputField.text, out seedInt);
        difficultyInt = difficultyDropdown.value;
    }
}
