using AYellowpaper.SerializedCollections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

public class GoldManager : NetworkBehaviour
{
    public static GoldManager Instance { get; private set; }

    public float TerritoryGeneratorDelay; 
    public byte TerritoryGeneratorIncrement; 
    public SerializedDictionary<ulong, short> PlayerGoldMap = new();
    [SerializeField] private TextMeshProUGUI GoldTMP;
    [SerializeField] private Animator GoldUIAnimator;
    [SerializeField] private GameEvent OnGoldChanged;
    [SerializeField] private SfxData NoGoldSfx;
    [SerializeField] private short StartingGold = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (!PlayerGoldMap.ContainsKey(id))
                    PlayerGoldMap[id] = StartingGold;
            }
        }

        GoldTMP.text = StartingGold.ToString();
        OnGoldChanged.Raise();
    }

    public short GetGold(ulong playerID)
    {
        return PlayerGoldMap[playerID];
    }

    public bool UpdateGold(ulong clientId, short amount)
    {
        if (!IsServer || clientId == ulong.MaxValue) return false;
        
        if (PlayerGoldMap[clientId] + amount < 0)
        {
            PlayNoGoldFxClientRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return false;
        }
    
        PlayerGoldMap[clientId] += amount;
        var newGoldAmount = PlayerGoldMap[clientId];
        OnGoldChanged.Raise();

        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            GoldTMP.text = newGoldAmount.ToString();
        }
        else
        {
            UpdateGoldTextClientRpc(newGoldAmount, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        return true;
    }
    
    [Rpc(SendTo.SpecifiedInParams)]
    private void PlayNoGoldFxClientRpc(RpcParams rpcParams)
    {
        GoldUIAnimator.Play("NotEnoughGold");
        SoundFXManager.Instance.PlaySfx(NoGoldSfx, Vector2.zero);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void UpdateGoldTextClientRpc(short newGold, RpcParams rpcParams)
    {
        GoldTMP.text = newGold.ToString();
        PlayerGoldMap[NetworkManager.LocalClientId] = newGold;
        OnGoldChanged.Raise();
    }
}