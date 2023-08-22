using UnityEngine;

namespace ActiveRagdoll
{
    public class RagdollInputHandler : IInputListener
    {
        public Vector2 MovementAxis { get; set; }
        public Vector2 AimAxis { get; set; }
        public float JumpValue { get; set; }
        public float GrabLeftValue { get; set; }
        public float GrabRightValue { get; set; }
        public bool PunchLeftValue { get; set; }
        public bool PunchRightValue { get; set; }

        public void Init()
        {
            InputManager.Instance.RegisterListener(this);
        }
    }
}