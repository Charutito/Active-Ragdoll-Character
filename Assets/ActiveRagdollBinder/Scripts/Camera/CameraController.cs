using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Player To Follow")]
    public Transform PlayerRoot;
    
    [Header("Follow Properties")]
    public float distance = 15.0f;
    public float smoothness = 0.15f;
    
    [Header("Rotation Properties")]
    public bool rotateCamera = true;
    public float rotateSpeed = 5.0f;
    
    public float minAngle = -35.0f;
    public float maxAngle = -15.0f;
    
    [SerializeField] private Camera camera;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private Quaternion rotation;
    private Vector3 dir;
    private Vector3 offset;
    
    public void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        offset = camera.transform.position;
    }
    
    public void Update()
    {
        currentX += InputManager.Instance.AimValue.x * rotateSpeed;
        currentY += InputManager.Instance.AimValue.y * rotateSpeed;

        currentY = Mathf.Clamp(currentY, minAngle, maxAngle);
    }
    
    public void FixedUpdate()
    {
        if(rotateCamera)
        {
            dir = new Vector3(0, 0, -distance);
            rotation = Quaternion.Euler(-currentY, currentX, 0);
            camera.transform.position = Vector3.Lerp (camera.transform.position, PlayerRoot.position + rotation * dir, smoothness);
            camera.transform.LookAt(PlayerRoot.position);
        }
        
        if(!rotateCamera)
        {
            var targetRotation = Quaternion.LookRotation(PlayerRoot.position - camera.transform.position);
            camera.transform.position = Vector3.Lerp (camera.transform.position, PlayerRoot.position + offset, smoothness);
            camera.transform.rotation = Quaternion.Slerp(camera.transform.rotation, targetRotation, smoothness);
        }
    }
}
