using UnityEngine;

public class Billboarding : MonoBehaviour
{
    private Transform _camTransform;

    private void Awake()
    {
        _camTransform = Camera.main!.transform;
    }

    private void Update()
    {
        Vector3 direction = _camTransform.position - transform.position;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);
    }
}