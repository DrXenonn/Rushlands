using System;
using System.Collections;
using Netcode.Transports.Facepunch;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class SteamManager : NetworkBehaviour
{
    [SerializeField] private TMP_InputField LobbyIDInputField;
    [SerializeField] private TextMeshProUGUI LobbyID;
    [SerializeField] private GameObject MainMenu;
    [SerializeField] private GameObject InLobbyMenu;
    [SerializeField] private GameEvent OnHostStartButtonClicked;
    [SerializeField] private GameObject LoadingScreen;

    private void Start()
    {
        if (GameObject.FindGameObjectsWithTag("LobbySaver").Length > 1)
        {
            ResetGameCompletely();
        }
    }

    private void OnEnable()
    {
        SteamMatchmaking.OnLobbyCreated += LobbyCreated;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += GameLobbyJoinRequested;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;
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

    private void LobbyEntered(Lobby lobby)
    {
        LobbySaver.Instance.CurrentLobby = lobby;
        LobbyID.text = lobby.Id.ToString();
        CheckUI();

        if (NetworkManager.Singleton.IsHost) return;
        NetworkManager.Singleton.gameObject.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
        NetworkManager.Singleton.StartClient();
    }

    private async void GameLobbyJoinRequested(Lobby lobby, SteamId steamID)
    {
        await lobby.Join();
    }

    public async void HostLobby()
    {
        await SteamMatchmaking.CreateLobbyAsync(6);
    }

    public async void JoinLobbyWithID()
    {
        ulong id;
        if (!ulong.TryParse(LobbyIDInputField.text, out id)) return;

        Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();

        foreach (var lobby in lobbies)
        {
            if (lobby.Id == id)
            {
                await lobby.Join();
                return;
            }
        }
    }
    
    public void ResetGameCompletely()
    {
        SceneManager.LoadScene("Empty");

        StartCoroutine(ReloadGameAfterClearing());
    }

    private IEnumerator ReloadGameAfterClearing()
    {
        yield return null;

        var temp = new GameObject("Temp");
        DontDestroyOnLoad(temp);
        Scene dontDestroyScene = temp.scene;

        foreach (GameObject go in dontDestroyScene.GetRootGameObjects())
        {
            Destroy(go);
        }

        yield return null;
        SceneManager.LoadScene("MainMenu");
    }
    
    public void CopyID()
    {
        var t = new TextEditor
        {
            text = LobbyID.text
        };

        t.SelectAll();
        t.Copy();
    }

    public void LeaveLobby()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            KickClientsRpc();
        }
        
        LobbySaver.Instance.CurrentLobby?.Leave();
        LobbySaver.Instance.CurrentLobby = null;
        NetworkManager.Singleton.Shutdown();
        CheckUI();
        ResetGameCompletely();
    }

    [Rpc(SendTo.NotServer)]
    private void KickClientsRpc()
    {
        LeaveLobby();
    }
    
    private void CheckUI()
    {
        if (LobbySaver.Instance.CurrentLobby == null)
        {
            MainMenu.SetActive(true);
            InLobbyMenu.SetActive(false);
        }
        else
        {
            MainMenu.SetActive(false);
            InLobbyMenu.SetActive(true);
        }
    }

    public void StartGameServer()
    {
        if (!IsServer) return;
        StartCoroutine(nameof(OnStart));
    }

    private IEnumerator OnStart()
    {
        LoadingScreen.SetActive(true);
        yield return null;
        EnableLoadingScreenRpc();
        yield return null;
        OnHostStartButtonClicked.Raise();
        yield return null;
        
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    [Rpc(SendTo.Everyone)]
    private void EnableLoadingScreenRpc()
    {
        LoadingScreen.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}