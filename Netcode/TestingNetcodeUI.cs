using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestingNetcodeUI : NetworkBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;
    public GameObject eventSystem;

    private void Awake()
    {
        startHostButton.onClick.AddListener(() => {
            Debug.Log("HOST");
            NetworkManager.Singleton.StartHost();
            eventSystem.SetActive(false);
        });
        startClientButton.onClick.AddListener(() =>
        {
            Debug.Log("CLIENT");
            NetworkManager.Singleton.StartClient();
            eventSystem.SetActive(false);
        });
    }
}