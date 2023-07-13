using System.Collections;
using UnityEngine;

public class RagdollController : MonoBehaviour, IInputListener
{
    [SerializeField] private UDictionary<string, RagdollJoint> RagdollDict = new UDictionary<string, RagdollJoint>();

    [SerializeField] private Rigidbody rightHand;
    [SerializeField] private Rigidbody leftHand;
    
    [SerializeField] private Transform centerOfMass;
    
    [Header("Hand Dependencies")]
    [SerializeField] private RagdollHandContact grabRight;
    [SerializeField] private RagdollHandContact grabLeft;

    [Header("Movement Properties")]
    public bool forwardIsCameraDirection = true;
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
    
    [Header("Balance Properties")]
    public bool autoGetUpWhenPossible = true;
    public bool useStepPrediction = true;
    public float balanceHeight = 2.5f;
    public float balanceStrength = 5000f;
    public float coreStrength = 1500f;
    public float limbStrength = 500f;

    public float StepDuration = 0.2f;
    public float StepHeight = 1.7f;
    public float FeetMountForce = 25f;
    
    [Header("Reach Properties")]
    public float reachSensitivity = 25f;
    public float armReachStiffness = 2000f;
    
    [Header("Actions")]
    public bool canBeKnockoutByImpact = true;
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
    
    private Camera cam;
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

    
    void Awake()
    {
        cam = Camera.main;
        InputManager.Instance.RegisterListener(this);

        SetupJointDrives();
        SetupOriginalPose();
    }

    private void SetupJointDrives()
    {
        BalanceOn = CreateJointDrive(balanceStrength);
        PoseOn = CreateJointDrive(limbStrength);
        CoreStiffness = CreateJointDrive(coreStrength);
        ReachStiffness = CreateJointDrive(armReachStiffness);
        DriveOff = CreateJointDrive(25);
    }

    private JointDrive CreateJointDrive(float positionSpring)
    {
        JointDrive jointDrive = new JointDrive();
        jointDrive.positionSpring = positionSpring;
        jointDrive.positionDamper = 0;
        jointDrive.maximumForce = Mathf.Infinity;
        return jointDrive;
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

    private Quaternion GetJointTargetRotation(string jointName)
    {
        return RagdollDict[jointName].Joint.targetRotation;
    }

    private void Update()
    {
        if(!inAir)
        {
            PlayerMovement();
            
            if(canPunch)
            {
                PerformPlayerPunch();
            }
        }

        PlayerReach();
        
        if(balanced && useStepPrediction)
        {
            PerformStepPrediction();
            UpdateCenterOfMass();
        }
        
        if(!useStepPrediction)
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
        if(forwardIsCameraDirection)
        {
            var lookPos = cam.transform.forward;
            lookPos.y = 0;
            var rotation = Quaternion.LookRotation(lookPos);
            RagdollDict[ROOT].Joint.targetRotation = Quaternion.Slerp(RagdollDict[ROOT].Joint.targetRotation, Quaternion.Inverse(rotation), Time.deltaTime * turnSpeed);
        }
        else
        {
            if (MovementAxis.x != 0)
            {
                RagdollDict[ROOT].Joint.targetRotation = Quaternion.Lerp(RagdollDict[ROOT].Joint.targetRotation, new Quaternion(RagdollDict[ROOT].Joint.targetRotation.x,RagdollDict[ROOT].Joint.targetRotation.y - (MovementAxis.x * turnSpeed), RagdollDict[ROOT].Joint.targetRotation.z, RagdollDict[ROOT].Joint.targetRotation.w), 6 * Time.fixedDeltaTime);
            }
            
            if(RagdollDict[ROOT].Joint.targetRotation.y < -0.98f)
            {
                RagdollDict[ROOT].Joint.targetRotation = new Quaternion(RagdollDict[ROOT].Joint.targetRotation.x, 0.98f, RagdollDict[ROOT].Joint.targetRotation.z, RagdollDict[ROOT].Joint.targetRotation.w);
            }

            else if(RagdollDict[ROOT].Joint.targetRotation.y > 0.98f)
            {
                RagdollDict[ROOT].Joint.targetRotation = new Quaternion(RagdollDict[ROOT].Joint.targetRotation.x, -0.98f, RagdollDict[ROOT].Joint.targetRotation.z, RagdollDict[ROOT].Joint.targetRotation.w);
            }
        }
    }
    
    private void ResetPlayerPose()
    {
        if(ResetPose && !jumping)
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
        if(CanResetPoseAfterLanding())
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
        if(JumpValue > 0)
        {
            if(!jumpAxisUsed)
            {
                if(balanced && !inAir)
                {
                    jumping = true;
                }
                
                else if(!balanced)
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
                
            var v3 = RagdollDict[ROOT].Rigidbody.transform.up * jumpForce;
            v3.x = RagdollDict[ROOT].Rigidbody.velocity.x;
            v3.z = RagdollDict[ROOT].Rigidbody.velocity.z;
            RagdollDict[ROOT].Rigidbody.velocity = v3;
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
    
    void GroundCheck()
    {
        Ray ray = new Ray (RagdollDict[ROOT].transform.position, -RagdollDict[ROOT].transform.up);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, balanceHeight, 1 << LayerMask.NameToLayer("Ground")) && !inAir && !isJumping && !reachRightAxisUsed && !reachLeftAxisUsed)
        {
            if(!balanced && RagdollDict[ROOT].Rigidbody.velocity.magnitude < 1f)
            {
                if(autoGetUpWhenPossible)
                {
                    balanced = true;
                }
            }
        }
        else if(!Physics.Raycast(ray, out hit, balanceHeight, 1 << LayerMask.NameToLayer("Ground")))
        {
            if(balanced)
            {
                balanced = false;
            }
        }
        
        if(balanced && isRagdoll)
        {
            DeactivateRagdoll();
        }
        else if(!balanced && !isRagdoll)
        {
            ActivateRagdoll();
        }
    }
    
    private void DeactivateRagdoll()
    {
        isRagdoll = false;
        balanced = true;

        SetJointAngularDrives(ROOT, BalanceOn);
        SetJointAngularDrives(HEAD, PoseOn);
        
        if (!reachRightAxisUsed)
        {
            SetJointAngularDrives(UPPER_RIGHT_ARM, PoseOn);
            SetJointAngularDrives(LOWER_RIGHT_ARM, PoseOn);
        }
        
        if (!reachLeftAxisUsed)
        {
            SetJointAngularDrives(UPPER_LEFT_ARM, PoseOn);
            SetJointAngularDrives(LOWER_LEFT_ARM, PoseOn);
        }
        
        SetJointAngularDrives(UPPER_RIGHT_LEG, PoseOn);
        SetJointAngularDrives(LOWER_RIGHT_LEG, PoseOn);
        SetJointAngularDrives(UPPER_LEFT_LEG, PoseOn);
        SetJointAngularDrives(LOWER_LEFT_LEG, PoseOn);
        SetJointAngularDrives(RIGHT_FOOT, PoseOn);
        SetJointAngularDrives(LEFT_FOOT, PoseOn);

        ResetPose = true;
    }

    public void ActivateRagdoll()
    {
        isRagdoll = true;
        balanced = false;

        SetJointAngularDrives(ROOT, DriveOff);
        SetJointAngularDrives(HEAD, DriveOff);
        
        if (!reachRightAxisUsed)
        {
            SetJointAngularDrives(UPPER_RIGHT_ARM, DriveOff);
            SetJointAngularDrives(LOWER_RIGHT_ARM, DriveOff);
        }
        
        if (!reachLeftAxisUsed)
        {
            SetJointAngularDrives(UPPER_LEFT_ARM, DriveOff);
            SetJointAngularDrives(LOWER_LEFT_ARM, DriveOff);
        }
        
        SetJointAngularDrives(UPPER_RIGHT_LEG, DriveOff);
        SetJointAngularDrives(LOWER_RIGHT_LEG, DriveOff);
        SetJointAngularDrives(UPPER_LEFT_LEG, DriveOff);
        SetJointAngularDrives(LOWER_LEFT_LEG, DriveOff);
        SetJointAngularDrives(RIGHT_FOOT, DriveOff);
        SetJointAngularDrives(LEFT_FOOT, DriveOff);
    }
    
    private void SetJointAngularDrives(string jointName, JointDrive jointDrive)
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
        RagdollDict[ROOT].Rigidbody.velocity = Vector3.Lerp(RagdollDict[ROOT].Rigidbody.velocity, (Direction * moveSpeed) + new Vector3(0, RagdollDict[ROOT].Rigidbody.velocity.y, 0), 0.8f);

        if (MovementAxis.x != 0 || MovementAxis.y != 0 && balanced)
        {
            StartWalkingForward();
        }
        else if (MovementAxis.x == 0 && MovementAxis.y == 0)
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
            var v3 = RagdollDict[ROOT].Rigidbody.transform.forward * (MovementAxis.y * moveSpeed);
            v3.y = RagdollDict[ROOT].Rigidbody.velocity.y;
            RagdollDict[ROOT].Rigidbody.velocity = v3;
        }

        if (MovementAxis.y > 0)
        {
            StartWalkingForwardInOwnDirection();
        }
        else if (MovementAxis.y < 0)
        {
            StartWalkingBackward();
        }
        else if (MovementAxis.y == 0)
        {
            StopWalking();
        }
    }

    private void StartWalkingForwardInOwnDirection()
    {
        if (!WalkForward && !moveAxisUsed)
        {
            WalkBackward = false;
            WalkForward = true;
            moveAxisUsed = true;
            isKeyDown = true;

            if (isRagdoll)
            {
                SetJointAngularDrivesForLegs(PoseOn);
            }
        }
    }

    private void StartWalkingBackward()
    {
        if (!WalkBackward && !moveAxisUsed)
        {
            WalkForward = false;
            WalkBackward = true;
            moveAxisUsed = true;
            isKeyDown = true;

            if (isRagdoll)
            {
                SetJointAngularDrivesForLegs(PoseOn);
            }
        }
    }

    private void StopWalking()
    {
        if (WalkForward || WalkBackward && moveAxisUsed)
        {
            WalkForward = false;
            WalkBackward = false;
            moveAxisUsed = false;
            isKeyDown = false;

            if (isRagdoll)
            {
                SetJointAngularDrivesForLegs(DriveOff);
            }
        }
    }

    private void SetJointAngularDrivesForLegs(JointDrive jointDrive)
    {
        SetJointAngularDrives(UPPER_RIGHT_LEG, jointDrive);
        SetJointAngularDrives(LOWER_RIGHT_LEG, jointDrive);
        SetJointAngularDrives(UPPER_LEFT_LEG, jointDrive);
        SetJointAngularDrives(LOWER_LEFT_LEG, jointDrive);
        SetJointAngularDrives(RIGHT_FOOT, jointDrive);
        SetJointAngularDrives(LEFT_FOOT, jointDrive);
    }
    
    private void PlayerReach()
    {
        MouseYAxisBody = Mathf.Clamp(MouseYAxisBody += (AimAxis.y / reachSensitivity), -0.9f, 0.9f);
        RagdollDict[BODY].Joint.targetRotation =  new Quaternion(MouseYAxisBody, 0, 0, 1);
        
        if(GrabLeftValue != 0 && !punchingLeft)
        {
            if(!reachLeftAxisUsed)
            {
                SetJointAngularDrives(UPPER_LEFT_ARM, ReachStiffness);
                SetJointAngularDrives(LOWER_LEFT_ARM, ReachStiffness);
                SetJointAngularDrives(BODY, CoreStiffness);
                reachLeftAxisUsed = true;
                reachLeftAxisUsed = true;
            }
            
            MouseYAxisArms = Mathf.Clamp(MouseYAxisArms += (AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            RagdollDict[UPPER_LEFT_ARM].Joint.targetRotation  = new Quaternion( -0.58f - (MouseYAxisArms), -0.88f - (MouseYAxisArms), -0.8f, 1);
        }
        
        if(GrabLeftValue == 0 && !punchingLeft)
        {
            if(reachLeftAxisUsed)
            {
                if(balanced)
                {
                    SetJointAngularDrives(UPPER_LEFT_ARM, PoseOn);
                    SetJointAngularDrives(LOWER_LEFT_ARM, PoseOn);
                    SetJointAngularDrives(BODY, PoseOn);
                }
                else if(!balanced)
                {
                    SetJointAngularDrives(UPPER_LEFT_ARM, DriveOff);
                    SetJointAngularDrives(LOWER_LEFT_ARM, DriveOff);
                }
                
                ResetPose = true;
                reachLeftAxisUsed = false;
            }
        }
        
        if(GrabRightValue != 0 && !punchingRight)
        {
            if(!reachRightAxisUsed)
            {
                SetJointAngularDrives(UPPER_RIGHT_ARM, ReachStiffness);
                SetJointAngularDrives(LOWER_RIGHT_ARM, ReachStiffness);
                SetJointAngularDrives(BODY, CoreStiffness);
                reachRightAxisUsed = true;
            }
            
            MouseYAxisArms = Mathf.Clamp(MouseYAxisArms += (AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            RagdollDict[UPPER_RIGHT_ARM].Joint.targetRotation = new Quaternion( 0.58f + (MouseYAxisArms), -0.88f - (MouseYAxisArms), 0.8f, 1);
        }
        
        if(GrabRightValue == 0 && !punchingRight)
        {
            if(reachRightAxisUsed)
            {
                if(balanced)
                {
                    SetJointAngularDrives(UPPER_RIGHT_ARM, PoseOn);
                    SetJointAngularDrives(LOWER_RIGHT_ARM, PoseOn);
                    SetJointAngularDrives(BODY, PoseOn);
                }
                else if(!balanced)
                {
                    SetJointAngularDrives(UPPER_RIGHT_ARM, DriveOff);
                    SetJointAngularDrives(LOWER_RIGHT_ARM, DriveOff);
                }
                
                ResetPose = true;
                reachRightAxisUsed = false;
            }
        }
    }

    
    private void PerformPlayerPunch()
    {
        if(!punchingRight && PunchRightValue)
        {
            punchingRight= true;
            
            RagdollDict[BODY].Joint.targetRotation = new Quaternion( -0.15f, -0.15f, 0, 1);
            RagdollDict[UPPER_RIGHT_ARM].Joint.targetRotation = new Quaternion( -0.62f, -0.51f, 0.02f, 1);
            RagdollDict[LOWER_RIGHT_ARM].Joint.targetRotation = new Quaternion( 1.31f, 0.5f, -0.5f, 1);
        }
        
        if(punchingRight && !PunchRightValue)
        {
            punchingRight = false;
            
            RagdollDict[BODY].Joint.targetRotation = new Quaternion( -0.15f, 0.15f, 0, 1);
            RagdollDict[UPPER_RIGHT_ARM].Joint.targetRotation = new Quaternion( 0.74f, 0.04f, 0f, 1);
            RagdollDict[LOWER_RIGHT_ARM].Joint.targetRotation = new Quaternion( 0.2f, 0, 0, 1);
            
            rightHand.AddForce(RagdollDict[ROOT].transform.forward * punchForce, ForceMode.Impulse);
 
            RagdollDict[BODY].Rigidbody.AddForce(RagdollDict[ROOT].transform.forward * punchForce, ForceMode.Impulse);
			
            StartCoroutine(DelayCoroutine());
            IEnumerator DelayCoroutine()
            {
                yield return new WaitForSeconds(0.3f);
                if(!PunchRightValue)
                {
                    RagdollDict[UPPER_RIGHT_ARM].Joint.targetRotation = UpperRightArmTarget;
                    RagdollDict[LOWER_RIGHT_ARM].Joint.targetRotation = LowerRightArmTarget;
                }
            }
        }
        
        if(!punchingLeft && PunchLeftValue)
        {
            punchingLeft = true;
            
            RagdollDict[BODY].Joint.targetRotation = new Quaternion( -0.15f, 0.15f, 0, 1);
            RagdollDict[UPPER_LEFT_ARM].Joint.targetRotation = new Quaternion( 0.62f, -0.51f, 0.02f, 1);
            RagdollDict[LOWER_LEFT_ARM].Joint.targetRotation = new Quaternion( -1.31f, 0.5f, 0.5f, 1);
        }
        
        if(punchingLeft && !PunchLeftValue)
        {
            punchingLeft = false;
            
            RagdollDict[BODY].Joint.targetRotation = new Quaternion( -0.15f, -0.15f, 0, 1);
            RagdollDict[UPPER_LEFT_ARM].Joint.targetRotation = new Quaternion( -0.74f, 0.04f, 0f, 1);
            RagdollDict[LOWER_LEFT_ARM].Joint.targetRotation = new Quaternion( -0.2f, 0, 0, 1);
            
            leftHand.AddForce(RagdollDict[ROOT].transform.forward * punchForce, ForceMode.Impulse);
 
            RagdollDict[BODY].Rigidbody.AddForce(RagdollDict[BODY].transform.forward * punchForce, ForceMode.Impulse);
			
            StartCoroutine(DelayCoroutine());
            IEnumerator DelayCoroutine()
            {
                yield return new WaitForSeconds(0.3f);
                if(!PunchLeftValue)
                {
                    RagdollDict[UPPER_LEFT_ARM].Joint.targetRotation = UpperLeftArmTarget;
                    RagdollDict[LOWER_LEFT_ARM].Joint.targetRotation = LowerLeftArmTarget;
                }
            }
        }
    }
    
    private void PerformWalking()
    {
        if (!inAir)
        {
            if (WalkForward)
            {
                if (RagdollDict[RIGHT_FOOT].transform.position.z < RagdollDict[LEFT_FOOT].transform.position.z && !StepLeft && !Alert_Leg_Right)
                {
                    StepRight = true;
                    Alert_Leg_Right = true;
                    Alert_Leg_Left = true;
                }
                
                if (RagdollDict[RIGHT_FOOT].transform.position.z > RagdollDict[LEFT_FOOT].transform.position.z && !StepRight && !Alert_Leg_Left)
                {
                    StepLeft = true;
                    Alert_Leg_Left = true;
                    Alert_Leg_Right = true;
                }
            }

            if (WalkBackward)
            {
                if (RagdollDict[RIGHT_FOOT].transform.position.z > RagdollDict[LEFT_FOOT].transform.position.z && !StepLeft && !Alert_Leg_Right)
                {
                    StepRight = true;
                    Alert_Leg_Right = true;
                    Alert_Leg_Left = true;
                }
                
                if (RagdollDict[RIGHT_FOOT].transform.position.z < RagdollDict[LEFT_FOOT].transform.position.z && !StepRight && !Alert_Leg_Left)
                {
                    StepLeft = true;
                    Alert_Leg_Left = true;
                    Alert_Leg_Right = true;
                }
            }
            
            if (StepRight)
            {
                Step_R_timer += Time.fixedDeltaTime;
                RagdollDict[RIGHT_FOOT].Rigidbody.AddForce(-Vector3.up * (FeetMountForce * Time.deltaTime), ForceMode.Impulse);

                if (WalkForward)
                {                
                    RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.x + 0.09f * StepHeight, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.y, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.z, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.w);
                    RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation.x - 0.09f * StepHeight * 2, RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation.y, RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation.z, RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation.w);
                    RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.x - 0.12f * StepHeight / 2, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.y, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.z, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.w);
                }
                
                if (WalkBackward)
                {
                    RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.x - 0.00f * StepHeight, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.y, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.z, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.w);
                    RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation.x - 0.07f * StepHeight * 2, RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation.y, RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation.z, RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation.w);
                    RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.x + 0.02f * StepHeight / 2, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.y, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.z, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.w);
                }
                
                if (Step_R_timer > StepDuration)
                {
                    Step_R_timer = 0;
                    StepRight = false;

                    if (WalkForward || WalkBackward)
                    {
                        StepLeft = true;
                    }
                }
            }
            else
            {
                RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation = Quaternion.Lerp(RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation, UpperRightLegTarget, (8f) * Time.fixedDeltaTime);
                RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation = Quaternion.Lerp(RagdollDict[LOWER_RIGHT_LEG].Joint.targetRotation, LowerRightLegTarget, (17f) * Time.fixedDeltaTime);
                
                RagdollDict[RIGHT_FOOT].Rigidbody.AddForce(-Vector3.up * (FeetMountForce * Time.deltaTime), ForceMode.Impulse);
                RagdollDict[LEFT_FOOT].Rigidbody.AddForce(-Vector3.up * (FeetMountForce * Time.deltaTime), ForceMode.Impulse);
            }

            if (StepLeft)
            {
                Step_L_timer += Time.fixedDeltaTime;

                RagdollDict[LEFT_FOOT].Rigidbody.AddForce(-Vector3.up * (FeetMountForce * Time.deltaTime), ForceMode.Impulse);

                if (WalkForward)
                {
                    RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.x + 0.09f * StepHeight, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.y, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.z, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.w);
                    RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation.x - 0.09f * StepHeight * 2, RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation.y, RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation.z, RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation.w);
                    RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.x - 0.12f * StepHeight / 2, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.y, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.z, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.w);
                }
                
                if (WalkBackward)
                {
                    RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.x - 0.00f * StepHeight, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.y, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.z, RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation.w);
                    RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation.x - 0.07f * StepHeight * 2, RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation.y, RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation.z, RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation.w);
                    RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation = new Quaternion(RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.x + 0.02f * StepHeight / 2, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.y, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.z, RagdollDict[UPPER_RIGHT_LEG].Joint.targetRotation.w);
                }

                if (Step_L_timer > StepDuration)
                {
                    Step_L_timer = 0;
                    StepLeft = false;

                    if (WalkForward || WalkBackward)
                    {
                        StepRight = true;
                    }
                }
            }
            else
            {
                RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation = Quaternion.Lerp(RagdollDict[UPPER_LEFT_LEG].Joint.targetRotation, UpperLeftLegTarget, (7f) * Time.fixedDeltaTime);
                RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation = Quaternion.Lerp(RagdollDict[LOWER_LEFT_LEG].Joint.targetRotation, LowerLeftLegTarget, (18f) * Time.fixedDeltaTime);

                RagdollDict[RIGHT_FOOT].Rigidbody.AddForce(-Vector3.up * (FeetMountForce * Time.deltaTime), ForceMode.Impulse);
                RagdollDict[LEFT_FOOT].Rigidbody.AddForce(-Vector3.up * (FeetMountForce * Time.deltaTime), ForceMode.Impulse);
            }
        }
    }
    
    private void PerformStepPrediction()
    {
        if(!WalkForward && !WalkBackward)
        {
            StepRight = false;
            StepLeft = false;
            Step_R_timer = 0;
            Step_L_timer = 0;
            Alert_Leg_Right = false;
            Alert_Leg_Left = false;
        }
        
        if (centerOfMass.position.z < RagdollDict[RIGHT_FOOT].transform.position.z && centerOfMass.position.z < RagdollDict[LEFT_FOOT].transform.position.z)
        {
            WalkBackward = true;
        }
        else
        {
            if(!isKeyDown)
            {
                WalkBackward = false;
            }
        }

        if (centerOfMass.position.z > RagdollDict[RIGHT_FOOT].transform.position.z && centerOfMass.position.z > RagdollDict[LEFT_FOOT].transform.position.z)
        {
            WalkForward = true;
        }
        else
        {
            if(!isKeyDown)
            {
                WalkForward = false;
            }
        }
    }

    private void UpdateCenterOfMass()
    {
        CenterOfMassPoint =
            (RagdollDict[ROOT].Rigidbody.mass * RagdollDict[ROOT].transform.position + 
             RagdollDict[BODY].Rigidbody.mass * RagdollDict[BODY].transform.position +
             RagdollDict[HEAD].Rigidbody.mass * RagdollDict[HEAD].transform.position +
             RagdollDict[UPPER_RIGHT_ARM].Rigidbody.mass * RagdollDict[UPPER_RIGHT_ARM].transform.position +
             RagdollDict[LOWER_RIGHT_ARM].Rigidbody.mass * RagdollDict[LOWER_RIGHT_ARM].transform.position +
             RagdollDict[UPPER_LEFT_ARM].Rigidbody.mass * RagdollDict[UPPER_LEFT_ARM].transform.position +
             RagdollDict[LOWER_LEFT_ARM].Rigidbody.mass * RagdollDict[LOWER_LEFT_ARM].transform.position +
             RagdollDict[UPPER_RIGHT_LEG].Rigidbody.mass * RagdollDict[UPPER_RIGHT_LEG].transform.position +
             RagdollDict[LOWER_RIGHT_LEG].Rigidbody.mass * RagdollDict[LOWER_RIGHT_LEG].transform.position +
             RagdollDict[UPPER_LEFT_LEG].Rigidbody.mass * RagdollDict[UPPER_LEFT_LEG].transform.position +
             RagdollDict[LOWER_LEFT_LEG].Rigidbody.mass * RagdollDict[LOWER_LEFT_LEG].transform.position +
             RagdollDict[RIGHT_FOOT].Rigidbody.mass * RagdollDict[RIGHT_FOOT].transform.position +
             RagdollDict[LEFT_FOOT].Rigidbody.mass * RagdollDict[LEFT_FOOT].transform.position) 
            
            /
            
            (RagdollDict[ROOT].Rigidbody.mass + RagdollDict[BODY].Rigidbody.mass +
             RagdollDict[HEAD].Rigidbody.mass + RagdollDict[UPPER_RIGHT_ARM].Rigidbody.mass +
             RagdollDict[LOWER_RIGHT_ARM].Rigidbody.mass + RagdollDict[UPPER_LEFT_ARM].Rigidbody.mass +
             RagdollDict[LOWER_LEFT_ARM].Rigidbody.mass + RagdollDict[UPPER_RIGHT_LEG].Rigidbody.mass +
             RagdollDict[LOWER_RIGHT_LEG].Rigidbody.mass + RagdollDict[UPPER_LEFT_LEG].Rigidbody.mass +
             RagdollDict[LOWER_LEFT_LEG].Rigidbody.mass + RagdollDict[RIGHT_FOOT].Rigidbody.mass +
             RagdollDict[LEFT_FOOT].Rigidbody.mass);

        centerOfMass.position = CenterOfMassPoint;
    }
    
    private void ResetWalkCycle()
    {
        if(!WalkForward && !WalkBackward)
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