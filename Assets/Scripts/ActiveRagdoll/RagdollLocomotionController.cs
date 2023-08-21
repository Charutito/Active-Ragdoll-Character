using UnityEngine;

namespace ActiveRagdoll
{
    public class RagdollLocomotionController
    {
        internal float stepRTimer;
        internal float stepLTimer;
        internal bool walkForward;
        internal bool walkBackward;
        internal bool stepRight;
        internal bool stepLeft;
        internal bool alertLegRight;
        internal bool alertLegLeft;
        internal bool balanced = true;
        internal bool gettingUp;
        internal bool resetPose;
        internal bool isRagdoll;
        internal bool isKeyDown;
        internal bool moveAxisUsed;
        internal bool jumpAxisUsed;
        internal bool reachLeftAxisUsed;
        internal bool reachRightAxisUsed;

        private RagdollJointHandler jointHandler;

        public RagdollLocomotionController(RagdollJointHandler jointsHandler)
        {
            jointHandler = jointsHandler;
        }

        public void SetRagdollState(bool shouldRagdoll, ref JointDrive rootJointDrive,
            ref JointDrive poseJointDrive,
            bool shouldResetPose)
        {
            isRagdoll = shouldRagdoll;
            balanced = !shouldRagdoll;

            jointHandler.SetJointAngularDrives(RagdollParts.ROOT, in rootJointDrive);
            jointHandler.SetJointAngularDrives(RagdollParts.HEAD, in poseJointDrive);

            if (!reachRightAxisUsed)
            {
                jointHandler.SetJointAngularDrives(RagdollParts.UPPER_RIGHT_ARM, in poseJointDrive);
                jointHandler.SetJointAngularDrives(RagdollParts.LOWER_RIGHT_ARM, in poseJointDrive);
            }

            if (!reachLeftAxisUsed)
            {
                jointHandler.SetJointAngularDrives(RagdollParts.UPPER_LEFT_ARM, in poseJointDrive);
                jointHandler.SetJointAngularDrives(RagdollParts.LOWER_LEFT_ARM, in poseJointDrive);
            }

            jointHandler.SetJointAngularDrives(RagdollParts.UPPER_RIGHT_LEG, in poseJointDrive);
            jointHandler.SetJointAngularDrives(RagdollParts.LOWER_RIGHT_LEG, in poseJointDrive);
            jointHandler.SetJointAngularDrives(RagdollParts.UPPER_LEFT_LEG, in poseJointDrive);
            jointHandler.SetJointAngularDrives(RagdollParts.LOWER_LEFT_LEG, in poseJointDrive);
            jointHandler.SetJointAngularDrives(RagdollParts.RIGHT_FOOT, in poseJointDrive);
            jointHandler.SetJointAngularDrives(RagdollParts.LEFT_FOOT, in poseJointDrive);

            if (shouldResetPose)
                resetPose = true;
        }

        internal void Walk(string forwardFootLabel, string backFootLabel, ref bool forwardFootState,
            ref bool backFootState, ref bool forwardAlertLeg, ref bool backAlertLeg)
        {
            float forwardFootTransformZ =
                jointHandler.GetConfigurableJointWithID(forwardFootLabel).transform.position.z;
            float backFootTransformZ = jointHandler.GetConfigurableJointWithID(backFootLabel).transform.position.z;

            bool forwardFootIsBehind = forwardFootTransformZ < backFootTransformZ;
            bool forwardFootIsAhead = forwardFootTransformZ > backFootTransformZ;

            //TODO: Displace logic to not use alternating conditions
            if (forwardFootIsBehind && !backFootState && !forwardAlertLeg)
            {
                forwardFootState = true;
                forwardAlertLeg = true;
                backAlertLeg = true;
            }

            if (forwardFootIsAhead && !forwardFootState && !backAlertLeg)
            {
                backFootState = true;
                backAlertLeg = true;
                forwardAlertLeg = true;
            }
        }
    }
}