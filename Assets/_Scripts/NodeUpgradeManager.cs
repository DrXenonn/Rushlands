using System.Collections.Generic;
using Unity.Netcode;

public class NodeUpgradeManager : NetworkBehaviour
{
    public static NodeUpgradeManager Instance { get; private set; }
    public List<NodeSettings> NodeSettingsMap = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    
    [Rpc(SendTo.Server)]
    public void NodeUpgradeReqServerRpc(ulong nodeNetworkID, byte nodeType, ulong playerID)
    {
        var nodeToUpgrade = NetworkManager.Singleton.SpawnManager.SpawnedObjects[nodeNetworkID].GetComponent<Node>();
        if (playerID != nodeToUpgrade.OwnerClientId)
        {
            OnUpgradeFinishRpc(playerID);
            return;
        }
        
        var settings = NodeSettingsMap[nodeType];
        var hasGold = GoldManager.Instance.UpdateGold(playerID, (short)-settings.Price);
        if (!hasGold)
        {
            OnUpgradeFinishRpc(playerID);
            return;
        }
        
        nodeToUpgrade.UpgradeRpc(nodeType);
    }

    [Rpc(SendTo.Everyone)]
    private void OnUpgradeFinishRpc(ulong playerId)
    {
        if (NetworkManager.LocalClientId == playerId)
        {
            NodeUIManager.Instance.OnFinishUpgrade();
        }
    }
}