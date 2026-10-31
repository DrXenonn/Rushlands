using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerCardSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject PlayerCard;

    public override void OnNetworkDespawn()
    {
        foreach (var networkObject in GetComponentsInChildren<NetworkObject>().ToList())
        {
            Destroy(networkObject.gameObject);
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnection;
            StartCoroutine(SpawnPlayerCard(NetworkManager.Singleton.LocalClientId));
        }
        else
        {
            enabled = false;
        }
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnection;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnection;
    }

    private void OnConnection(ulong clientId)
    {
        StartCoroutine(SpawnPlayerCard(clientId));
    }

    private void OnDisconnection(ulong clientId)
    {
        foreach (var networkObject in GetComponentsInChildren<NetworkObject>())
        {
            if (networkObject.OwnerClientId == clientId && networkObject != null)
            {
                networkObject.Despawn();
                Destroy(networkObject.gameObject);
                break;
            }
        }
    }

    private IEnumerator SpawnPlayerCard(ulong clientId)
    {
        yield return new WaitForSeconds(0.5f);
        var playerCard = Instantiate(PlayerCard);
        playerCard.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
    }
}
