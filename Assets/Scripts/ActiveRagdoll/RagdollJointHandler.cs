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
    }
}