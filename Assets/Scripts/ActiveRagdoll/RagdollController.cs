using System;
using System.Collections;
using System.Runtime.CompilerServices;
using ActiveRagdoll;
using UnityEngine;
using Utils;

public class RagdollController : MonoBehaviour
{
    [SerializeField] private RagdollJointHandler jointHandler;
    [SerializeField] private RagdollImpactHandler impactHandler;
    [SerializeField] private RagdollLocomotionController locomotionController;
    [SerializeField] private bool forwardIsCameraDirection = true;
    [SerializeField] private Transform centerOfMass;
    [SerializeField] private float turnSpeed = 6f;
    [Header("Reach Properties")] public float reachSensitivity = 25f;
    [Header("Actions")] public bool canPunch = true;
    public float punchForce = 15f;
    private readonly RagdollInputHandler inputHandler = new();
    private readonly RagdollState ragdollState = new();
    [SerializeField] private Camera cam;
    private Vector3 CenterOfMassPoint; //TODO: Check usage
    private RagdollDefaultTargetState defaultTargetState;
    private readonly WaitForSeconds punchDelayWaitTime = new(0.3f);
    private Rigidbody RightHandRigidBody => jointHandler.GetRigidBodyFromJoint(RagdollParts.RIGHT_HAND);
    private Rigidbody LeftHandRigidBody => jointHandler.GetRigidBodyFromJoint(RagdollParts.LEFT_HAND);

    private void Awake()
    {
        inputHandler.Init();
        defaultTargetState = new RagdollDefaultTargetState(jointHandler);
        locomotionController.Init(jointHandler, ragdollState, defaultTargetState);
        SetupHandContacts();
        SetupFeetContacts();
        SetupRagdollImpactContacts();
    }

    private void SetupRagdollImpactContacts()
    {
        var impactContacts = GetComponentsInChildren<RagdollImpactContact>();
        foreach (var impactContact in impactContacts)
        {
            impactContact.Init(impactHandler, locomotionController);
        }
    }

    private void SetupHandContacts()
    {
        var handContacts = GetComponentsInChildren<RagdollHandContact>();
        foreach (var handContact in handContacts)
        {
            handContact.Init(inputHandler, ragdollState);
        }
    }

    private void SetupFeetContacts()
    {
        var feetContacts = GetComponentsInChildren<RagdollFeetContact>();
        foreach (var feetContact in feetContacts)
        {
            feetContact.Init(ragdollState);
        }
    }

    private void Update()
    {
        if (!ragdollState.inAir)
        {
            locomotionController.PlayerMovement(forwardIsCameraDirection, inputHandler.MovementAxis);

            if (canPunch)
            {
                PerformPlayerPunch();
            }
        }

        PlayerReach();

        locomotionController.HandleStepPrediction(centerOfMass.transform.position.z);

        locomotionController.GroundCheck();
        UpdateCenterOfMass();
    }

    private void FixedUpdate()
    {
        locomotionController.PerformWalking();
        PerformPlayerRotation();
        ResetPlayerPose();
        locomotionController.PerformPlayerGetUpJumping(inputHandler.JumpValue);
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
        if (!locomotionController.resetPose || ragdollState.jumping)
            return;

        jointHandler.GetConfigurableJointWithID(RagdollParts.BODY).targetRotation = defaultTargetState.BodyTarget;
        jointHandler.GetConfigurableJointWithID(RagdollParts.UPPER_RIGHT_ARM).targetRotation =
            defaultTargetState.UpperRightArmTarget;
        jointHandler.GetConfigurableJointWithID(RagdollParts.LOWER_RIGHT_ARM).targetRotation =
            defaultTargetState.LowerRightArmTarget;
        jointHandler.GetConfigurableJointWithID(RagdollParts.UPPER_LEFT_ARM).targetRotation =
            defaultTargetState.UpperLeftArmTarget;
        jointHandler.GetConfigurableJointWithID(RagdollParts.LOWER_LEFT_ARM).targetRotation =
            defaultTargetState.LowerLeftArmTarget;

        ragdollState.mouseYAxisArms = 0;
        locomotionController.resetPose = false;
    }

    public void PlayerLanded()
    {
        if (CanResetPoseAfterLanding())
        {
            ragdollState.inAir = false;
            locomotionController.resetPose = true;
        }
    }

    private bool CanResetPoseAfterLanding()
    {
        return ragdollState.inAir && !ragdollState.isJumping && !ragdollState.jumping;
    }

    private void PlayerReach()
    {
        ragdollState.mouseYAxisBody =
            Mathf.Clamp(ragdollState.mouseYAxisBody += (inputHandler.AimAxis.y / reachSensitivity), -0.2f, 0.1f);
        jointHandler.GetConfigurableJointWithID(RagdollParts.BODY).targetRotation =
            new Quaternion(ragdollState.mouseYAxisBody, 0, 0, 1);

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
            ragdollState.mouseYAxisArms =
                Mathf.Clamp(ragdollState.mouseYAxisArms += (inputHandler.AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            jointHandler.GetConfigurableJointWithID(upperArmJoint).targetRotation =
                new Quaternion((0.58f + (ragdollState.mouseYAxisArms)) * multiplier,
                    -0.88f - (ragdollState.mouseYAxisArms), 0.8f * multiplier,
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


    private void HandleRightSideReach() => HandlePlayerReach(ragdollState.punchingRight, inputHandler.GrabRightValue,
        ref ragdollState.reachRightAxisUsed, RagdollParts.UPPER_RIGHT_ARM, RagdollParts.LOWER_RIGHT_ARM, true);

    private void HandleLeftSideReach() => HandlePlayerReach(ragdollState.punchingLeft, inputHandler.GrabLeftValue,
        ref ragdollState.reachLeftAxisUsed, RagdollParts.UPPER_LEFT_ARM, RagdollParts.LOWER_LEFT_ARM, false);

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
        HandlePunch(ref ragdollState.punchingLeft, inputHandler.PunchLeftValue, false, RagdollParts.UPPER_LEFT_ARM,
            RagdollParts.LOWER_LEFT_ARM, LeftHandRigidBody,
            () => defaultTargetState.UpperLeftArmTarget, () => defaultTargetState.LowerLeftArmTarget);

    private void HandleRightPunch() => HandlePunch(ref ragdollState.punchingRight, inputHandler.PunchRightValue, true,
        RagdollParts.UPPER_RIGHT_ARM,
        RagdollParts.LOWER_RIGHT_ARM, RightHandRigidBody, () => defaultTargetState.UpperRightArmTarget,
        () => defaultTargetState.LowerLeftArmTarget);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCenterOfMass()
    {
        jointHandler.GetCenterOfMass(out CenterOfMassPoint, centerOfMass);
    }
}