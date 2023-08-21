using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using Utils;

public class RagdollController : MonoBehaviour, IInputListener
{
    [SerializeField] private UDictionary<string, RagdollJoint> RagdollDict = new UDictionary<string, RagdollJoint>();

    [SerializeField] private Rigidbody rightHand;
    [SerializeField] private Rigidbody leftHand;

    [SerializeField] private Transform centerOfMass;

    [Header("Movement Properties")] public bool forwardIsCameraDirection = true;
    public float moveSpeed = 10f;
    public float turnSpeed = 6f;
    public float jumpForce = 18f;

    public Vector2 MovementAxis { get; set; } = Vector2.zero;
    public Vector2 AimAxis { get; set; }
    public float JumpValue { get; set; } = 0;
    public float GrabLeftValue { get; set; } = 0;
    public float GrabRightValue { get; set; } = 0;
    public bool PunchLeftValue { get; set; }
    public bool PunchRightValue { get; set; }

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

    private const string ROOT = "Root";
    private const string BODY = "Body";
    private const string HEAD = "Head";
    private const string UPPER_RIGHT_ARM = "UpperRightArm";
    private const string LOWER_RIGHT_ARM = "LowerRightArm";
    private const string UPPER_LEFT_ARM = "UpperLeftArm";
    private const string LOWER_LEFT_ARM = "LowerLeftArm";
    private const string UPPER_RIGHT_LEG = "UpperRightLeg";
    private const string LOWER_RIGHT_LEG = "LowerRightLeg";
    private const string UPPER_LEFT_LEG = "UpperLeftLeg";
    private const string LOWER_LEFT_LEG = "LowerLeftLeg";
    private const string RIGHT_FOOT = "RightFoot";
    private const string LEFT_FOOT = "LeftFoot";

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

    [HideInInspector] public bool jumping;
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool inAir;
    [HideInInspector] public bool punchingRight;
    [HideInInspector] public bool punchingLeft;

    [SerializeField] private Camera cam;
    private Vector3 Direction;
    private Vector3 CenterOfMassPoint;

    private JointDrive BalanceOn;
    private JointDrive PoseOn;
    private JointDrive CoreStiffness;
    private JointDrive ReachStiffness;
    private JointDrive DriveOff;

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

    private static int groundLayer;
    private WaitForSeconds punchDelayWaitTime = new WaitForSeconds(0.3f);

    void Awake()
    {
        //cam = Camera.main;
        InputManager.Instance.RegisterListener(this);
        groundLayer = LayerMask.NameToLayer("Ground");
        SetupJointDrives();
        SetupOriginalPose();
    }

    private void SetupJointDrives()
    {
        BalanceOn = JointDriveHelper.CreateJointDrive(balanceStrength);
        PoseOn = JointDriveHelper.CreateJointDrive(limbStrength);
        CoreStiffness = JointDriveHelper.CreateJointDrive(coreStrength);
        ReachStiffness = JointDriveHelper.CreateJointDrive(armReachStiffness);
        DriveOff = JointDriveHelper.CreateJointDrive(25);
    }

    private void SetupOriginalPose()
    {
        BodyTarget = GetJointTargetRotation(ROOT);
        HeadTarget = GetJointTargetRotation(HEAD);
        UpperRightArmTarget = GetJointTargetRotation(UPPER_RIGHT_ARM);
        LowerRightArmTarget = GetJointTargetRotation(LOWER_RIGHT_ARM);
        UpperLeftArmTarget = GetJointTargetRotation(UPPER_LEFT_ARM);
        LowerLeftArmTarget = GetJointTargetRotation(LOWER_LEFT_ARM);
        UpperRightLegTarget = GetJointTargetRotation(UPPER_RIGHT_LEG);
        LowerRightLegTarget = GetJointTargetRotation(LOWER_RIGHT_LEG);
        UpperLeftLegTarget = GetJointTargetRotation(UPPER_LEFT_LEG);
        LowerLeftLegTarget = GetJointTargetRotation(LOWER_LEFT_LEG);
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
            UpdateCenterOfMass();
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
        ConfigurableJoint rootJoint = RagdollDict[ROOT].Joint;

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

            if (MovementAxis.x != 0)
            {
                rootJointTargetRotation = Quaternion.Lerp(rootJointTargetRotation,
                    rootJointTargetRotation.DisplaceY(-MovementAxis.x * turnSpeed),
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
            RagdollDict[BODY].Joint.targetRotation = BodyTarget;
            RagdollDict[UPPER_RIGHT_ARM].Joint.targetRotation = UpperRightArmTarget;
            RagdollDict[LOWER_RIGHT_ARM].Joint.targetRotation = LowerRightArmTarget;
            RagdollDict[UPPER_LEFT_ARM].Joint.targetRotation = UpperLeftArmTarget;
            RagdollDict[LOWER_LEFT_ARM].Joint.targetRotation = LowerLeftArmTarget;

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
        if (JumpValue > 0)
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

            Rigidbody rootRigidbody = RagdollDict[ROOT].Rigidbody;
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
        Transform rootTransform = RagdollDict[ROOT].transform;
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
               RagdollDict[ROOT].Rigidbody.velocity.magnitude < 1f &&
               autoGetUpWhenPossible;
    }


    private void SetRagdollState(bool shouldRagdoll, ref JointDrive rootJointDrive, ref JointDrive poseJointDrive,
        bool shouldResetPose)
    {
        isRagdoll = shouldRagdoll;
        balanced = !shouldRagdoll;

        SetJointAngularDrives(ROOT, in rootJointDrive);
        SetJointAngularDrives(HEAD, in poseJointDrive);

        if (!reachRightAxisUsed)
        {
            SetJointAngularDrives(UPPER_RIGHT_ARM, in poseJointDrive);
            SetJointAngularDrives(LOWER_RIGHT_ARM, in poseJointDrive);
        }

        if (!reachLeftAxisUsed)
        {
            SetJointAngularDrives(UPPER_LEFT_ARM, in poseJointDrive);
            SetJointAngularDrives(LOWER_LEFT_ARM, in poseJointDrive);
        }

        SetJointAngularDrives(UPPER_RIGHT_LEG, in poseJointDrive);
        SetJointAngularDrives(LOWER_RIGHT_LEG, in poseJointDrive);
        SetJointAngularDrives(UPPER_LEFT_LEG, in poseJointDrive);
        SetJointAngularDrives(LOWER_LEFT_LEG, in poseJointDrive);
        SetJointAngularDrives(RIGHT_FOOT, in poseJointDrive);
        SetJointAngularDrives(LEFT_FOOT, in poseJointDrive);

        if (shouldResetPose)
            ResetPose = true;
    }

    private void DeactivateRagdoll() => SetRagdollState(false, ref BalanceOn, ref PoseOn, true);

    public void ActivateRagdoll() => SetRagdollState(true, ref DriveOff, ref DriveOff, false);

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
        Direction = RagdollDict[ROOT].transform.rotation * new Vector3(MovementAxis.x, 0.0f, MovementAxis.y);
        Direction.y = 0f;
        Rigidbody rootRigidbody = RagdollDict[ROOT].Rigidbody;
        var velocity = rootRigidbody.velocity;
        rootRigidbody.velocity = Vector3.Lerp(velocity,
            (Direction * moveSpeed) + new Vector3(0, velocity.y, 0), 0.8f);

        if (MovementAxis.x != 0 || MovementAxis.y != 0 && balanced)
        {
            StartWalkingForward();
        }
        else if (MovementAxis is { x: 0, y: 0 })
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
        if (MovementAxis.y != 0)
        {
            var rootRigidbody = RagdollDict[ROOT].Rigidbody;
            var v3 = rootRigidbody.transform.forward * (MovementAxis.y * moveSpeed);
            v3.y = rootRigidbody.velocity.y;
            rootRigidbody.velocity = v3;
        }

        if (MovementAxis.y > 0)
        {
            StartWalkingForwardInOwnDirection();
        }
        else if (MovementAxis.y < 0)
        {
            StartWalkingBackward();
        }
        else
        {
            StopWalking();
        }
    }

    private void StartWalkingForwardInOwnDirection() =>
        SetWalkMovingState(() => (!WalkForward && !moveAxisUsed), true, false, true, true, PoseOn);

    private void StartWalkingBackward() =>
        SetWalkMovingState(() => !WalkBackward && !moveAxisUsed, false, true, true, true, PoseOn);

    private void StopWalking() => SetWalkMovingState(() => WalkForward || WalkBackward && moveAxisUsed, false, false,
        false, false, DriveOff);

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
        SetJointAngularDrives(UPPER_RIGHT_LEG, in jointDrive);
        SetJointAngularDrives(LOWER_RIGHT_LEG, in jointDrive);
        SetJointAngularDrives(UPPER_LEFT_LEG, in jointDrive);
        SetJointAngularDrives(LOWER_LEFT_LEG, in jointDrive);
        SetJointAngularDrives(RIGHT_FOOT, in jointDrive);
        SetJointAngularDrives(LEFT_FOOT, in jointDrive);
    }

    private void PlayerReach()
    {
        MouseYAxisBody = Mathf.Clamp(MouseYAxisBody += (AimAxis.y / reachSensitivity), -0.2f, 0.1f);
        RagdollDict[BODY].Joint.targetRotation = new Quaternion(MouseYAxisBody, 0, 0, 1);

        if (GrabLeftValue != 0 && !punchingLeft)
        {
            if (!reachLeftAxisUsed)
            {
                SetJointAngularDrives(UPPER_LEFT_ARM, in ReachStiffness);
                SetJointAngularDrives(LOWER_LEFT_ARM, in ReachStiffness);
                SetJointAngularDrives(BODY, in CoreStiffness);
                reachLeftAxisUsed = true;
                reachLeftAxisUsed = true;
            }

            MouseYAxisArms = Mathf.Clamp(MouseYAxisArms += (AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            RagdollDict[UPPER_LEFT_ARM].Joint.targetRotation =
                new Quaternion(-0.58f - (MouseYAxisArms), -0.88f - (MouseYAxisArms), -0.8f, 1);
        }

        if (GrabLeftValue == 0 && !punchingLeft)
        {
            if (reachLeftAxisUsed)
            {
                if (balanced)
                {
                    SetJointAngularDrives(UPPER_LEFT_ARM, in PoseOn);
                    SetJointAngularDrives(LOWER_LEFT_ARM, in PoseOn);
                    SetJointAngularDrives(BODY, in PoseOn);
                }
                else if (!balanced)
                {
                    SetJointAngularDrives(UPPER_LEFT_ARM, in DriveOff);
                    SetJointAngularDrives(LOWER_LEFT_ARM, in DriveOff);
                }

                ResetPose = true;
                reachLeftAxisUsed = false;
            }
        }

        if (GrabRightValue != 0 && !punchingRight)
        {
            if (!reachRightAxisUsed)
            {
                SetJointAngularDrives(UPPER_RIGHT_ARM, in ReachStiffness);
                SetJointAngularDrives(LOWER_RIGHT_ARM, in ReachStiffness);
                SetJointAngularDrives(BODY, in CoreStiffness);
                reachRightAxisUsed = true;
            }

            MouseYAxisArms = Mathf.Clamp(MouseYAxisArms += (AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            RagdollDict[UPPER_RIGHT_ARM].Joint.targetRotation =
                new Quaternion(0.58f + (MouseYAxisArms), -0.88f - (MouseYAxisArms), 0.8f, 1);
        }

        if (GrabRightValue == 0 && !punchingRight)
        {
            if (reachRightAxisUsed)
            {
                if (balanced)
                {
                    SetJointAngularDrives(UPPER_RIGHT_ARM, in PoseOn);
                    SetJointAngularDrives(LOWER_RIGHT_ARM, in PoseOn);
                    SetJointAngularDrives(BODY, in PoseOn);
                }
                else if (!balanced)
                {
                    SetJointAngularDrives(UPPER_RIGHT_ARM, in DriveOff);
                    SetJointAngularDrives(LOWER_RIGHT_ARM, in DriveOff);
                }

                ResetPose = true;
                reachRightAxisUsed = false;
            }
        }
    }

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

        ConfigurableJoint bodyJoint = RagdollDict[BODY].Joint;
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

            handRigidbody.AddForce(RagdollDict[ROOT].transform.forward * punchForce, ForceMode.Impulse);
            RagdollDict[BODY].Rigidbody.AddForce(RagdollDict[BODY].transform.forward * punchForce, ForceMode.Impulse);

            StartCoroutine(PunchDelayCoroutine(isRightPunch, upperArmJoint, lowerArmJoint, upperArmTargetMethod,
                lowerArmTargetMethod));
        }
    }

    private IEnumerator PunchDelayCoroutine(bool isRightArm, ConfigurableJoint upperArmJoint,
        ConfigurableJoint lowerArmJoint, Func<Quaternion> upperArmTarget, Func<Quaternion> lowerArmTarget)
    {
        yield return punchDelayWaitTime;
        //Mainly because we can't pass in ref of bool value to coroutine, if not using unsafe void*
        bool punchValueToCheck = isRightArm ? PunchRightValue : PunchLeftValue;
        if (punchValueToCheck) yield break;

        upperArmJoint.targetRotation = upperArmTarget.Invoke();
        lowerArmJoint.targetRotation = lowerArmTarget.Invoke();
    }

    private void HandleLeftPunch() =>
        HandlePunch(ref punchingLeft, PunchLeftValue, false, UPPER_LEFT_ARM, LOWER_LEFT_ARM, leftHand,
            () => UpperLeftArmTarget, () => LowerLeftArmTarget);

    private void HandleRightPunch() => HandlePunch(ref punchingRight, PunchRightValue, true, UPPER_RIGHT_ARM,
        LOWER_RIGHT_ARM, rightHand, () => UpperRightArmTarget, () => LowerLeftArmTarget);

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
        ResetStep(UPPER_LEFT_LEG, LOWER_LEFT_LEG, in UpperLeftLegTarget, in LowerLeftLegTarget, 7f, 18f);

    private void ResetStepRight() => ResetStep(UPPER_RIGHT_LEG, LOWER_RIGHT_LEG, in UpperRightLegTarget,
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
        RagdollDict[RIGHT_FOOT].Rigidbody.AddForce(feetForce, ForceMode.Impulse);
        RagdollDict[LEFT_FOOT].Rigidbody.AddForce(feetForce, ForceMode.Impulse);
    }

    private void TakeStepLeft() => TakeStep(ref Step_L_timer, LEFT_FOOT, ref StepLeft, ref StepRight, UPPER_LEFT_LEG,
        LOWER_LEFT_LEG, UPPER_RIGHT_LEG);

    private void TakeStepRight() => TakeStep(ref Step_R_timer, RIGHT_FOOT, ref StepRight, ref StepLeft, UPPER_RIGHT_LEG,
        LOWER_RIGHT_LEG, UPPER_LEFT_LEG);

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

    private void WalkBackwards() => Walk(LEFT_FOOT, RIGHT_FOOT, ref StepLeft, ref StepRight, ref Alert_Leg_Left,
        ref Alert_Leg_Right);

    private void WalkForwards() => Walk(RIGHT_FOOT, LEFT_FOOT, ref StepRight, ref StepLeft, ref Alert_Leg_Right,
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

        if (centerOfMass.position.z < RagdollDict[RIGHT_FOOT].transform.position.z &&
            centerOfMass.position.z < RagdollDict[LEFT_FOOT].transform.position.z)
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

        if (centerOfMass.position.z > RagdollDict[RIGHT_FOOT].transform.position.z &&
            centerOfMass.position.z > RagdollDict[LEFT_FOOT].transform.position.z)
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
        //Be wary of this, it's called up to 2 times per frame
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