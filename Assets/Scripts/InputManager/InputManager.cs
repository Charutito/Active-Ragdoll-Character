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
    
    public Vector2 AimValue => inputActions.Player.HorizontalLook.ReadValue<Vector2>();
    
    private void SetupController()
    {
        inputActions = new InputActions();
        inputActions.Player.Movement.performed += x => Move(x.ReadValue<Vector2>());
        inputActions.Player.Movement.canceled += x => Move(x.ReadValue<Vector2>());
        inputActions.Player.Jump.performed += x => Jump(x.ReadValue<float>());
        inputActions.Player.Jump.canceled += x => Jump(x.ReadValue<float>());
        inputActions.Player.GrabLeft.performed += x => GrabLeft(x.ReadValue<float>());
        inputActions.Player.GrabLeft.canceled += x => GrabLeft(x.ReadValue<float>());
        inputActions.Player.GrabRight.performed += x => GrabRight(x.ReadValue<float>());
        inputActions.Player.GrabRight.canceled += x => GrabRight(x.ReadValue<float>());
        inputActions.Player.PunchLeft.performed += x => PunchLeftPressed(x.ReadValue<float>());
        inputActions.Player.PunchLeft.canceled += x => PunchLeftPressed(x.ReadValue<float>());
        inputActions.Player.PunchRight.performed += x => PunchRightPressed(x.ReadValue<float>());
        inputActions.Player.PunchRight.canceled += x => PunchRightPressed(x.ReadValue<float>());
        inputActions.Player.HorizontalLook.performed += x => Aim(x.ReadValue<Vector2>());
        inputActions.Player.HorizontalLook.canceled += x => Aim(x.ReadValue<Vector2>());
    }
    
    private void Move(Vector2 axis) => currentListener.MovementAxis = axis;
    private void Aim(Vector2 axis) => currentListener.AimAxis = axis;
    private void Jump(float value) => currentListener.JumpValue = value;
    private void GrabLeft(float value) => currentListener.GrabLeftValue = value;
    private void GrabRight(float value) => currentListener.GrabRightValue = value;
    private void PunchLeftPressed(float value) => currentListener.PunchLeftValue = value > 0.5f;
    private void PunchRightPressed(float value) => currentListener.PunchRightValue = value > 0.5f;
}


public interface IInputListener
{
    Vector2 MovementAxis { get; set; }
    Vector2 AimAxis { get; set; }
    float JumpValue { get; set; }
    float GrabLeftValue { get; set; }
    float GrabRightValue { get; set; }
    bool PunchLeftValue { get; set; }
    bool PunchRightValue { get; set; }
}