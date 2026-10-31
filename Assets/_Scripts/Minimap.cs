using UnityEngine;
using UnityEngine.EventSystems;

public class Minimap : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Transform Camera;
    [SerializeField] private RectTransform MinimapRect;
    [SerializeField] private float Offset = 20f;

    private const float WorldMinX = -180f;
    private const float WorldMaxX = -14;
    private const float WorldMinZ = -231f;
    private const float WorldMaxZ = -10;

    public void OnPointerClick(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(MinimapRect, eventData.position, eventData.pressEventCamera, out var localPoint);
        
        var normalizedPos = new Vector2(
            Mathf.InverseLerp(-MinimapRect.rect.width / 2, MinimapRect.rect.width / 2, localPoint.x),
            Mathf.InverseLerp(-MinimapRect.rect.height / 2, MinimapRect.rect.height / 2, localPoint.y)
        );

        var worldX = Mathf.Lerp(WorldMaxX, WorldMinX, normalizedPos.y);
        var worldZ = Mathf.Lerp(WorldMinZ, WorldMaxZ, normalizedPos.x);

        Camera.position = new Vector3(worldX + Offset, Camera.position.y, worldZ);
    }
}