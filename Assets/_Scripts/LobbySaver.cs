using Steamworks.Data;
using UnityEngine;

public class LobbySaver : MonoBehaviour
{
    public Lobby? CurrentLobby;
    public static LobbySaver Instance;
    
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}