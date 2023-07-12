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
}