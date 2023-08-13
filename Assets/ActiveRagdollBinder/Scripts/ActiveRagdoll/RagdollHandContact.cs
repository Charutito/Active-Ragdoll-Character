using UnityEngine;

public class RagdollHandContact : MonoBehaviour
{
    public RagdollController ragdollController;
    public bool Left;
    public bool hasJoint;

    private const string CAN_BE_GRABBED = "CanBeGrabbed";

    private void Update()
    {
        HandleJointRelease(Left ? ragdollController.GrabLeftValue : ragdollController.GrabRightValue);
    }

    private void HandleJointRelease(float reachAxisValue)
    {
        if (hasJoint && reachAxisValue == 0)
        {
            DestroyJoint();
        }

        if (hasJoint && gameObject.GetComponent<FixedJoint>() == null)
        {
            hasJoint = false;
        }
    }

    private void DestroyJoint()
    {
        gameObject.GetComponent<FixedJoint>().breakForce = 0;
        hasJoint = false;
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
        return col.gameObject.CompareTag(CAN_BE_GRABBED) && !hasJoint;
    }

    private bool CanPerformGrabAction()
    {
        if (Left)
        {
            return ragdollController.GrabLeftValue != 0 && !ragdollController.punchingLeft;
        }
        else
        {
            return ragdollController.GrabRightValue != 0 && !ragdollController.punchingRight;
        }
    }

    private void PerformGrabAction(Rigidbody connectedBody)
    {
        hasJoint = true;
        gameObject.AddComponent<FixedJoint>();
        gameObject.GetComponent<FixedJoint>().breakForce = Mathf.Infinity;
        gameObject.GetComponent<FixedJoint>().connectedBody = connectedBody;
    }
}