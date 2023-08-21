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

        public RagdollDefaultTargetState(RagdollJointHandler jointHandler)
        {
            SetupOriginalPose(jointHandler);
        }

        private void SetupOriginalPose(RagdollJointHandler jointsHandler)
        {
            BodyTarget = jointsHandler.GetJointTargetRotation(RagdollParts.ROOT);
            HeadTarget = jointsHandler.GetJointTargetRotation(RagdollParts.HEAD);
            UpperRightArmTarget = jointsHandler.GetJointTargetRotation(RagdollParts.UPPER_RIGHT_ARM);
            LowerRightArmTarget = jointsHandler.GetJointTargetRotation(RagdollParts.LOWER_RIGHT_ARM);
            UpperLeftArmTarget = jointsHandler.GetJointTargetRotation(RagdollParts.UPPER_LEFT_ARM);
            LowerLeftArmTarget = jointsHandler.GetJointTargetRotation(RagdollParts.LOWER_LEFT_ARM);
            UpperRightLegTarget = jointsHandler.GetJointTargetRotation(RagdollParts.UPPER_RIGHT_LEG);
            LowerRightLegTarget = jointsHandler.GetJointTargetRotation(RagdollParts.LOWER_RIGHT_LEG);
            UpperLeftLegTarget = jointsHandler.GetJointTargetRotation(RagdollParts.UPPER_LEFT_LEG);
            LowerLeftLegTarget = jointsHandler.GetJointTargetRotation(RagdollParts.LOWER_LEFT_LEG);
        }
    }
}