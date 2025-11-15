using UnityEngine;

public interface IInputService
{
    float GetHorizontal();
    float GetVertical();
    bool IsRunning();
    bool IsJumpPressed();

    Vector3 GetCameraForward();
    Vector3 GetCameraRight();
}

public class InputService : IInputService
{
    private readonly Camera _mainCamera;

    public InputService()
    {
        _mainCamera = Camera.main;
    }

    public float GetHorizontal() => Input.GetAxis("Horizontal");
    public float GetVertical() => Input.GetAxis("Vertical");
    public bool IsRunning() => Input.GetKey(KeyCode.LeftShift);
    public bool IsJumpPressed() => Input.GetKeyDown(KeyCode.Space);

    public Vector3 GetCameraForward()
    {
        if (_mainCamera == null) return Vector3.forward;

        Vector3 forward = _mainCamera.transform.forward;
        forward.y = 0; // 保持水平
        return forward.normalized;
    }

    public Vector3 GetCameraRight()
    {
        if (_mainCamera == null) return Vector3.right;

        Vector3 right = _mainCamera.transform.right;
        right.y = 0; // 保持水平
        return right.normalized;
    }
}