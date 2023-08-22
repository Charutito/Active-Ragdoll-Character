using System;
using UnityEngine;
using Utils;

namespace ActiveRagdoll
{
    [Serializable]
    public class RagdollLocomotionController
    {
        [Header("Movement Properties")] [SerializeField]
        private float moveSpeed = 10f;

        [SerializeField] private float jumpForce = 18f;
        [SerializeField] private float StepDuration = 0.2f;
        [SerializeField] private float StepHeight = 1.7f;
        [SerializeField] private float FeetMountForce = 25f;

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
        private RagdollDefaultTargetState defaultTargetState;

        public void Init(RagdollJointHandler jointsHandler, RagdollState state,
            RagdollDefaultTargetState defaultTargetState)
        {
            jointHandler = jointsHandler;
            groundLayer = LayerMask.NameToLayer(GROUND_LAYER_NAME);
            ragdollState = state;
            this.defaultTargetState = defaultTargetState;
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

        internal void PerformPlayerGetUpJumping(float jumpValue)
        {
            if (jumpValue > 0)
            {
                if (!ragdollState.jumpAxisUsed)
                {
                    if (balanced && !ragdollState.inAir)
                    {
                        ragdollState.jumping = true;
                    }

                    else if (!balanced)
                    {
                        DeactivateRagdoll();
                    }
                }

                ragdollState.jumpAxisUsed = true;
            }

            else
            {
                ragdollState.jumpAxisUsed = false;
            }


            if (ragdollState.jumping)
            {
                ragdollState.isJumping = true;

                Rigidbody rootRigidbody = jointHandler.GetRigidBodyFromJoint(RagdollParts.ROOT);
                rootRigidbody.velocity = rootRigidbody.velocity.ModifyY(rootRigidbody.transform.up.y * jumpForce);
            }

            if (ragdollState.isJumping)
            {
                ragdollState.jumpingResetTimer += Time.fixedDeltaTime;

                if (ragdollState.jumpingResetTimer > 0.2f)
                {
                    ragdollState.jumpingResetTimer = 0.0f;
                    ragdollState.jumping = false;
                    ragdollState.isJumping = false;
                    ragdollState.inAir = true;
                }
            }
        }

        internal void PerformWalking()
        {
            if (ragdollState.inAir)
                return;

            if (walkForward)
            {
                WalkForwards();
            }

            if (walkBackward)
            {
                WalkBackwards();
            }

            if (stepRight)
            {
                TakeStepRight();
            }
            else
            {
                ResetStepRight();
            }

            if (stepLeft)
            {
                TakeStepLeft();
            }
            else
            {
                ResetStepLeft();
            }
        }

        private void WalkBackwards() => Walk(RagdollParts.LEFT_FOOT, RagdollParts.RIGHT_FOOT,
            ref stepLeft, ref stepRight,
            ref alertLegLeft,
            ref alertLegRight);

        private void WalkForwards() => Walk(RagdollParts.RIGHT_FOOT, RagdollParts.LEFT_FOOT,
            ref stepRight, ref stepLeft,
            ref alertLegRight,
            ref alertLegLeft);

        private void TakeStepLeft() => TakeStep(ref stepLTimer, RagdollParts.LEFT_FOOT,
            ref stepLeft, ref stepRight,
            RagdollParts.UPPER_LEFT_LEG,
            RagdollParts.LOWER_LEFT_LEG, RagdollParts.UPPER_RIGHT_LEG);

        private void TakeStepRight() => TakeStep(ref stepRTimer, RagdollParts.RIGHT_FOOT,
            ref stepRight, ref stepLeft,
            RagdollParts.UPPER_RIGHT_LEG,
            RagdollParts.LOWER_RIGHT_LEG, RagdollParts.UPPER_LEFT_LEG);

        private void TakeStep(ref float stepTimer,
            string footLabel,
            ref bool stepFootState,
            ref bool oppositeStepFootState,
            string upperJointLabel,
            string lowerJointLabel,
            string upperOppositeJointLabel)
        {
            stepTimer += Time.fixedDeltaTime;
            jointHandler.GetRigidBodyFromJoint(footLabel)
                .AddForce(-Vector3.up * (FeetMountForce * Time.deltaTime), ForceMode.Impulse);

            var upperLegJoint = jointHandler.GetConfigurableJointWithID(upperJointLabel);
            var upperLegJointTargetRotation = upperLegJoint.targetRotation;

            var lowerLegJoint = jointHandler.GetConfigurableJointWithID(lowerJointLabel);
            var lowerLegJointTargetRotation = lowerLegJoint.targetRotation;

            var upperOppositeLegJoint = jointHandler.GetConfigurableJointWithID(upperOppositeJointLabel);
            var upperOppositeLegJointTargetRotation = upperOppositeLegJoint.targetRotation;

            bool isWalking = walkForward || walkBackward;

            if (walkForward)
            {
                upperLegJointTargetRotation = upperLegJointTargetRotation.DisplaceX(0.09f * StepHeight);
                lowerLegJointTargetRotation = lowerLegJointTargetRotation.DisplaceX(-0.09f * StepHeight * 2);
                upperOppositeLegJointTargetRotation =
                    upperOppositeLegJointTargetRotation.DisplaceX(-0.12f * StepHeight / 2);
            }

            if (walkBackward)
            {
                //TODO: Is this necessary for something? It's multiplying by 0.
                upperLegJointTargetRotation = upperLegJointTargetRotation.DisplaceX(-0.00f * StepHeight);
                lowerLegJointTargetRotation = lowerLegJointTargetRotation.DisplaceX(-0.07f * StepHeight * 2);
                upperOppositeLegJointTargetRotation =
                    upperOppositeLegJointTargetRotation.DisplaceX(0.02f * StepHeight / 2);
            }

            if (isWalking)
            {
                upperLegJoint.targetRotation = upperLegJointTargetRotation;
                lowerLegJoint.targetRotation = lowerLegJointTargetRotation;
                upperOppositeLegJoint.targetRotation = upperOppositeLegJointTargetRotation;
            }


            if (stepTimer <= StepDuration)
                return;

            stepTimer = 0;
            stepFootState = false;

            if (isWalking)
            {
                oppositeStepFootState = true;
            }
        }

        private void ResetStepLeft() =>
            ResetStep(RagdollParts.UPPER_LEFT_LEG, RagdollParts.LOWER_LEFT_LEG,
                defaultTargetState.UpperLeftLegTarget, defaultTargetState.LowerLeftLegTarget,
                7f, 18f);

        private void ResetStepRight() => ResetStep(
            RagdollParts.UPPER_RIGHT_LEG, RagdollParts.LOWER_RIGHT_LEG,
            defaultTargetState.UpperRightLegTarget, defaultTargetState.LowerRightLegTarget,
            8f, 17f);

        private void ResetStep(string upperLegLabel,
            string lowerLegLabel,
            Quaternion upperLegTarget,
            Quaternion lowerLegTarget,
            float upperLegLerpMultiplier,
            float lowerLegLerpMultiplier)
        {
            jointHandler.GetConfigurableJointWithID(upperLegLabel).targetRotation = Quaternion.Lerp(
                jointHandler.GetJointTargetRotation(upperLegLabel), upperLegTarget,
                upperLegLerpMultiplier * Time.fixedDeltaTime);
            jointHandler.GetConfigurableJointWithID(lowerLegLabel).targetRotation = Quaternion.Lerp(
                jointHandler.GetJointTargetRotation(lowerLegLabel), lowerLegTarget,
                lowerLegLerpMultiplier * Time.fixedDeltaTime);

            Vector3 feetForce = -Vector3.up * (FeetMountForce * Time.deltaTime);
            jointHandler.GetRigidBodyFromJoint(RagdollParts.RIGHT_FOOT).AddForce(feetForce, ForceMode.Impulse);
            jointHandler.GetRigidBodyFromJoint(RagdollParts.LEFT_FOOT).AddForce(feetForce, ForceMode.Impulse);
        }

        internal void PlayerMovement(bool forwardIsCameraDirection, Vector2 movementVector)
        {
            if (forwardIsCameraDirection)
            {
                MoveInCameraDirection(movementVector);
            }
            else
            {
                MoveInOwnDirection(movementVector);
            }
        }

        private void MoveInCameraDirection(Vector2 movementVector)
        {
            var direction = jointHandler.GetRagdollJointWithID(RagdollParts.ROOT).transform.rotation *
                            new Vector3(movementVector.x, 0.0f, movementVector.y);
            direction.y = 0f;
            Rigidbody rootRigidbody = jointHandler.GetRigidBodyFromJoint(RagdollParts.ROOT);
            var velocity = rootRigidbody.velocity;
            rootRigidbody.velocity = Vector3.Lerp(velocity,
                (direction * moveSpeed) + new Vector3(0, velocity.y, 0), 0.8f);

            if (movementVector.x != 0 || movementVector.y != 0 && balanced)
            {
                StartWalkingForward();
            }
            else if (movementVector is { x: 0, y: 0 })
            {
                StopWalkingForward();
            }
        }

        private void StartWalkingForward()
        {
            if (walkForward || ragdollState.moveAxisUsed)
                return;

            walkForward = true;
            ragdollState.moveAxisUsed = true;
            ragdollState.isKeyDown = true;
        }

        private void StopWalkingForward()
        {
            if (!walkForward || !ragdollState.moveAxisUsed)
                return;

            walkForward = false;
            ragdollState.moveAxisUsed = false;
            ragdollState.isKeyDown = false;
        }

        private void MoveInOwnDirection(Vector2 movementVector)
        {
            if (movementVector.y != 0)
            {
                var rootRigidbody = jointHandler.GetRigidBodyFromJoint(RagdollParts.ROOT);
                var v3 = rootRigidbody.transform.forward * (movementVector.y * moveSpeed);
                v3.y = rootRigidbody.velocity.y;
                rootRigidbody.velocity = v3;
            }

            if (movementVector.y > 0)
            {
                StartWalkingForwardInOwnDirection();
            }
            else if (movementVector.y < 0)
            {
                StartWalkingBackward();
            }
            else
            {
                StopWalking();
            }
        }

        private void StartWalkingForwardInOwnDirection() =>
            SetWalkMovingState(() => (!walkForward && !ragdollState.moveAxisUsed), true, false,
                true, true,
                jointHandler.PoseOn);

        private void StartWalkingBackward() =>
            SetWalkMovingState(() => !walkBackward && !ragdollState.moveAxisUsed, false, true,
                true, true,
                jointHandler.PoseOn);

        private void StopWalking() => SetWalkMovingState(
            () => walkForward ||
                  walkBackward && ragdollState.moveAxisUsed, false, false,
            false, false, jointHandler.DriveOff);

        private void SetWalkMovingState(Func<bool> activationCondition, bool walkForwardSetState,
            bool walkBackwardSetState,
            bool isMoveAxisUsed, bool isKeyCurrentlyDown, in JointDrive legsJointDrive)
        {
            if (activationCondition.Invoke())
            {
                InternalChangeWalkState(walkForwardSetState, walkBackwardSetState, isMoveAxisUsed, isKeyCurrentlyDown,
                    in legsJointDrive);
            }
        }

        private void InternalChangeWalkState(bool walkForwardState, bool walkBackwardState, bool isMoveAxisUsed,
            bool isKeyCurrentlyDown,
            in JointDrive legsJointDrive)
        {
            this.walkForward = walkForwardState;
            this.walkBackward = walkBackwardState;
            ragdollState.moveAxisUsed = isMoveAxisUsed;
            ragdollState.isKeyDown = isKeyCurrentlyDown;
            if (isRagdoll)
                jointHandler.SetJointAngularDrivesForLegs(in legsJointDrive);
        }
    }
}