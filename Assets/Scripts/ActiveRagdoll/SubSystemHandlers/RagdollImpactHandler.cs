using UnityEngine;

namespace ActiveRagdoll
{
    [System.Serializable]
    public class RagdollImpactHandler
    {
        [field: SerializeField] public bool canBeKnockoutByImpact { get; private set; } = true;
        [field: SerializeField] public float requiredForceToBeKO { get; private set; } = 20f;
    }
}