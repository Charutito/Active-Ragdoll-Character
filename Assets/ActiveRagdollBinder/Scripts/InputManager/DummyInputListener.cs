using UnityEngine;

public class DummyInputListener : MonoBehaviour, IInputListener
{
    public Vector2 MovementAxis { get; set; } = Vector2.zero;
    public Vector2 AimAxis { get; set; }
    public float JumpValue { get; set; }
    public float GrabLeftValue { get; set; }
    public float GrabRightValue { get; set; }
    public bool PunchLeftValue { get; set; }
    public bool PunchRightValue { get; set; }

    public void Start()
    {
        InputManager.Instance.RegisterListener(this);
    }

    public void Update()
    {
        Debug.Log($"{GetType()} :: Movement: {MovementAxis}");
        Debug.Log($"{GetType()} :: Aim: {AimAxis.y}");
        Debug.Log($"{GetType()} :: Jump Value: {JumpValue}");
        Debug.Log($"{GetType()} :: GrabLeft Value: {GrabLeftValue}");
        Debug.Log($"{GetType()} :: GrabRight Value: {GrabRightValue}");
        Debug.Log($"{GetType()} :: PunchLeft Value: {PunchLeftValue}");
        Debug.Log($"{GetType()} :: PunchRight Value: {PunchRightValue}");
    }
}