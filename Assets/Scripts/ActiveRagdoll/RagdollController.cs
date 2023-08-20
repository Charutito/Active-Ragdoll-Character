using System;
using System.Collections;
using System.Runtime.CompilerServices;
using ActiveRagdoll;
using UnityEngine;
using Utils;

public class RagdollController : MonoBehaviour
{
    [SerializeField] private UDictionary<string, RagdollJoint> RagdollDict = new UDictionary<string, RagdollJoint>();

    [SerializeField] private Rigidbody rightHand;
    [SerializeField] private Rigidbody leftHand;
    [SerializeField] private Transform centerOfMass;

    [Header("Movement Properties")] public bool forwardIsCameraDirection = true;
    public float moveSpeed = 10f;
    public float turnSpeed = 6f;
    public float jumpForce = 18f;

    [Header("Balance Properties")] public bool autoGetUpWhenPossible = true;
    public bool useStepPrediction = true;
    public float balanceHeight = 2.5f;
    public float balanceStrength = 5000f;
    public float coreStrength = 1500f;
    public float limbStrength = 500f;

    public float StepDuration = 0.2f;
    public float StepHeight = 1.7f;
    public float FeetMountForce = 25f;

    [Header("Reach Properties")] public float reachSensitivity = 25f;
    public float armReachStiffness = 2000f;

    [Header("Actions")] public bool canBeKnockoutByImpact = true;
    public float requiredForceToBeKO = 20f;
    public bool canPunch = true;
    public float punchForce = 15f;

    //Hidden variables
    private float timer;
    private float Step_R_timer;
    private float Step_L_timer;
    private float MouseYAxisArms;
    private float MouseXAxisArms;
    private float MouseYAxisBody;

    private bool WalkForward;
    private bool WalkBackward;
    private bool StepRight;
    private bool StepLeft;
    private bool Alert_Leg_Right;
    private bool Alert_Leg_Left;
    private bool balanced = true;
    private bool GettingUp;
    private bool ResetPose;
    private bool isRagdoll;
    private bool isKeyDown;
    private bool moveAxisUsed;
    private bool jumpAxisUsed;
    private bool reachLeftAxisUsed;
    private bool reachRightAxisUsed;

    private readonly RagdollInputHandler inputHandler = new();
    [HideInInspector] public bool jumping;
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool inAir;
    [HideInInspector] public bool punchingRight;
    [HideInInspector] public bool punchingLeft;

    [SerializeField] private Camera cam;
    private Vector3 Direction;
    private Vector3 CenterOfMassPoint;

    private Quaternion HeadTarget;
    private Quaternion BodyTarget;
    private Quaternion UpperRightArmTarget;
    private Quaternion LowerRightArmTarget;
    private Quaternion UpperLeftArmTarget;
    private Quaternion LowerLeftArmTarget;
    private Quaternion UpperRightLegTarget;
    private Quaternion LowerRightLegTarget;
    private Quaternion UpperLeftLegTarget;
    private Quaternion LowerLeftLegTarget;
    [SerializeField] private RagdollJointHandler jointHandler;

    private static int groundLayer;
    private readonly WaitForSeconds punchDelayWaitTime = new(0.3f);

    void Awake()
    {
        groundLayer = LayerMask.NameToLayer("Ground");
        inputHandler.Init();
        SetupOriginalPose();
        SetupHandContacts();
    }

    private void SetupHandContacts()
    {
        RagdollHandContact[] handContacts = GetComponentsInChildren<RagdollHandContact>();
        foreach (var handContact in handContacts)
        {
            handContact.Init(inputHandler);
        }
    }

    private void SetupOriginalPose()
    {
        BodyTarget = GetJointTargetRotation(RagdollParts.ROOT);
        HeadTarget = GetJointTargetRotation(RagdollParts.HEAD);
        UpperRightArmTarget = GetJointTargetRotation(RagdollParts.UPPER_RIGHT_ARM);
        LowerRightArmTarget = GetJointTargetRotation(RagdollParts.LOWER_RIGHT_ARM);
        UpperLeftArmTarget = GetJointTargetRotation(RagdollParts.UPPER_LEFT_ARM);
        LowerLeftArmTarget = GetJointTargetRotation(RagdollParts.LOWER_LEFT_ARM);
        UpperRightLegTarget = GetJointTargetRotation(RagdollParts.UPPER_RIGHT_LEG);
        LowerRightLegTarget = GetJointTargetRotation(RagdollParts.LOWER_RIGHT_LEG);
        UpperLeftLegTarget = GetJointTargetRotation(RagdollParts.UPPER_LEFT_LEG);
        LowerLeftLegTarget = GetJointTargetRotation(RagdollParts.LOWER_LEFT_LEG);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Quaternion GetJointTargetRotation(string jointName)
    {
        return RagdollDict[jointName].Joint.targetRotation;
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

        if (balanced && useStepPrediction)
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
        ConfigurableJoint rootJoint = RagdollDict[RagdollParts.ROOT].Joint;

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
        if (ResetPose && !jumping)
        {
            RagdollDict[RagdollParts.BODY].Joint.targetRotation = BodyTarget;
            RagdollDict[RagdollParts.UPPER_RIGHT_ARM].Joint.targetRotation = UpperRightArmTarget;
            RagdollDict[RagdollParts.LOWER_RIGHT_ARM].Joint.targetRotation = LowerRightArmTarget;
            RagdollDict[RagdollParts.UPPER_LEFT_ARM].Joint.targetRotation = UpperLeftArmTarget;
            RagdollDict[RagdollParts.LOWER_LEFT_ARM].Joint.targetRotation = LowerLeftArmTarget;

            MouseYAxisArms = 0;
            ResetPose = false;
        }
    }

    public void PlayerLanded()
    {
        if (CanResetPoseAfterLanding())
        {
            inAir = false;
            ResetPose = true;
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
            if (!jumpAxisUsed)
            {
                if (balanced && !inAir)
                {
                    jumping = true;
                }

                else if (!balanced)
                {
                    DeactivateRagdoll();
                }
            }

            jumpAxisUsed = true;
        }

        else
        {
            jumpAxisUsed = false;
        }


        if (jumping)
        {
            isJumping = true;

            Rigidbody rootRigidbody = RagdollDict[RagdollParts.ROOT].Rigidbody;
            rootRigidbody.velocity = rootRigidbody.velocity.ModifyY(rootRigidbody.transform.up.y * jumpForce);
        }

        if (isJumping)
        {
            timer += Time.fixedDeltaTime;

            if (timer > 0.2f)
            {
                timer = 0.0f;
                jumping = false;
                isJumping = false;
                inAir = true;
            }
        }
    }

    private void GroundCheck()
    {
        Transform rootTransform = RagdollDict[RagdollParts.ROOT].transform;
        Ray ray = new Ray(rootTransform.position, -rootTransform.up);
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
        return !inAir &&
               !isJumping &&
               !reachRightAxisUsed &&
               !reachLeftAxisUsed &&
               !balanced &&
               RagdollDict[RagdollParts.ROOT].Rigidbody.velocity.magnitude < 1f &&
               autoGetUpWhenPossible;
    }


    private void SetRagdollState(bool shouldRagdoll, ref JointDrive rootJointDrive, ref JointDrive poseJointDrive,
        bool shouldResetPose)
    {
        isRagdoll = shouldRagdoll;
        balanced = !shouldRagdoll;

        SetJointAngularDrives(RagdollParts.ROOT, in rootJointDrive);
        SetJointAngularDrives(RagdollParts.HEAD, in poseJointDrive);

        if (!reachRightAxisUsed)
        {
            SetJointAngularDrives(RagdollParts.UPPER_RIGHT_ARM, in poseJointDrive);
            SetJointAngularDrives(RagdollParts.LOWER_RIGHT_ARM, in poseJointDrive);
        }

        if (!reachLeftAxisUsed)
        {
            SetJointAngularDrives(RagdollParts.UPPER_LEFT_ARM, in poseJointDrive);
            SetJointAngularDrives(RagdollParts.LOWER_LEFT_ARM, in poseJointDrive);
        }

        SetJointAngularDrives(RagdollParts.UPPER_RIGHT_LEG, in poseJointDrive);
        SetJointAngularDrives(RagdollParts.LOWER_RIGHT_LEG, in poseJointDrive);
        SetJointAngularDrives(RagdollParts.UPPER_LEFT_LEG, in poseJointDrive);
        SetJointAngularDrives(RagdollParts.LOWER_LEFT_LEG, in poseJointDrive);
        SetJointAngularDrives(RagdollParts.RIGHT_FOOT, in poseJointDrive);
        SetJointAngularDrives(RagdollParts.LEFT_FOOT, in poseJointDrive);

        if (shouldResetPose)
            ResetPose = true;
    }

    private void DeactivateRagdoll() =>
        SetRagdollState(false, ref jointHandler.BalanceOn, ref jointHandler.PoseOn, true);

    public void ActivateRagdoll() => SetRagdollState(true, ref jointHandler.DriveOff, ref jointHandler.DriveOff, false);

    private void SetJointAngularDrives(string jointName, in JointDrive jointDrive)
    {
        RagdollDict[jointName].Joint.angularXDrive = jointDrive;
        RagdollDict[jointName].Joint.angularYZDrive = jointDrive;
    }

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
        Direction = RagdollDict[RagdollParts.ROOT].transform.rotation *
                    new Vector3(inputHandler.MovementAxis.x, 0.0f, inputHandler.MovementAxis.y);
        Direction.y = 0f;
        Rigidbody rootRigidbody = RagdollDict[RagdollParts.ROOT].Rigidbody;
        var velocity = rootRigidbody.velocity;
        rootRigidbody.velocity = Vector3.Lerp(velocity,
            (Direction * moveSpeed) + new Vector3(0, velocity.y, 0), 0.8f);

        if (inputHandler.MovementAxis.x != 0 || inputHandler.MovementAxis.y != 0 && balanced)
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
        if (!WalkForward && !moveAxisUsed)
        {
            WalkForward = true;
            moveAxisUsed = true;
            isKeyDown = true;
        }
    }

    private void StopWalkingForward()
    {
        if (WalkForward && moveAxisUsed)
        {
            WalkForward = false;
            moveAxisUsed = false;
            isKeyDown = false;
        }
    }

    private void MoveInOwnDirection()
    {
        if (inputHandler.MovementAxis.y != 0)
        {
            var rootRigidbody = RagdollDict[RagdollParts.ROOT].Rigidbody;
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
        SetWalkMovingState(() => (!WalkForward && !moveAxisUsed), true, false, true, true, jointHandler.PoseOn);

    private void StartWalkingBackward() =>
        SetWalkMovingState(() => !WalkBackward && !moveAxisUsed, false, true, true, true, jointHandler.PoseOn);

    private void StopWalking() => SetWalkMovingState(() => WalkForward || WalkBackward && moveAxisUsed, false, false,
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
        WalkForward = walkForward;
        WalkBackward = walkBackward;
        moveAxisUsed = isMoveAxisUsed;
        isKeyDown = isKeyCurrentlyDown;
        if (isRagdoll)
            SetJointAngularDrivesForLegs(in legsJointDrive);
    }

    private void SetJointAngularDrivesForLegs(in JointDrive jointDrive)
    {
        SetJointAngularDrives(RagdollParts.UPPER_RIGHT_LEG, in jointDrive);
        SetJointAngularDrives(RagdollParts.LOWER_RIGHT_LEG, in jointDrive);
        SetJointAngularDrives(RagdollParts.UPPER_LEFT_LEG, in jointDrive);
        SetJointAngularDrives(RagdollParts.LOWER_LEFT_LEG, in jointDrive);
        SetJointAngularDrives(RagdollParts.RIGHT_FOOT, in jointDrive);
        SetJointAngularDrives(RagdollParts.LEFT_FOOT, in jointDrive);
    }

    private void PlayerReach()
    {
        MouseYAxisBody = Mathf.Clamp(MouseYAxisBody += (inputHandler.AimAxis.y / reachSensitivity), -0.2f, 0.1f);
        RagdollDict[RagdollParts.BODY].Joint.targetRotation = new Quaternion(MouseYAxisBody, 0, 0, 1);

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
                SetJointAngularDrives(upperArmJoint, in jointHandler.ReachStiffness);
                SetJointAngularDrives(lowerArmJoint, in jointHandler.ReachStiffness);
                SetJointAngularDrives(RagdollParts.BODY, in jointHandler.CoreStiffness);
                reachSideAxisUsed = true;
            }

            int multiplier = isRightArm ? 1 : -1;
            MouseYAxisArms = Mathf.Clamp(MouseYAxisArms += (inputHandler.AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            RagdollDict[upperArmJoint].Joint.targetRotation =
                new Quaternion((0.58f + (MouseYAxisArms)) * multiplier, -0.88f - (MouseYAxisArms), 0.8f * multiplier,
                    1);
        }
        else
        {
            if (!reachSideAxisUsed)
                return;
            if (balanced)
            {
                SetJointAngularDrives(upperArmJoint, in jointHandler.PoseOn);
                SetJointAngularDrives(lowerArmJoint, in jointHandler.PoseOn);
                SetJointAngularDrives(RagdollParts.BODY, in jointHandler.PoseOn);
            }
            else
            {
                SetJointAngularDrives(upperArmJoint, in jointHandler.DriveOff);
                SetJointAngularDrives(lowerArmJoint, in jointHandler.DriveOff);
            }

            ResetPose = true;
            reachSideAxisUsed = false;
        }
    }


    private void HandleRightSideReach() => HandlePlayerReach(punchingRight, inputHandler.GrabRightValue,
        ref reachRightAxisUsed, RagdollParts.UPPER_RIGHT_ARM, RagdollParts.LOWER_RIGHT_ARM, true);

    private void HandleLeftSideReach() => HandlePlayerReach(punchingLeft, inputHandler.GrabLeftValue,
        ref reachLeftAxisUsed, RagdollParts.UPPER_LEFT_ARM, RagdollParts.LOWER_LEFT_ARM, false);

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

        ConfigurableJoint bodyJoint = RagdollDict[RagdollParts.BODY].Joint;
        ConfigurableJoint upperArmJoint = RagdollDict[upperArmLabel].Joint;
        ConfigurableJoint lowerArmJoint = RagdollDict[lowerArmLabel].Joint;

        punchingArmState = punchingArmValue;
        int punchRotationMultiplier = isRightPunch ? -1 : 1;

        if (punchingArmValue)
        {
            bodyJoint.targetRotation = new Quaternion(-0.15f, 0.15f * punchRotationMultiplier, 0, 1);
            upperArmJoint.targetRotation = new Quaternion(0.62f * punchRotationMultiplier, -0.51f, 0.02f, 1);
            lowerArmJoint.targetRotation =
                new Quaternion(-1.31f * punchRotationMultiplier, 0.5f, 0.5f * punchRotationMultiplier, 1);
        }

        else
        {
            bodyJoint.targetRotation = new Quaternion(-0.15f, -0.15f * punchRotationMultiplier, 0, 1);
            upperArmJoint.targetRotation = new Quaternion(-0.74f * punchRotationMultiplier, 0.04f, 0f, 1);
            lowerArmJoint.targetRotation = new Quaternion(-0.2f * punchRotationMultiplier, 0, 0, 1);

            handRigidbody.AddForce(RagdollDict[RagdollParts.ROOT].transform.forward * punchForce, ForceMode.Impulse);
            RagdollDict[RagdollParts.BODY].Rigidbody
                .AddForce(RagdollDict[RagdollParts.BODY].transform.forward * punchForce, ForceMode.Impulse);

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
            RagdollParts.LOWER_LEFT_ARM, leftHand,
            () => UpperLeftArmTarget, () => LowerLeftArmTarget);

    private void HandleRightPunch() => HandlePunch(ref punchingRight, inputHandler.PunchRightValue, true,
        RagdollParts.UPPER_RIGHT_ARM,
        RagdollParts.LOWER_RIGHT_ARM, rightHand, () => UpperRightArmTarget, () => LowerLeftArmTarget);

    private void PerformWalking()
    {
        if (inAir)
            return;

        if (WalkForward)
        {
            WalkForwards();
        }

        if (WalkBackward)
        {
            WalkBackwards();
        }

        if (StepRight)
        {
            TakeStepRight();
        }
        else
        {
            ResetStepRight();
        }

        if (StepLeft)
        {
            TakeStepLeft();
        }
        else
        {
            ResetStepLeft();
        }
    }

    private void ResetStepLeft() =>
        ResetStep(RagdollParts.UPPER_LEFT_LEG, RagdollParts.LOWER_LEFT_LEG, in UpperLeftLegTarget,
            in LowerLeftLegTarget, 7f, 18f);

    private void ResetStepRight() => ResetStep(RagdollParts.UPPER_RIGHT_LEG, RagdollParts.LOWER_RIGHT_LEG,
        in UpperRightLegTarget,
        in LowerRightLegTarget, 8f, 17f);

    private void ResetStep(string upperLegLabel,
        string lowerLegLabel,
        in Quaternion upperLegTarget,
        in Quaternion lowerLegTarget,
        float upperLegLerpMultiplier,
        float lowerLegLerpMultiplier)
    {
        RagdollDict[upperLegLabel].Joint.targetRotation = Quaternion.Lerp(
            RagdollDict[upperLegLabel].Joint.targetRotation, upperLegTarget,
            upperLegLerpMultiplier * Time.fixedDeltaTime);
        RagdollDict[lowerLegLabel].Joint.targetRotation = Quaternion.Lerp(
            RagdollDict[lowerLegLabel].Joint.targetRotation, lowerLegTarget,
            lowerLegLerpMultiplier * Time.fixedDeltaTime);

        Vector3 feetForce = -Vector3.up * (FeetMountForce * Time.deltaTime);
        RagdollDict[RagdollParts.RIGHT_FOOT].Rigidbody.AddForce(feetForce, ForceMode.Impulse);
        RagdollDict[RagdollParts.LEFT_FOOT].Rigidbody.AddForce(feetForce, ForceMode.Impulse);
    }

    private void TakeStepLeft() => TakeStep(ref Step_L_timer, RagdollParts.LEFT_FOOT, ref StepLeft, ref StepRight,
        RagdollParts.UPPER_LEFT_LEG,
        RagdollParts.LOWER_LEFT_LEG, RagdollParts.UPPER_RIGHT_LEG);

    private void TakeStepRight() => TakeStep(ref Step_R_timer, RagdollParts.RIGHT_FOOT, ref StepRight, ref StepLeft,
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
        RagdollDict[footLabel].Rigidbody
            .AddForce(-Vector3.up * (FeetMountForce * Time.deltaTime), ForceMode.Impulse);

        var upperLegJoint = RagdollDict[upperJointLabel].Joint;
        var upperLegJointTargetRotation = upperLegJoint.targetRotation;

        var lowerLegJoint = RagdollDict[lowerJointLabel].Joint;
        var lowerLegJointTargetRotation = lowerLegJoint.targetRotation;

        var upperOppositeLegJoint = RagdollDict[upperOppositeJointLabel].Joint;
        var upperOppositeLegJointTargetRotation = upperOppositeLegJoint.targetRotation;

        bool isWalking = WalkForward || WalkBackward;

        if (WalkForward)
        {
            upperLegJointTargetRotation = upperLegJointTargetRotation.DisplaceX(0.09f * StepHeight);
            lowerLegJointTargetRotation = lowerLegJointTargetRotation.DisplaceX(-0.09f * StepHeight * 2);
            upperOppositeLegJointTargetRotation =
                upperOppositeLegJointTargetRotation.DisplaceX(-0.12f * StepHeight / 2);
        }

        if (WalkBackward)
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

    private void Walk(
        string forwardFootLabel,
        string backFootLabel,
        ref bool forwardFootState,
        ref bool backFootState,
        ref bool forwardAlertLeg,
        ref bool backAlertLeg)
    {
        bool forwardFootIsBehind = RagdollDict[forwardFootLabel].transform.position.z <
                                   RagdollDict[backFootLabel].transform.position.z;
        bool forwardFootIsAhead = RagdollDict[forwardFootLabel].transform.position.z >
                                  RagdollDict[backFootLabel].transform.position.z;

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

    private void WalkBackwards() => Walk(RagdollParts.LEFT_FOOT, RagdollParts.RIGHT_FOOT, ref StepLeft, ref StepRight,
        ref Alert_Leg_Left,
        ref Alert_Leg_Right);

    private void WalkForwards() => Walk(RagdollParts.RIGHT_FOOT, RagdollParts.LEFT_FOOT, ref StepRight, ref StepLeft,
        ref Alert_Leg_Right,
        ref Alert_Leg_Left);

    private void PerformStepPrediction()
    {
        if (!WalkForward && !WalkBackward)
        {
            StepRight = false;
            StepLeft = false;
            Step_R_timer = 0;
            Step_L_timer = 0;
            Alert_Leg_Right = false;
            Alert_Leg_Left = false;
        }

        if (centerOfMass.position.z < RagdollDict[RagdollParts.RIGHT_FOOT].transform.position.z &&
            centerOfMass.position.z < RagdollDict[RagdollParts.LEFT_FOOT].transform.position.z)
        {
            WalkBackward = true;
        }
        else
        {
            if (!isKeyDown)
            {
                WalkBackward = false;
            }
        }

        if (centerOfMass.position.z > RagdollDict[RagdollParts.RIGHT_FOOT].transform.position.z &&
            centerOfMass.position.z > RagdollDict[RagdollParts.LEFT_FOOT].transform.position.z)
        {
            WalkForward = true;
        }
        else
        {
            if (!isKeyDown)
            {
                WalkForward = false;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCenterOfMass()
    {
        Vector3 massPositionDisplacement = Vector3.zero;
        float totalMass = 0;

        foreach (var element in RagdollDict)
        {
            var joint = element.Value;
            var mass = joint.Rigidbody.mass;
            massPositionDisplacement += mass * joint.transform.position;
            totalMass += mass;
        }

        CenterOfMassPoint = (massPositionDisplacement / totalMass);
        centerOfMass.position = CenterOfMassPoint;
    }

    private void ResetWalkCycle()
    {
        if (!WalkForward && !WalkBackward)
        {
            StepRight = false;
            StepLeft = false;
            Step_R_timer = 0;
            Step_L_timer = 0;
            Alert_Leg_Right = false;
            Alert_Leg_Left = false;
        }
    }
}