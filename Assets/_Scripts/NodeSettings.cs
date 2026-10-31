using UnityEngine;

[CreateAssetMenu(fileName = "Node Settings", menuName = "Node Settings")]
public class NodeSettings : ScriptableObject
{
    public byte ID;
    public string Name;
    public NodeVisuals NodeVisuals;
    public Sprite Icon;
    public short MaxUnits;
    public short StartingUnits;
    public float UnitsPerSecond;
    public float GoldPerSecond;
    public float AttackPerSecond;
    public short Range;
    public short BaseDamage;
    public short Armor;
    public short Price;
    public bool IsBallista;
    public bool IsMainBuilding;

    public NodeSettings DemolishedNode;
    public NodeSettings[] UpgradeOptions;
    public SfxData SelectionSfx;

    public float GetUnitGenDelay()
    {
        return UnitsPerSecond == 0 ? -1 : 1f / UnitsPerSecond;
    }

    public float GetGoldGenDelay()
    {
        return GoldPerSecond == 0 ? -1 : 1f / GoldPerSecond;
    }

    public float GetAttackDelay()
    {
        return AttackPerSecond == 0 ? -1 : 1f / AttackPerSecond;
    }
}