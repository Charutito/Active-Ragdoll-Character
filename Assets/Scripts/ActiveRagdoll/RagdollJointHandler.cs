using System.Runtime.CompilerServices;
using UnityEngine;

namespace ActiveRagdoll
{
    [System.Serializable]
    public class RagdollJointHandler
    {
        [SerializeField, Tooltip("The balancing strength to be used by this ragdoll")]
        private float balanceStrength = 5000f;

        [SerializeField, Tooltip("The force to be applied in order to stiffen the core of the ragdoll")]
        private float coreStrength = 1500f;

        [SerializeField, Tooltip("Mati llena este dato porque no se que es xd")]
        private float limbStrength = 500f;

        [SerializeField] private float armReachStiffness = 2000f;

        [SerializeField] private UDictionary<string, RagdollJoint> joints = new();

        internal JointDrive BalanceOn;
        internal JointDrive PoseOn;
        internal JointDrive CoreStiffness;
        internal JointDrive ReachStiffness;
        internal JointDrive DriveOff;


        public RagdollJointHandler()
        {
            Init();
        }

        private void Init()
        {
            BalanceOn = CreateJointDrive(balanceStrength);
            PoseOn = CreateJointDrive(limbStrength);
            CoreStiffness = CreateJointDrive(coreStrength);
            ReachStiffness = CreateJointDrive(armReachStiffness);
            DriveOff = CreateJointDrive(25);
        }

        internal void SetJointAngularDrives(string jointName, in JointDrive jointDrive)
        {
            joints[jointName].Joint.angularXDrive = jointDrive;
            joints[jointName].Joint.angularYZDrive = jointDrive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Quaternion GetJointTargetRotation(string jointName)
        {
            return joints[jointName].Joint.targetRotation;
        }

        private static JointDrive CreateJointDrive(float positionSpring)
        {
            var jointDrive = new JointDrive
            {
                positionSpring = positionSpring,
                positionDamper = 0,
                maximumForce = Mathf.Infinity
            };
            return jointDrive;
        }

        internal bool TryGetJointWithID(string jointID, out RagdollJoint joint)
        {
            bool gotJoint = joints.TryGetValue(jointID, out joint);
#if UNITY_EDITOR || DEBUG
            if (!gotJoint)
                Debug.LogWarning($"Joint with id {jointID} not found");
#endif
            return gotJoint;
        }

        /// <summary>
        /// Obtains a ragdoll joint with a specific ID. Only use if absolutely sure the joint exists
        /// </summary>
        /// <param name="jointID"></param>
        /// <returns>The specified <see cref="RagdollJoint"/>></returns>
        internal RagdollJoint GetRagdollJointWithID(string jointID)
        {
            if (!joints.TryGetValue(jointID, out var joint))
            {
#if UNITY_EDITOR || DEBUG
                Debug.LogWarning($"Ragdoll joint with id {jointID} not found");
#endif
            }

            return joint;
        }


        internal ConfigurableJoint GetConfigurableJointWithID(string jointID)
        {
            if (joints.TryGetValue(jointID, out var joint))
            {
                return joint.Joint;
            }

#if UNITY_EDITOR || DEBUG
            Debug.LogWarning($"Configurable joint for Ragdolljoint with id {jointID} not found");
#endif
            return default;
        }

        public void GetCenterOfMass(out Vector3 CenterOfMassPoint, Transform centerOfMass)
        {
            Vector3 massPositionDisplacement = Vector3.zero;
            float totalMass = 0;

            foreach (var element in joints)
            {
                var joint = element.Value;
                var mass = joint.Rigidbody.mass;
                massPositionDisplacement += mass * joint.transform.position;
                totalMass += mass;
            }

            CenterOfMassPoint = (massPositionDisplacement / totalMass);
            centerOfMass.position = CenterOfMassPoint;
        }
    }
}