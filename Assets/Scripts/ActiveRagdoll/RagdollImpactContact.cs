using ActiveRagdoll;
using UnityEngine;

public class RagdollImpactContact : MonoBehaviour
{
    private RagdollImpactHandler ragdollImpactHandler;
    private RagdollLocomotionController locomotionController;

    public void Init(RagdollImpactHandler impactHandler, RagdollLocomotionController locomotionController)
    {
        ragdollImpactHandler = impactHandler;
        this.locomotionController = locomotionController;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (!ragdollImpactHandler.canBeKnockoutByImpact ||
            col.relativeVelocity.magnitude < ragdollImpactHandler.requiredForceToBeKO)
            return;

        locomotionController.ActivateRagdoll();
    }
}