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
        inputActions.Player.Movement.canceled += x => Move(x.ReadValue<Vector2>());
        inputActions.Player.Jump.performed += x => Jump();
        inputActions.Player.Jump.canceled += x => Jump();
        inputActions.Player.GrabLeft.performed += x => GrabLeft();
        inputActions.Player.GrabLeft.canceled += x => GrabLeft();
        inputActions.Player.GrabRight.performed += x => GrabRight();
        inputActions.Player.GrabRight.canceled += x => GrabRight();
        inputActions.Player.PunchLeft.performed += x => PunchLeft();
        inputActions.Player.PunchLeft.canceled += x => PunchLeft();
        inputActions.Player.PunchRight.performed += x => PunchRight();
        inputActions.Player.PunchRight.canceled += x => PunchRight();
    }
    
    private void Move(Vector2 axis) => currentListener.MovementAxis = axis;
    private void Aim(Vector2 axis) => currentListener.AimAxis = axis;
    private void Jump() => currentListener.Jump();
    private void GrabLeft() => currentListener.GrabLeft();
    private void GrabRight() => currentListener.GrabRight();
    private void PunchLeft() => currentListener.PunchLeft();
    private void PunchRight() => currentListener.PunchRight();
}


public interface IInputListener
{
    Vector2 MovementAxis { get; set; }
    Vector2 AimAxis { get; set; }
    void Jump();
    void GrabLeft();
    void GrabRight();
    void PunchLeft();
    void PunchRight();
}