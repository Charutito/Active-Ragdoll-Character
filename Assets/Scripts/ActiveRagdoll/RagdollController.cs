using System;
using System.Collections;
using System.Runtime.CompilerServices;
using ActiveRagdoll;
using UnityEngine;
using Utils;

public class RagdollController : MonoBehaviour
{
    [SerializeField] private RagdollJointHandler jointHandler;
    [SerializeField] private Transform centerOfMass;

    [Header("Movement Properties")] public bool forwardIsCameraDirection = true;
    public float moveSpeed = 10f;
    public float turnSpeed = 6f;
    public float jumpForce = 18f;

    [Header("Balance Properties")] public bool autoGetUpWhenPossible = true;
    public bool useStepPrediction = true;
    public float balanceHeight = 2.5f;

    public float StepDuration = 0.2f;
    public float StepHeight = 1.7f;
    public float FeetMountForce = 25f;

    [Header("Reach Properties")] public float reachSensitivity = 25f;

    [Header("Actions")] public bool canBeKnockoutByImpact = true;
    public float requiredForceToBeKO = 20f;
    public bool canPunch = true;
    public float punchForce = 15f;

    //Hidden variables
    private float jumpingResetTimer;
    private float MouseYAxisArms;
    private float MouseXAxisArms;
    private float MouseYAxisBody;


    private readonly RagdollInputHandler inputHandler = new();
    private RagdollLocomotionController locomotionController;
    [HideInInspector] public bool jumping;
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool inAir;
    [HideInInspector] public bool punchingRight;
    [HideInInspector] public bool punchingLeft;

    [SerializeField] private Camera cam;
    private Vector3 Direction;
    private Vector3 CenterOfMassPoint; //TODO: Check usage

    private RagdollDefaultTargetState defaultTargetState;
    private static int groundLayer;
    private readonly WaitForSeconds punchDelayWaitTime = new(0.3f);
    private Rigidbody RightHandRigidBody => jointHandler.GetRigidBodyFromJoint(RagdollParts.RIGHT_HAND);
    private Rigidbody LeftHandRigidBody => jointHandler.GetRigidBodyFromJoint(RagdollParts.LEFT_HAND);

    void Awake()
    {
        groundLayer = LayerMask.NameToLayer("Ground");
        inputHandler.Init();
        locomotionController = new RagdollLocomotionController(jointHandler);
        defaultTargetState = new RagdollDefaultTargetState(jointHandler);
        SetupHandContacts();
    }

    private void SetupHandContacts()
    {
        var handContacts = GetComponentsInChildren<RagdollHandContact>();
        foreach (var handContact in handContacts)
        {
            handContact.Init(inputHandler);
        }
    }

    private void Update()
    {
        if (!inAir)
        {
            PlayerMovement();

            if (canPunch)
            {
                PerformPlayerPunch();
            }
        }

        PlayerReach();

        if (locomotionController.balanced && useStepPrediction)
        {
            PerformStepPrediction();
        }

        if (!useStepPrediction)
        {
            ResetWalkCycle();
        }

        GroundCheck();
        UpdateCenterOfMass();
    }

    private void FixedUpdate()
    {
        PerformWalking();
        PerformPlayerRotation();
        ResetPlayerPose();
        PerformPlayerGetUpJumping();
    }

    private void PerformPlayerRotation()
    {
        if (!jointHandler.TryGetJointWithID(RagdollParts.ROOT, out var joint))
        {
            return;
        }

        ConfigurableJoint rootJoint = joint.Joint;

        if (forwardIsCameraDirection)
        {
            var lookPos = cam.transform.forward.ToX0Z();
            var rotation = Quaternion.LookRotation(lookPos);
            rootJoint.targetRotation = Quaternion.Slerp(rootJoint.targetRotation,
                Quaternion.Inverse(rotation), Time.deltaTime * turnSpeed);
        }
        else
        {
            //buffer all changes to quaternion before applying to memory location
            Quaternion rootJointTargetRotation = rootJoint.targetRotation;

            if (inputHandler.MovementAxis.x != 0)
            {
                rootJointTargetRotation = Quaternion.Lerp(rootJointTargetRotation,
                    rootJointTargetRotation.DisplaceY(-inputHandler.MovementAxis.x * turnSpeed),
                    6 * Time.fixedDeltaTime);
            }

            if (rootJointTargetRotation.y < -0.98f)
            {
                rootJointTargetRotation = rootJointTargetRotation.ModifyY(0.98f);
            }

            else if (rootJointTargetRotation.y > 0.98f)
            {
                rootJointTargetRotation = rootJointTargetRotation.ModifyY(-0.98f);
            }

            rootJoint.targetRotation = rootJointTargetRotation;
        }
    }

    private void ResetPlayerPose()
    {
        if (locomotionController.resetPose && !jumping)
        {
            jointHandler.GetConfigurableJointWithID(RagdollParts.BODY).targetRotation = defaultTargetState.BodyTarget;
            jointHandler.GetConfigurableJointWithID(RagdollParts.UPPER_RIGHT_ARM).targetRotation =
                defaultTargetState.UpperRightArmTarget;
            jointHandler.GetConfigurableJointWithID(RagdollParts.LOWER_RIGHT_ARM).targetRotation =
                defaultTargetState.LowerRightArmTarget;
            jointHandler.GetConfigurableJointWithID(RagdollParts.UPPER_LEFT_ARM).targetRotation =
                defaultTargetState.UpperLeftArmTarget;
            jointHandler.GetConfigurableJointWithID(RagdollParts.LOWER_LEFT_ARM).targetRotation =
                defaultTargetState.LowerLeftArmTarget;

            MouseYAxisArms = 0;
            locomotionController.resetPose = false;
        }
    }

    public void PlayerLanded()
    {
        if (CanResetPoseAfterLanding())
        {
            inAir = false;
            locomotionController.resetPose = true;
        }
    }

    private bool CanResetPoseAfterLanding()
    {
        return inAir && !isJumping && !jumping;
    }

    private void PerformPlayerGetUpJumping()
    {
        if (inputHandler.JumpValue > 0)
        {
            if (!locomotionController.jumpAxisUsed)
            {
                if (locomotionController.balanced && !inAir)
                {
                    jumping = true;
                }

                else if (!locomotionController.balanced)
                {
                    DeactivateRagdoll();
                }
            }

            locomotionController.jumpAxisUsed = true;
        }

        else
        {
            locomotionController.jumpAxisUsed = false;
        }


        if (jumping)
        {
            isJumping = true;

            Rigidbody rootRigidbody = jointHandler.GetRigidBodyFromJoint(RagdollParts.ROOT);
            rootRigidbody.velocity = rootRigidbody.velocity.ModifyY(rootRigidbody.transform.up.y * jumpForce);
        }

        if (isJumping)
        {
            jumpingResetTimer += Time.fixedDeltaTime;

            if (jumpingResetTimer > 0.2f)
            {
                jumpingResetTimer = 0.0f;
                jumping = false;
                isJumping = false;
                inAir = true;
            }
        }
    }

    private void GroundCheck()
    {
        Transform rootTransform = jointHandler.GetRagdollJointWithID(RagdollParts.ROOT).transform;
        Ray ray = new Ray(rootTransform.position, Vector3.down);
        bool isHittingGround = Physics.Raycast(ray, out _, balanceHeight, 1 << groundLayer);

        if (!isHittingGround)
        {
            if (locomotionController.balanced)
            {
                locomotionController.balanced = false;
            }
        }
        else if (ShouldSetBalanced())
        {
            locomotionController.balanced = true;
        }

        bool needsStateChange = (locomotionController.balanced == locomotionController.isRagdoll);

        if (!needsStateChange)
            return;

        if (locomotionController.balanced)
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
        return !inAir &&
               !isJumping &&
               !locomotionController.reachRightAxisUsed &&
               !locomotionController.reachLeftAxisUsed &&
               !locomotionController.balanced &&
               jointHandler.GetRigidBodyFromJoint(RagdollParts.ROOT).velocity.magnitude < 1f &&
               autoGetUpWhenPossible;
    }

    private void DeactivateRagdoll() =>
        locomotionController.SetRagdollState(false, ref jointHandler.BalanceOn, ref jointHandler.PoseOn, true);

    public void ActivateRagdoll() =>
        locomotionController.SetRagdollState(true, ref jointHandler.DriveOff, ref jointHandler.DriveOff, false);

    private void PlayerMovement()
    {
        if (forwardIsCameraDirection)
        {
            MoveInCameraDirection();
        }
        else
        {
            MoveInOwnDirection();
        }
    }

    private void MoveInCameraDirection()
    {
        Direction = jointHandler.GetRagdollJointWithID(RagdollParts.ROOT).transform.rotation *
                    new Vector3(inputHandler.MovementAxis.x, 0.0f, inputHandler.MovementAxis.y);
        Direction.y = 0f;
        Rigidbody rootRigidbody = jointHandler.GetRigidBodyFromJoint(RagdollParts.ROOT);
        var velocity = rootRigidbody.velocity;
        rootRigidbody.velocity = Vector3.Lerp(velocity,
            (Direction * moveSpeed) + new Vector3(0, velocity.y, 0), 0.8f);

        if (inputHandler.MovementAxis.x != 0 || inputHandler.MovementAxis.y != 0 && locomotionController.balanced)
        {
            StartWalkingForward();
        }
        else if (inputHandler.MovementAxis is { x: 0, y: 0 })
        {
            StopWalkingForward();
        }
    }

    private void StartWalkingForward()
    {
        if (!locomotionController.walkForward && !locomotionController.moveAxisUsed)
        {
            locomotionController.walkForward = true;
            locomotionController.moveAxisUsed = true;
            locomotionController.isKeyDown = true;
        }
    }

    private void StopWalkingForward()
    {
        if (locomotionController.walkForward && locomotionController.moveAxisUsed)
        {
            locomotionController.walkForward = false;
            locomotionController.moveAxisUsed = false;
            locomotionController.isKeyDown = false;
        }
    }

    private void MoveInOwnDirection()
    {
        if (inputHandler.MovementAxis.y != 0)
        {
            var rootRigidbody = jointHandler.GetRigidBodyFromJoint(RagdollParts.ROOT);
            var v3 = rootRigidbody.transform.forward * (inputHandler.MovementAxis.y * moveSpeed);
            v3.y = rootRigidbody.velocity.y;
            rootRigidbody.velocity = v3;
        }

        if (inputHandler.MovementAxis.y > 0)
        {
            StartWalkingForwardInOwnDirection();
        }
        else if (inputHandler.MovementAxis.y < 0)
        {
            StartWalkingBackward();
        }
        else
        {
            StopWalking();
        }
    }

    private void StartWalkingForwardInOwnDirection() =>
        SetWalkMovingState(() => (!locomotionController.walkForward && !locomotionController.moveAxisUsed), true, false,
            true, true,
            jointHandler.PoseOn);

    private void StartWalkingBackward() =>
        SetWalkMovingState(() => !locomotionController.walkBackward && !locomotionController.moveAxisUsed, false, true,
            true, true,
            jointHandler.PoseOn);

    private void StopWalking() => SetWalkMovingState(
        () => locomotionController.walkForward ||
              locomotionController.walkBackward && locomotionController.moveAxisUsed, false, false,
        false, false, jointHandler.DriveOff);

    private void SetWalkMovingState(Func<bool> activationCondition, bool walkForwardSetState, bool walkBackwardSetState,
        bool isMoveAxisUsed, bool isKeyCurrentlyDown, in JointDrive legsJointDrive)
    {
        if (activationCondition.Invoke())
        {
            InternalChangeWalkState(walkForwardSetState, walkBackwardSetState, isMoveAxisUsed, isKeyCurrentlyDown,
                in legsJointDrive);
        }
    }

    private void InternalChangeWalkState(bool walkForward, bool walkBackward, bool isMoveAxisUsed,
        bool isKeyCurrentlyDown,
        in JointDrive legsJointDrive)
    {
        locomotionController.walkForward = walkForward;
        locomotionController.walkBackward = walkBackward;
        locomotionController.moveAxisUsed = isMoveAxisUsed;
        locomotionController.isKeyDown = isKeyCurrentlyDown;
        if (locomotionController.isRagdoll)
            SetJointAngularDrivesForLegs(in legsJointDrive);
    }

    private void SetJointAngularDrivesForLegs(in JointDrive jointDrive)
    {
        jointHandler.SetJointAngularDrives(RagdollParts.UPPER_RIGHT_LEG, in jointDrive);
        jointHandler.SetJointAngularDrives(RagdollParts.LOWER_RIGHT_LEG, in jointDrive);
        jointHandler.SetJointAngularDrives(RagdollParts.UPPER_LEFT_LEG, in jointDrive);
        jointHandler.SetJointAngularDrives(RagdollParts.LOWER_LEFT_LEG, in jointDrive);
        jointHandler.SetJointAngularDrives(RagdollParts.RIGHT_FOOT, in jointDrive);
        jointHandler.SetJointAngularDrives(RagdollParts.LEFT_FOOT, in jointDrive);
    }

    private void PlayerReach()
    {
        MouseYAxisBody = Mathf.Clamp(MouseYAxisBody += (inputHandler.AimAxis.y / reachSensitivity), -0.2f, 0.1f);
        jointHandler.GetConfigurableJointWithID(RagdollParts.BODY).targetRotation =
            new Quaternion(MouseYAxisBody, 0, 0, 1);

        HandleLeftSideReach();
        HandleRightSideReach();
    }

    private void HandlePlayerReach(bool punchingSide, float grabValue, ref bool reachSideAxisUsed, string upperArmJoint,
        string lowerArmJoint, bool isRightArm)
    {
        if (punchingSide)
            return;
        if (grabValue != 0)
        {
            if (!reachSideAxisUsed)
            {
                jointHandler.SetJointAngularDrives(upperArmJoint, in jointHandler.ReachStiffness);
                jointHandler.SetJointAngularDrives(lowerArmJoint, in jointHandler.ReachStiffness);
                jointHandler.SetJointAngularDrives(RagdollParts.BODY, in jointHandler.CoreStiffness);
                reachSideAxisUsed = true;
            }

            int multiplier = isRightArm ? 1 : -1;
            MouseYAxisArms = Mathf.Clamp(MouseYAxisArms += (inputHandler.AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            jointHandler.GetConfigurableJointWithID(upperArmJoint).targetRotation =
                new Quaternion((0.58f + (MouseYAxisArms)) * multiplier, -0.88f - (MouseYAxisArms), 0.8f * multiplier,
                    1);
        }
        else
        {
            if (!reachSideAxisUsed)
                return;
            if (locomotionController.balanced)
            {
                jointHandler.SetJointAngularDrives(upperArmJoint, in jointHandler.PoseOn);
                jointHandler.SetJointAngularDrives(lowerArmJoint, in jointHandler.PoseOn);
                jointHandler.SetJointAngularDrives(RagdollParts.BODY, in jointHandler.PoseOn);
            }
            else
            {
                jointHandler.SetJointAngularDrives(upperArmJoint, in jointHandler.DriveOff);
                jointHandler.SetJointAngularDrives(lowerArmJoint, in jointHandler.DriveOff);
            }

            locomotionController.resetPose = true;
            reachSideAxisUsed = false;
        }
    }


    private void HandleRightSideReach() => HandlePlayerReach(punchingRight, inputHandler.GrabRightValue,
        ref locomotionController.reachRightAxisUsed, RagdollParts.UPPER_RIGHT_ARM, RagdollParts.LOWER_RIGHT_ARM, true);

    private void HandleLeftSideReach() => HandlePlayerReach(punchingLeft, inputHandler.GrabLeftValue,
        ref locomotionController.reachLeftAxisUsed, RagdollParts.UPPER_LEFT_ARM, RagdollParts.LOWER_LEFT_ARM, false);

    private void PerformPlayerPunch()
    {
        HandleRightPunch();
        HandleLeftPunch();
    }

    private void HandlePunch(
        ref bool punchingArmState,
        bool punchingArmValue,
        bool isRightPunch,
        string upperArmLabel,
        string lowerArmLabel,
        Rigidbody handRigidbody,
        Func<Quaternion> upperArmTargetMethod,
        Func<Quaternion> lowerArmTargetMethod)
    {
        if (punchingArmState == punchingArmValue)
            return;

        RagdollJoint bodyJoint = jointHandler.GetRagdollJointWithID(RagdollParts.BODY);
        ConfigurableJoint upperArmJoint = jointHandler.GetConfigurableJointWithID(upperArmLabel);
        ConfigurableJoint lowerArmJoint = jointHandler.GetConfigurableJointWithID(lowerArmLabel);

        punchingArmState = punchingArmValue;
        int punchRotationMultiplier = isRightPunch ? -1 : 1;

        if (punchingArmValue)
        {
            bodyJoint.Joint.targetRotation = new Quaternion(-0.15f, 0.15f * punchRotationMultiplier, 0, 1);
            upperArmJoint.targetRotation = new Quaternion(0.62f * punchRotationMultiplier, -0.51f, 0.02f, 1);
            lowerArmJoint.targetRotation =
                new Quaternion(-1.31f * punchRotationMultiplier, 0.5f, 0.5f * punchRotationMultiplier, 1);
        }

        else
        {
            bodyJoint.Joint.targetRotation = new Quaternion(-0.15f, -0.15f * punchRotationMultiplier, 0, 1);
            upperArmJoint.targetRotation = new Quaternion(-0.74f * punchRotationMultiplier, 0.04f, 0f, 1);
            lowerArmJoint.targetRotation = new Quaternion(-0.2f * punchRotationMultiplier, 0, 0, 1);

            handRigidbody.AddForce(jointHandler.GetRagdollJointWithID(RagdollParts.ROOT).transform.forward * punchForce,
                ForceMode.Impulse);
            bodyJoint.Rigidbody.AddForce(bodyJoint.transform.forward * punchForce, ForceMode.Impulse);

            StartCoroutine(PunchDelayCoroutine(isRightPunch, upperArmJoint, lowerArmJoint, upperArmTargetMethod,
                lowerArmTargetMethod));
        }
    }

    private IEnumerator PunchDelayCoroutine(bool isRightArm, ConfigurableJoint upperArmJoint,
        ConfigurableJoint lowerArmJoint, Func<Quaternion> upperArmTarget, Func<Quaternion> lowerArmTarget)
    {
        yield return punchDelayWaitTime;
        //Mainly because we can't pass in ref of bool value to coroutine, if not using unsafe void*
        bool punchValueToCheck = isRightArm ? inputHandler.PunchRightValue : inputHandler.PunchLeftValue;
        if (punchValueToCheck) yield break;

        upperArmJoint.targetRotation = upperArmTarget.Invoke();
        lowerArmJoint.targetRotation = lowerArmTarget.Invoke();
    }

    private void HandleLeftPunch() =>
        HandlePunch(ref punchingLeft, inputHandler.PunchLeftValue, false, RagdollParts.UPPER_LEFT_ARM,
            RagdollParts.LOWER_LEFT_ARM, LeftHandRigidBody,
            () => defaultTargetState.UpperLeftArmTarget, () => defaultTargetState.LowerLeftArmTarget);

    private void HandleRightPunch() => HandlePunch(ref punchingRight, inputHandler.PunchRightValue, true,
        RagdollParts.UPPER_RIGHT_ARM,
        RagdollParts.LOWER_RIGHT_ARM, RightHandRigidBody, () => defaultTargetState.UpperRightArmTarget,
        () => defaultTargetState.LowerLeftArmTarget);

    private void PerformWalking()
    {
        if (inAir)
            return;

        if (locomotionController.walkForward)
        {
            WalkForwards();
        }

        if (locomotionController.walkBackward)
        {
            WalkBackwards();
        }

        if (locomotionController.stepRight)
        {
            TakeStepRight();
        }
        else
        {
            ResetStepRight();
        }

        if (locomotionController.stepLeft)
        {
            TakeStepLeft();
        }
        else
        {
            ResetStepLeft();
        }
    }

    private void ResetStepLeft() =>
        ResetStep(RagdollParts.UPPER_LEFT_LEG, RagdollParts.LOWER_LEFT_LEG, defaultTargetState.UpperLeftLegTarget,
            defaultTargetState.LowerLeftLegTarget, 7f, 18f);

    private void ResetStepRight() => ResetStep(RagdollParts.UPPER_RIGHT_LEG, RagdollParts.LOWER_RIGHT_LEG,
        defaultTargetState.UpperRightLegTarget,
        defaultTargetState.LowerRightLegTarget, 8f, 17f);

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

    private void TakeStepLeft() => TakeStep(ref locomotionController.stepLTimer, RagdollParts.LEFT_FOOT,
        ref locomotionController.stepLeft, ref locomotionController.stepRight,
        RagdollParts.UPPER_LEFT_LEG,
        RagdollParts.LOWER_LEFT_LEG, RagdollParts.UPPER_RIGHT_LEG);

    private void TakeStepRight() => TakeStep(ref locomotionController.stepRTimer, RagdollParts.RIGHT_FOOT,
        ref locomotionController.stepRight, ref locomotionController.stepLeft,
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

        bool isWalking = locomotionController.walkForward || locomotionController.walkBackward;

        if (locomotionController.walkForward)
        {
            upperLegJointTargetRotation = upperLegJointTargetRotation.DisplaceX(0.09f * StepHeight);
            lowerLegJointTargetRotation = lowerLegJointTargetRotation.DisplaceX(-0.09f * StepHeight * 2);
            upperOppositeLegJointTargetRotation =
                upperOppositeLegJointTargetRotation.DisplaceX(-0.12f * StepHeight / 2);
        }

        if (locomotionController.walkBackward)
        {
            //TODO: Is this necessary for something? It's multiplying by 0.
            upperLegJointTargetRotation = upperLegJointTargetRotation.DisplaceX(-0.00f * StepHeight);
            lowerLegJointTargetRotation = lowerLegJointTargetRotation.DisplaceX(-0.07f * StepHeight * 2);
            upperOppositeLegJointTargetRotation = upperOppositeLegJointTargetRotation.DisplaceX(0.02f * StepHeight / 2);
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

    private void WalkBackwards() => locomotionController.Walk(RagdollParts.LEFT_FOOT, RagdollParts.RIGHT_FOOT,
        ref locomotionController.stepLeft, ref locomotionController.stepRight,
        ref locomotionController.alertLegLeft,
        ref locomotionController.alertLegRight);

    private void WalkForwards() => locomotionController.Walk(RagdollParts.RIGHT_FOOT, RagdollParts.LEFT_FOOT,
        ref locomotionController.stepRight, ref locomotionController.stepLeft,
        ref locomotionController.alertLegRight,
        ref locomotionController.alertLegLeft);

    private void PerformStepPrediction()
    {
        if (!locomotionController.walkForward && !locomotionController.walkBackward)
        {
            locomotionController.stepRight = false;
            locomotionController.stepLeft = false;
            locomotionController.stepRTimer = 0;
            locomotionController.stepLTimer = 0;
            locomotionController.alertLegRight = false;
            locomotionController.alertLegLeft = false;
        }

        float rightFootZPosition =
            jointHandler.GetConfigurableJointWithID(RagdollParts.RIGHT_FOOT).transform.position.z;
        float leftFootZPosition =
            jointHandler.GetConfigurableJointWithID(RagdollParts.LEFT_FOOT).transform.position.z;

        //TODO: Refactor to reduce amount of code here
        if (centerOfMass.position.z < rightFootZPosition && centerOfMass.position.z < leftFootZPosition)
        {
            locomotionController.walkBackward = true;
        }
        else
        {
            if (!locomotionController.isKeyDown)
            {
                locomotionController.walkBackward = false;
            }
        }

        if (centerOfMass.position.z > rightFootZPosition && centerOfMass.position.z > leftFootZPosition)
        {
            locomotionController.walkForward = true;
        }
        else
        {
            if (!locomotionController.isKeyDown)
            {
                locomotionController.walkForward = false;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCenterOfMass()
    {
        jointHandler.GetCenterOfMass(out CenterOfMassPoint, centerOfMass);
    }

    private void ResetWalkCycle()
    {
        if (!locomotionController.walkForward && !locomotionController.walkBackward)
        {
            locomotionController.stepRight = false;
            locomotionController.stepLeft = false;
            locomotionController.stepRTimer = 0;
            locomotionController.stepLTimer = 0;
            locomotionController.alertLegRight = false;
            locomotionController.alertLegLeft = false;
        }
    }
}