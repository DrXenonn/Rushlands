using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform Cam;
    [SerializeField] private float MinCamSpeed, MaxCamSpeed;
    [SerializeField] private float CamZoomSpeed = 0.05f;
    [SerializeField] private float CamMaxHeight, CamMinHeight;
    [SerializeField] private float CamMaxRot, CamMinRot;
    [SerializeField] private float MinZoomForRotation = 0.5f;
    [SerializeField] private float RotationSmoothTime = 0.2f;
    [SerializeField] private float ZoomSmoothTime = 0.2f;
    [SerializeField] private float MovementSmoothTime = 0.2f;
    [SerializeField] private float MaxX, MinX, MaxZ, MinZ;
    
    private Vector3 _movementVector;
    private float _zoomLevel;
    private float _targetZoomLevel;
    private float _zoomSmoothVelocity;
    private float _rotationSmoothVelocity;

    public void OnMove(InputAction.CallbackContext context)
    {
        var value = context.ReadValue<Vector3>();

        if (_zoomLevel >= 0.99f && value.y > 0 || _zoomLevel <= 0.01 && value.y < 0)
        {
            value.y = 0f;
        }

        _movementVector = value;
    }

    private void Update()
    {
        HandleZooming();
        HandleMovement();
    }

    private void HandleMovement()
    {
        var camSpeed = Mathf.Lerp(MaxCamSpeed, MinCamSpeed, _zoomLevel);
        var targetPosition = transform.position + _movementVector * (camSpeed * Time.deltaTime);
        if (targetPosition.x > MaxX || targetPosition.z > MaxZ || targetPosition.x < MinX || targetPosition.z < MinZ) return;
        transform.position = Vector3.Lerp(transform.position, targetPosition, MovementSmoothTime);
    }


    private void HandleZooming()
    {
        _targetZoomLevel += _movementVector.y * CamZoomSpeed;
        _targetZoomLevel = Mathf.Clamp(_targetZoomLevel, 0f, 1f);
        
        _zoomLevel = Mathf.SmoothDamp(_zoomLevel, _targetZoomLevel, ref _zoomSmoothVelocity, ZoomSmoothTime);

        var newPos = transform.position;
        newPos.y = Mathf.Lerp(CamMaxHeight, CamMinHeight, _zoomLevel);
        transform.position = newPos;

        var targetRotX = _zoomLevel >= MinZoomForRotation 
            ? Mathf.Lerp(CamMaxRot, CamMinRot, (_zoomLevel - MinZoomForRotation) / (1f - MinZoomForRotation))
            : CamMaxRot;

        var smoothRotX = Mathf.SmoothDampAngle(Cam.rotation.eulerAngles.x, targetRotX, ref _rotationSmoothVelocity, RotationSmoothTime);
        Cam.rotation = Quaternion.Euler(smoothRotX, Cam.rotation.eulerAngles.y, Cam.rotation.eulerAngles.z);
    }
}