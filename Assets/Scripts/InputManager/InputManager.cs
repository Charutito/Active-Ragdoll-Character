using UnityEngine;

public class InputManager : SingletonObject<InputManager>
{
    private InputActions inputActions;
    private IInputListener currentListener;

    private void Awake()
    {
        SetupController();
        EnableController();
    }
    
    public void RegisterListener(IInputListener inputListener) => currentListener = inputListener;

    public void EnableController() => inputActions.Enable();
    public void DisableController() => inputActions.Disable();
    
    private void SetupController()
    {
        inputActions = new InputActions();
        inputActions.Player.Movement.performed += x => Move(x.ReadValue<Vector2>());
    }
    
    private void Move(Vector2 axis) => currentListener.MovementAxis = axis;
    private void Aim(Vector2 axis) => currentListener.AimAxis = axis;
    private void Jump() => currentListener.Jump();
}


public interface IInputListener
{
    Vector2 MovementAxis { get; set; }
    Vector2 AimAxis { get; set; }
    void Jump();
}