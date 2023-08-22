namespace ActiveRagdoll
{
    public class RagdollState
    {
        public bool jumping;
        public bool isJumping;
        public bool inAir;
        public bool punchingRight;
        public bool punchingLeft;
        internal float jumpingResetTimer;
        internal float mouseYAxisArms;
        internal float mouseXAxisArms;
        internal float mouseYAxisBody;
        internal bool isKeyDown;
        internal bool moveAxisUsed;
        internal bool jumpAxisUsed;
        internal bool reachLeftAxisUsed;
        internal bool reachRightAxisUsed;
    }
}