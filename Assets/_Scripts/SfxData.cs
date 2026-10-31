using UnityEngine;

[CreateAssetMenu(fileName = "SfxData", menuName = "SfxData")]
public class SfxData : ScriptableObject
{
    public AudioClip Clip;
    public bool _3D = true;
    [Range(0, 1)] public float Volume = 0.5f;
    [Range(-3, 3)] public float MinPitch = 1;
    [Range(-3, 3)] public float MaxPitch = 1;
    public float MinDistance = 1f;
    public float MaxDistance = 500f;
}