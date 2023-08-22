using ActiveRagdoll;
using UnityEngine;

public class RagdollFeetContact : MonoBehaviour
{
    [SerializeField] private RagdollController RagdollPlayer;
    private const string GROUND = "Ground";
    private int groundLayer = -1;
    private RagdollState ragdollState;

    private int GroundLayer
    {
        get
        {
            if (groundLayer == -1)
            {
                groundLayer = LayerMask.NameToLayer(GROUND);
            }

            return groundLayer;
        }
    }

    public void Init(RagdollState state)
    {
        ragdollState = state;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (ragdollState.isJumping || !ragdollState.inAir)
            return;

        if (col.gameObject.layer == GroundLayer)
        {
            RagdollPlayer.PlayerLanded();
        }
    }
}