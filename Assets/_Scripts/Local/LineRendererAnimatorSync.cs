using UnityEngine;

[ExecuteInEditMode]
public class LineRendererAnimatorSync : MonoBehaviour
{
    [SerializeField] private LineRenderer LineRenderer;
    [SerializeField] private Transform AnimatedPoint;
    [SerializeField] private int PointIndex;

    private void Update()
    {
        LineRenderer.SetPosition(PointIndex, AnimatedPoint.localPosition);
    }
}