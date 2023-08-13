using UnityEngine;

public class RagdollFeetContact : MonoBehaviour
{
    [SerializeField] private RagdollController RagdollPlayer;
    private const string GROUND = "Ground";
    
    void OnCollisionEnter(Collision col)
    {
        if(!RagdollPlayer.isJumping && RagdollPlayer.inAir)
        {
            if(col.gameObject.layer == LayerMask.NameToLayer(GROUND))
            {
                RagdollPlayer.PlayerLanded();
            }
        }
    }
}
