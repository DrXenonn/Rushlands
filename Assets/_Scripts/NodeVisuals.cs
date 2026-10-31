using UnityEngine;
using UnityEngine.UI;

public class NodeVisuals : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] Renderers;
    [SerializeField] private Image SelectionHighlight; 
    [SerializeField] private Transform NodeCountTextPosition;
    [SerializeField] private Transform GoldVFXPosition;
    [SerializeField] private Transform BallistaTransform;

    public MeshRenderer[] GetRenderers()
    {
        return Renderers;
    }

    public Vector3 GetNodeCountPos()
    {
        return NodeCountTextPosition.position;
    }

    public Transform GetGoldVFXTransform()
    {
        return GoldVFXPosition;
    }

    public Image GetSelectionHighlightImage()
    {
        return SelectionHighlight;
    }

    public Transform GetBallistaTransform()
    {
        return BallistaTransform;
    }
}