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


        [Header("Balance Properties")] public bool autoGetUpWhenPossible = true;
        public bool useStepPrediction = true;
        public float balanceHeight = 2.5f;

        private RagdollJointHandler jointHandler;
        private static int groundLayer;
        private const string GROUND_LAYER_NAME = "Ground";
        private RagdollState ragdollState;

        public RagdollLocomotionController(RagdollJointHandler jointsHandler, RagdollState state)
        {
            jointHandler = jointsHandler;
            groundLayer = LayerMask.NameToLayer(GROUND_LAYER_NAME);
            ragdollState = state;
        }

        private void SetRagdollState(bool shouldRagdoll, ref JointDrive rootJointDrive,
            ref JointDrive poseJointDrive,
            bool shouldResetPose)
        {
            isRagdoll = shouldRagdoll;
            balanced = !shouldRagdoll;

            jointHandler.SetJointAngularDrives(RagdollParts.ROOT, in rootJointDrive);
            jointHandler.SetJointAngularDrives(RagdollParts.HEAD, in poseJointDrive);

            if (!ragdollState.reachRightAxisUsed)
            {
                jointHandler.SetJointAngularDrives(RagdollParts.UPPER_RIGHT_ARM, in poseJointDrive);
                jointHandler.SetJointAngularDrives(RagdollParts.LOWER_RIGHT_ARM, in poseJointDrive);
            }

            if (!ragdollState.reachLeftAxisUsed)
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

        public void DeactivateRagdoll() =>
            SetRagdollState(false, ref jointHandler.BalanceOn, ref jointHandler.PoseOn, true);

        public void ActivateRagdoll() =>
            SetRagdollState(true, ref jointHandler.DriveOff, ref jointHandler.DriveOff, false);


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

        internal void GroundCheck()
        {
            Transform rootTransform = jointHandler.GetRagdollJointWithID(RagdollParts.ROOT).transform;
            Ray ray = new Ray(rootTransform.position, Vector3.down);
            bool isHittingGround = Physics.Raycast(ray, out _, balanceHeight, 1 << groundLayer);

            if (!isHittingGround)
            {
                if (balanced)
                {
                    balanced = false;
                }
            }
            else if (ShouldSetBalanced())
            {
                balanced = true;
            }

            bool needsStateChange = (balanced == isRagdoll);

            if (!needsStateChange)
                return;

            if (balanced)
            {
                DeactivateRagdoll();
            }
            else
            {
                ActivateRagdoll();
            }
        }

        private bool ShouldSetBalanced()
        {
            return !ragdollState.inAir &&
                   !ragdollState.isJumping &&
                   !ragdollState.reachRightAxisUsed &&
                   !ragdollState.reachLeftAxisUsed &&
                   !balanced &&
                   jointHandler.GetRigidBodyFromJoint(RagdollParts.ROOT).velocity.magnitude < 1f &&
                   autoGetUpWhenPossible;
        }

        internal void HandleStepPrediction(float centerOfMassZPosition)
        {
            if (useStepPrediction)
            {
                PerformStepPrediction(centerOfMassZPosition);
            }
            else
            {
                ResetWalkCycle();
            }
        }

        private void PerformStepPrediction(float centerOfMassZPosition)
        {
            if (!balanced)
                return;

            if (!walkForward && !walkBackward)
            {
                stepRight = false;
                stepLeft = false;
                stepRTimer = 0;
                stepLTimer = 0;
                alertLegRight = false;
                alertLegLeft = false;
            }

            float rightFootZPosition =
                jointHandler.GetConfigurableJointWithID(RagdollParts.RIGHT_FOOT).transform.position.z;
            float leftFootZPosition =
                jointHandler.GetConfigurableJointWithID(RagdollParts.LEFT_FOOT).transform.position.z;

            //TODO: Refactor to reduce amount of code here
            if (centerOfMassZPosition < rightFootZPosition && centerOfMassZPosition < leftFootZPosition)
            {
                walkBackward = true;
            }
            else
            {
                if (!ragdollState.isKeyDown)
                {
                    walkBackward = false;
                }
            }

            if (centerOfMassZPosition > rightFootZPosition && centerOfMassZPosition > leftFootZPosition)
            {
                walkForward = true;
            }
            else
            {
                if (!ragdollState.isKeyDown)
                {
                    walkForward = false;
                }
            }
        }

        private void ResetWalkCycle()
        {
            if (!walkForward && !walkBackward)
            {
                stepRight = false;
                stepLeft = false;
                stepRTimer = 0;
                stepLTimer = 0;
                alertLegRight = false;
                alertLegLeft = false;
            }
        }
        
    }
}