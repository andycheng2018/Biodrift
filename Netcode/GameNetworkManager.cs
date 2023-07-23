using UnityEngine;
using Steamworks;
using TMPro;
using Steamworks.Data;
using UnityEngine.EventSystems;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameNetworkManager : MonoBehaviour
{
    public TMP_InputField LobbyIDInputField;
    public TextMeshProUGUI LobbyID;
    public GameObject mainMenu;
    public GameObject lobbyMenu;
    public GameObject joinMenu;
    public TMP_InputField messageInputField;
    public TextMeshProUGUI messageTemplate;
    public GameObject messagesContainer;
    public GameObject playerTextObject;
    public GameObject playerTextContainer;
    public Lobby currentLobby;
    public List<SteamId> playerIds = new List<SteamId>();
    public int maxPlayers = 4;

    private void Start()
    {
        messageTemplate.text = "";
    }

    private void OnEnable()
    {
        SteamMatchmaking.OnLobbyCreated += LobbyCreated;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += LobbyMemberLeave;
        SteamFriends.OnGameLobbyJoinRequested += GameLobbyJoinRequested;
        SteamMatchmaking.OnChatMessage += ChatSent;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= LobbyMemberLeave;
        SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;
        SteamMatchmaking.OnChatMessage -= ChatSent;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ToggleChatBox();
        }
    }

    private async void GameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        await lobby.Join();
    }
   
    private void LobbyEntered(Lobby lobby)
    {
        LobbySaver.instance.currentLobby = lobby;
        LobbyID.text = lobby.Id.ToString();
        CheckUI();
        if (!NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.gameObject.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
            NetworkManager.Singleton.StartClient();
        }
        AddMessageToBox(SteamClient.Name + " entered the lobby");
        AddPlayerText();
    }

    private void LobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK)
        {
            lobby.SetPublic();
            lobby.SetJoinable(true);
            NetworkManager.Singleton.StartHost();
        }
    }

    public async void HostGame()
    {
        await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
    }

    public async void JoinLobbyWithID()
    {
        ulong ID;
        if (!ulong.TryParse(LobbyIDInputField.text, out ID))
        {
            return;
        }

        Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();

        foreach (Lobby lobby in lobbies)
        {
            if (lobby.Id == ID)
            {
                await lobby.Join();
                return;
            }
        }
    }

    public void CopyID()
    {
        TextEditor textEditor = new TextEditor();
        textEditor.text = LobbyID.text;
        textEditor.SelectAll();
        textEditor.Copy();
    }

    public void LeaveLobby()
    {
        LobbySaver.instance.currentLobby?.Leave();
        LobbySaver.instance.currentLobby = null;
        NetworkManager.Singleton.Shutdown();
        CheckUI();
        ClearChat();
    }

    private void CheckUI()
    {
        if (LobbySaver.instance.currentLobby == null)
        {
            mainMenu.SetActive(true);
            lobbyMenu.SetActive(false);
            joinMenu.SetActive(false);
        } else
        {
            mainMenu.SetActive(false);
            lobbyMenu.SetActive(true);
            joinMenu.SetActive(false);
        }
    }

    public void StartGameServer()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            LobbySaver.instance.SaveVariables();
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }

    private void ToggleChatBox()
    {
        if (messageInputField.gameObject.activeSelf)
        {
            if (!string.IsNullOrEmpty(messageInputField.text))
            {
                LobbySaver.instance.currentLobby?.SendChatString(messageInputField.text);
                messageInputField.text = "";
            }

            messageInputField.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        } else
        {
            messageInputField.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(messageInputField.gameObject);
        }
    }

    private void ChatSent(Lobby lobby, Friend friend, string msg)
    {
        AddMessageToBox(friend.Name + ": " + msg);
    }

    private void AddMessageToBox(string msg)
    {
        GameObject message = Instantiate(messageTemplate.gameObject, messagesContainer.transform);
        message.GetComponent<TextMeshProUGUI>().text = msg;
    }

    public static Texture2D GetTextureFromImage(Steamworks.Data.Image image)
    {
        Texture2D texture = new Texture2D((int)image.Width, (int)image.Height);

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var p = image.GetPixel(x, y);
                texture.SetPixel(x, (int)image.Height - y, new UnityEngine.Color(p.r / 255.0f, p.g / 255.0f, p.b / 255.0f, p.a / 255.0f));
            }
        }
        texture.Apply();
        return texture;
    }

    public async void AssignPlayerData(GameObject f, SteamId id)
    {
        var img = await SteamFriends.GetLargeAvatarAsync(id);
        f.GetComponentInChildren<TMP_Text>().text = SteamClient.Name;
        f.GetComponentInChildren<RawImage>().texture = GetTextureFromImage(img.Value);
    }

    private void AddPlayerText()
    {
        if (!playerIds.Contains(SteamClient.SteamId))
        {
            playerIds.Add(SteamClient.SteamId);
        }

        for (int i = playerTextContainer.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(playerTextContainer.transform.GetChild(i).gameObject);
        }

        foreach (SteamId id in playerIds)
        {
            GameObject playerText = Instantiate(playerTextObject, playerTextContainer.transform);
            AssignPlayerData(playerText, id);
        }
    }

    private void LobbyMemberJoined(Lobby lobby, Friend friend)
    {
        AddMessageToBox(friend.Name + " joined the lobby");
        AddPlayerText();
    }

    private void LobbyMemberLeave(Lobby lobby, Friend friend)
    {
        AddMessageToBox(friend.Name + " left the lobby");
        playerIds.Remove(SteamClient.SteamId);
        for (int i = playerTextContainer.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(playerTextContainer.transform.GetChild(i).gameObject);
        }
        foreach (SteamId id in playerIds)
        {
            GameObject playerText = Instantiate(playerTextObject, playerTextContainer.transform);
            AssignPlayerData(playerText, id);
        }
    }

    private void ClearChat()
    {
        for (int i = messagesContainer.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(messagesContainer.transform.GetChild(i).gameObject);
        }

        for (int i = playerTextContainer.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(playerTextContainer.transform.GetChild(i).gameObject);
        }
    }

    public void Quit()
    {
        Application.Quit();
    } 
}