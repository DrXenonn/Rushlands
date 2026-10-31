using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private RawImage RawImage;
    [SerializeField] private float X, Y;

    private void Update()
    {
        RawImage.uvRect = new Rect(RawImage.uvRect.position + new Vector2(X, Y) * Time.deltaTime, RawImage.uvRect.size);
    }
}