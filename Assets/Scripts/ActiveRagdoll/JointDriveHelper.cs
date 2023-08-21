using UnityEngine;

internal class JointDriveHelper
{
    internal static JointDrive CreateJointDrive(float positionSpring)
    {
        JointDrive jointDrive = new JointDrive();
        jointDrive.positionSpring = positionSpring;
        jointDrive.positionDamper = 0;
        jointDrive.maximumForce = Mathf.Infinity;
        return jointDrive;
    }
}