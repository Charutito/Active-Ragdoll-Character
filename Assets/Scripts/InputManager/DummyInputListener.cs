using UnityEngine;

public class DummyInputListener : MonoBehaviour, IInputListener
{
    public Vector2 MovementAxis { get; set; } = Vector2.zero;
    public Vector2 AimAxis { get; set; }

    public void Start()
    {
        InputManager.Instance.RegisterListener(this);
    }

    public void Update()
    {
        Debug.Log($"{GetType()} :: Movement: {MovementAxis}");
    }
    
    public void Jump()
    {
        Debug.Log($"{GetType()} :: Jump");
    }

    public void GrabLeft()
    {
        Debug.Log($"{GetType()} :: GrabLeft");
    }

    public void GrabRight()
    {
        Debug.Log($"{GetType()} :: GrabRight");
    }

    public void PunchLeft()
    {
        Debug.Log($"{GetType()} :: PunchLeft");
    }

    public void PunchRight()
    {
        Debug.Log($"{GetType()} :: PunchRight");
    }
}