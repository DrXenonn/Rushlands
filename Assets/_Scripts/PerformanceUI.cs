using System.Collections;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PerformanceUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI PingTMP;
    private NetworkTransport _networkTransport;
    private ulong _serverId;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1);
        _networkTransport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        _serverId = NetworkManager.ServerClientId;
        
        InvokeRepeating(nameof(SlowUpdate), 0f, 3f);
    }

    private void SlowUpdate()
    {
        PingTMP.text = _networkTransport.GetCurrentRtt(_serverId) + " ms";
    }
}