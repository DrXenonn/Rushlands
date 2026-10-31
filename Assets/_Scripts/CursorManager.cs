using AYellowpaper.SerializedCollections;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private SerializedDictionary<CursorType, Texture2D> CursorMap = new();
    public static CursorManager Instance;

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

    public void SetCursor(CursorType cursorType)
    {
        Cursor.SetCursor(CursorMap[cursorType], Vector2.zero, CursorMode.Auto);
    }
}

public enum CursorType
{
    Default,
    NodeSelected,
}