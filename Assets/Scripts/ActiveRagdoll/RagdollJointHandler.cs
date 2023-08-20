using UnityEngine;

namespace ActiveRagdoll
{
    [System.Serializable]
    public class RagdollJointHandler
    {
        [SerializeField] private float balanceStrength = 5000f;
        [SerializeField] private float coreStrength = 1500f;
        [SerializeField] private float limbStrength = 500f;
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