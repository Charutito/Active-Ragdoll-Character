using UnityEngine;

public class RagdollImpactContact : MonoBehaviour
{
    public RagdollController ragdollController;

    void OnCollisionEnter(Collision col)
    {
        if (ragdollController.canBeKnockoutByImpact && col.relativeVelocity.magnitude > ragdollController.requiredForceToBeKO)
        {
            ragdollController.ActivateRagdoll();
        }
    }
}
