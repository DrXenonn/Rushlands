using Unity.Netcode;
using UnityEngine;

public class BallistaAnimationSyncer : MonoBehaviour
{
    public static BallistaAnimationSyncer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}