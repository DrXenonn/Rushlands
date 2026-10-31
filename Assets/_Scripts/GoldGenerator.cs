using System.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(2)]
[RequireComponent(typeof(Node))]
public class GoldGenerator : NetworkBehaviour
{
    [SerializeField] private GoldVfx GoldVfx;
    private Node _node;
    private WaitForSeconds _waitForSeconds;
    
    private void Awake()
    {
        _node = GetComponent<Node>();
    }

    public override void OnNetworkSpawn()
    {
        if (!_node.IsServer)
        {
            enabled = false;
        }
    }

    public void SwitchGen(bool state)
    {
        StopAllCoroutines();

        if (!state) return;
        var delay = _node.RunTimeSettings.GetGoldGenDelay(); 
        if (delay <= 0) return;
        _waitForSeconds = new WaitForSeconds(delay);
        StartCoroutine(nameof(GoldGenCoroutine));
    }
    
    private IEnumerator GoldGenCoroutine()
    {
        while (true)
        {
            yield return _waitForSeconds;
            AddGold(1 ,false);
        }
    }

    public void AddGold(byte amount, bool territoryBonus)
    {
        if (_node.OwnerClientId == ulong.MaxValue) return;
        GoldManager.Instance.UpdateGold(_node.OwnerClientId, amount);
        
        if (_node.OwnerClientId == NetworkManager.ServerClientId)
        {
            PlayEffects(amount, territoryBonus);
        }
        else
        {
            PlayVFXRpc(amount, territoryBonus, RpcTarget.Single(_node.OwnerClientId, RpcTargetUse.Temp));
        }
    }

    private void PlayEffects(byte amount, bool territoryBonus)
    {
        var goldVfx = Instantiate(GoldVfx, _node.Visuals.GetGoldVFXTransform());
        goldVfx.transform.SetLocalPositionAndRotation(Vector3.zero, quaternion.identity);
        goldVfx.Init(amount, territoryBonus);
    }


    [Rpc(SendTo.SpecifiedInParams)]
    private void PlayVFXRpc(byte amount, bool territoryBonus, RpcParams rpcParams)
    {
        PlayEffects(amount, territoryBonus);
    }
}