using AYellowpaper.SerializedCollections;
using UnityEngine;

public enum PlayerColor
{
    None,
    Blue,
    Red,
    Yellow,
    Green,
    Orange,
    Purple,
}


[System.Serializable]
public struct Colors
{
    public Material Material;
    public Material MinimapMaterial;
    public Material BorderMaterial;
    public Material MidBorderMaterial;
    public Color UnitTextColor;
    public Color UIColor;
    public Color SelectionHighlightColor;
}

public class ColorManager : MonoBehaviour
{
    public static ColorManager Instance { get; private set; }
    public SerializedDictionary<PlayerColor, Colors> Colors = new(7);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}