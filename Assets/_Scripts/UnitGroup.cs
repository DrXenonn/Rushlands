using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class UnitGroup : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI UnitCount, BoatUnitCount;
    [SerializeField] private NavMeshAgent NavMeshAgent;
    [SerializeField] private GameEvent OnUnitGroupSpawned;
    [SerializeField] private MeshRenderer[] Renderers;
    [SerializeField] private GameObject[] LittleFlags;
    [SerializeField] private Collider Collider;
    [SerializeField] private Transform LittleFlagsParent;
    [SerializeField] private GameObject Banner;
    [SerializeField] private GameObject Boat;
    [SerializeField] private Animator BoatAnimator;
    [SerializeField] private Animator BannerAnimator;
    private UnitGroupData _data;
    private readonly NetworkVariable<int> _count = new();

    private void Awake()
    {
        NavMeshAgent.updateRotation = false;
    }

    public override void OnNetworkSpawn()
    {
        _count.OnValueChanged += UpdateCount;

        if (!IsServer)
        {
            enabled = false;
        }
        else
        {
            var dir = (transform.position - _data.Target.position).normalized;
            NavMeshAgent.enabled = true;
            _count.Value = _data.Count;
            InitLittleFlags(dir);
            InitClientRpc(_data.Color, dir);
        }

        UpdateCount(-1, _count.Value);
    }

    public void OnEliminate(Component sender, object data)
    {
        if (!IsServer) return;
        var (loserId, conquerorId) = (Tuple<ulong, ulong>)data;
        var ownerId = GetOwnerId();

        if (loserId == ownerId)
        {
            HandleDeSpawnAnimationRpc();
            NetworkObject.Despawn();
        }
    }

    private void UpdateCount(int previousvalue, int newvalue)
    {
        UnitCount.text = newvalue.ToString();
        BoatUnitCount.text = newvalue.ToString();
        HandleLittleFlagsSize();
    }

    public void Init(UnitGroupData data)
    {
        var color = ColorManager.Instance.Colors[data.Color];
        foreach (var r in Renderers)
        {
            r.material = color.Material;
        }

        OnUnitGroupSpawned.Raise(NavMeshAgent, data.Target);
        _data = data;
        Collider.enabled = true;
        BoatUnitCount.color = color.UnitTextColor;
    }

    private void InitLittleFlags(Vector3 dir)
    {
        var newPosition = transform.position + (dir * 4);
        newPosition.y = LittleFlagsParent.transform.position.y;

        LittleFlagsParent.transform.position = newPosition;

        var targetRotation = Quaternion.LookRotation(-dir);
        LittleFlagsParent.rotation = targetRotation;
    }


    public void ApplyDamage(short amount)
    {
        if (_count.Value - amount < 1)
        {
            HandleDeSpawnAnimationRpc();
            NetworkObject.Despawn();
        }
        else
        {
            _count.Value -= amount;
        }
    }

    private void HandleLittleFlagsSize()
    {
        switch (_count.Value)
        {
            case <= 5:
                LittleFlags[0].SetActive(false);
                LittleFlags[1].SetActive(false);
                LittleFlags[2].SetActive(false);
                LittleFlags[3].SetActive(false);
                LittleFlags[4].SetActive(false);
                break;
            case <= 10:
                LittleFlags[1].SetActive(false);
                LittleFlags[2].SetActive(false);
                LittleFlags[3].SetActive(false);
                LittleFlags[4].SetActive(false);
                break;
            case <= 15:
                LittleFlags[2].SetActive(false);
                LittleFlags[3].SetActive(false);
                LittleFlags[4].SetActive(false);
                break;
            case <= 25:
                LittleFlags[3].SetActive(false);
                LittleFlags[4].SetActive(false);
                break;
            case <= 35:
                LittleFlags[4].SetActive(false);
                break;
        }
    }

    public ulong GetOwnerId()
    {
        return PlayerManager.Instance.GetIDWithColor(_data.Color);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Node") && other.transform == _data.Target)
        {
            other.gameObject.GetComponent<Node>().Trade(_data.Color, _count.Value);
            NetworkObject.Despawn();
        }
        else if (other.CompareTag("Water"))
        {
            ChangeModel(true);
            ChangeModelClientRpc(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            ChangeModel(false);
            ChangeModelClientRpc(false);
        }
    }

    private void ChangeModel(bool isShip)
    {
        if (isShip)
        {
            Banner.SetActive(false);
            NavMeshAgent.updateRotation = true;
            Boat.SetActive(true);
        }
        else
        {
            Boat.SetActive(false);
            NavMeshAgent.updateRotation = false;
            transform.rotation = Quaternion.identity;
            Banner.SetActive(true);
        }
    }

    [Rpc(SendTo.NotServer)]
    private void ChangeModelClientRpc(bool isShip)
    {
        if (isShip)
        {
            ChangeModel(true);
        }
        else
        {
            ChangeModel(false);
        }
    }

    [Rpc(SendTo.NotServer)]
    public void InitClientRpc(PlayerColor playerColor, Vector3 dir)
    {
        var color = ColorManager.Instance.Colors[playerColor];
        foreach (var r in Renderers)
        {
            r.material = color.Material;
        }

        BoatUnitCount.color = color.UnitTextColor;
        InitLittleFlags(dir);
    }

    [Rpc(SendTo.Everyone)]
    private void HandleDeSpawnAnimationRpc()
    {
        if (Banner.activeSelf)
        {
            BannerAnimator.transform.SetParent(null, true);
            BannerAnimator.Play("BannerFall");
            Destroy(BannerAnimator.gameObject, 10f);
        }
        else
        {
            BoatAnimator.transform.SetParent(null, true);
            BoatAnimator.Play("BoatSink");
            Destroy(BoatAnimator.gameObject, 4f);
        }
    }
}