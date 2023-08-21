using UnityEngine;

public class RagdollHandContact : MonoBehaviour
{
    public RagdollController ragdollController;
    public bool Left;
    private bool HasJoint => joint != null;

    private const string CAN_BE_GRABBED = "CanBeGrabbed";
    private IInputListener inputListener;
    private FixedJoint joint;

    public void Init(IInputListener newInputListener)
    {
        inputListener = newInputListener;
    }

    private void Update()
    {
        //TODO: Add grabValue change event
        HandleJointRelease(Left ? inputListener.GrabLeftValue : inputListener.GrabRightValue);
    }

    private void HandleJointRelease(float reachAxisValue)
    {
        if (!HasJoint)
            return;

        if (reachAxisValue == 0)
        {
            DestroyJoint();
        }
    }

    private void DestroyJoint()
    {
        joint.breakForce = 0;
    }


    private void OnCollisionEnter(Collision col)
    {
        if (CanGrab(col))
        {
            if (CanPerformGrabAction())
            {
                PerformGrabAction(col.gameObject.GetComponent<Rigidbody>());
            }
        }
    }

    private bool CanGrab(Collision col)
    {
        return col.gameObject.CompareTag(CAN_BE_GRABBED) && !HasJoint;
    }

    private bool CanPerformGrabAction()
    {
        if (Left)
        {
            return inputListener.GrabLeftValue != 0 && !ragdollController.punchingLeft;
        }
        else
        {
            return inputListener.GrabRightValue != 0 && !ragdollController.punchingRight;
        }
    }

    private void PerformGrabAction(Rigidbody connectedBody)
    {
        // hasJoint = true;
        joint = gameObject.AddComponent<FixedJoint>();
        joint.breakForce = Mathf.Infinity;
        joint.connectedBody = connectedBody;
    }
}