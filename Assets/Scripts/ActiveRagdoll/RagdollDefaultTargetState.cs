using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace ActiveRagdoll
{
    public class RagdollDefaultTargetState
    {
        public Quaternion HeadTarget { get; private set; }
        public Quaternion BodyTarget { get; private set; }
        public Quaternion UpperRightArmTarget { get; private set; }
        public Quaternion LowerRightArmTarget { get; private set; }
        public Quaternion UpperLeftArmTarget { get; private set; }
        public Quaternion LowerLeftArmTarget { get; private set; }
        public Quaternion UpperRightLegTarget { get; private set; }
        public Quaternion LowerRightLegTarget { get; private set; }
        public Quaternion UpperLeftLegTarget { get; private set; }
        public Quaternion LowerLeftLegTarget { get; private set; }
        private UDictionary<string, RagdollJoint> ragdollJoints;
        public RagdollDefaultTargetState(UDictionary<string, RagdollJoint> ragdollJoints)
        {
            Assert.IsNotNull(ragdollJoints);
            this.ragdollJoints = ragdollJoints;
            SetupOriginalPose();
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
            ragdollJoints = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Quaternion GetJointTargetRotation(string jointName)
        {
            return ragdollJoints[jointName].Joint.targetRotation;
        }
    }
}