using UnityEngine;

public class RagdollJoint : MonoBehaviour
{
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private ConfigurableJoint joint;
    
    public Rigidbody Rigidbody => rigidbody;
    public ConfigurableJoint Joint => joint;
}
