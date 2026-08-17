using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player; 
    public float smoothSpeed = 10f; 

    [Header("Dynamic Offsets")]
    public Vector3 groundOffset = new Vector3(0f, 4f, -8f);
    public Vector3 ceilingOffset = new Vector3(0f, -4f, -8f);
    
    [Header("Look Settings")]
    public float lookAheadDistance = 5f; 
    public float lookAngle = 15f; 
    private Vector3 currentOffset;

    void Start()
    {
        currentOffset = groundOffset;
    }

    void LateUpdate() 
    {
        if (player != null)
        {
            // THE FIX: Replaced Inspector dependency with direct Singleton call
            if (GameManager.Instance.currentState == GameManager.GameState.MainMenu || GameManager.Instance.currentState == GameManager.GameState.FirewallHacking || GameManager.Instance.currentState == GameManager.GameState.Tutorial)
            {
                float orbitSpeed = 15f;    
                float orbitDistance = 1.2f;  

                // 1. Check if the player is on the ceiling[cite: 6]
                bool isUpsideDown = player.position.y > 2f; 
                
                // 2. Invert the pitch angle if on the ceiling so the camera stays inside the tunnel
                float currentLookAngle = isUpsideDown ? -lookAngle : lookAngle;

                // 3. Apply the dynamic angle to the orbit rotation
                Quaternion orbitRotation = Quaternion.Euler(currentLookAngle, Time.time * orbitSpeed, 0f);
                Vector3 targetIdlePosition = player.position + (orbitRotation * new Vector3(0, 0, -orbitDistance));

                transform.position = Vector3.Lerp(transform.position, targetIdlePosition, smoothSpeed * Time.deltaTime);
                transform.LookAt(player.position);
                
                return;
            }
            else
            {
                bool isUpsideDown = player.position.y > 2f; 
                Vector3 targetOffset = isUpsideDown ? ceilingOffset : groundOffset;

                currentOffset = Vector3.Lerp(currentOffset, targetOffset, smoothSpeed * Time.deltaTime);

                Vector3 desiredPosition = player.position + currentOffset;
                transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

                Vector3 lookTarget = player.position + (Vector3.forward * lookAheadDistance);
                Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
                
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
            }
        }
    }
}