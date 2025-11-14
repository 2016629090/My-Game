using UnityEngine;

public interface IInputService
{
    float GetHorizontal();
    float GetVertical();
    bool IsRunning();
}

public class InputService : IInputService
{
    public float GetHorizontal()
    {
        return Input.GetAxis("Horizontal");
    }

    public float GetVertical()
    {
        return Input.GetAxis("Vertical");
    }

    public bool IsRunning()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }
}