using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Local_NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject InLobbyMenu, MainMenu;
    
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        InLobbyMenu.SetActive(true);
        MainMenu.SetActive(false);
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        InLobbyMenu.SetActive(true);
        MainMenu.SetActive(false);
    }

    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}