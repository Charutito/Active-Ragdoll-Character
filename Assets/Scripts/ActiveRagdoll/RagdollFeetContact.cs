using UnityEngine;

public class RagdollFeetContact : MonoBehaviour
{
    [SerializeField] private RagdollController RagdollPlayer;
    private const string GROUND = "Ground";
    private int groundLayer = -1;

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

    private void OnCollisionEnter(Collision col)
    {
        if (!RagdollPlayer.isJumping && RagdollPlayer.inAir)
        {
            if (col.gameObject.layer == GroundLayer)
            {
                RagdollPlayer.PlayerLanded();
            }
        }
    }
}