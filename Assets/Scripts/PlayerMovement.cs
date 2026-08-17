using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; 

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 15f;

    public float steerSpeed = 15f; // You might want to increase this slightly now that it accelerates smoothly!
    public float steerSmoothness = 10f; // How quickly the car reaches top steering speed
    public float boundaryX = 22f;
    
    [Header("Rotation Settings")]
    public float maxTurnAngle = 25f;
    public float turnSpeed = 1f;
    public float baseYRotation = -90f;

    [Header("Gravity Settings")]
    public float gravityForce = 40f; 
    public float flipCooldown = 2f;
    
    [Header("Juice & Feel")]
    public float flipVerticalBoost = 50f; 
    public float flipRotationSpeed = 12f; 

    [Header("Mobile Input")]
    public float minSwipeDistance = 50f; 
    public float tapDelay = 0.05f; 
    public float swipeDeadzone = 15f; 
    private double timeWhenResumed = 0;

    [Header("Game Manager Reference")]
    public GameManager gameManager;

    private float currentYRotation;
    private float currentXRotation; 
    private float currentLateralSpeed; // Tracks our smoothed sideways momentum
    private bool isUpsideDown = false;
    private float lastFlipTime = -10f;
    private Rigidbody rb;

    void Awake()
    {
        currentYRotation = baseYRotation;
        currentXRotation = 0f; 
        currentLateralSpeed = 0f;
        rb = GetComponent<Rigidbody>();
        
        Physics.gravity = new Vector3(0, -gravityForce, 0);
    }

    void Update()
    {
        float horizontalInput = 0f;
        bool wantsToFlipUp = false;
        bool wantsToFlipDown = false;
        bool keyboardToggle = false;

        // 1. Keyboard Input
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput = 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame) keyboardToggle = true;
        }

        // 2. Mobile Touch Input
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.None || phase == UnityEngine.InputSystem.TouchPhase.Canceled) 
                    continue;
                if (touch.startTime.ReadValue() < timeWhenResumed)
                {
                    continue;
                }
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                {
                    continue;
                }

                Vector2 touchPos = touch.position.ReadValue();
                Vector2 startPos = touch.startPosition.ReadValue();
                
                float yDifference = touchPos.y - startPos.y;
                float xDifference = touchPos.x - startPos.x;

                bool isDriftingVertically = Mathf.Abs(yDifference) > swipeDeadzone && Mathf.Abs(yDifference) > Mathf.Abs(xDifference);

                if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    if (isDriftingVertically && Mathf.Abs(yDifference) > minSwipeDistance)
                    {
                        if (yDifference > 0) wantsToFlipUp = true;
                        else wantsToFlipDown = true;
                    }
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Began || 
                         phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                         phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    double touchDuration = Time.realtimeSinceStartup - touch.startTime.ReadValue();
                    
                    if (!isDriftingVertically && touchDuration > tapDelay)
                    {
                        if (touchPos.x < Screen.width / 2f) horizontalInput = -1f;
                        else if (touchPos.x > Screen.width / 2f) horizontalInput = 1f;
                    }
                }
            }
        }

        // --- NEW SMOOTH MOVEMENT LOGIC ---
        if (transform.position.x >= boundaryX - 0.1f && horizontalInput > 0f) horizontalInput = 0f;
        if (transform.position.x <= -boundaryX + 0.1f && horizontalInput < 0f) horizontalInput = 0f;

        // 1. Calculate the speed we WANT to be going based on input
        float targetLateralSpeed = horizontalInput * steerSpeed;
        
        // 2. Smoothly transition our current speed towards that target speed
        currentLateralSpeed = Mathf.Lerp(currentLateralSpeed, targetLateralSpeed, steerSmoothness * Time.deltaTime);

        // 3. Calculate where the car will be next frame using the smoothed speed
        Vector3 newPosition = transform.position + (Vector3.right * currentLateralSpeed * Time.deltaTime);

        // 4. Clamp the X position so it absolutely cannot pass the boundary, replacing the old hacky input check
        newPosition.x = Mathf.Clamp(newPosition.x, -boundaryX, boundaryX);

        // 5. Apply the clamped position
        transform.position = newPosition;

        // Apply smooth Y rotation based on input
        float targetYRotation = baseYRotation + (horizontalInput * maxTurnAngle);
        currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, turnSpeed * Time.deltaTime);
        
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Playing)
        {
            // Calculate remaining time: (Time of last flip + 2 seconds) - Current Time
            float cooldownRemaining = Mathf.Max(0f, (lastFlipTime + flipCooldown) - Time.time);
            GameManager.Instance.UpdateGravityTimerText(cooldownRemaining);
        }
        // 3. Directional Gravity Flip Logic
        if (Time.time >= lastFlipTime + flipCooldown)
        {
            bool shouldExecuteFlip = false;

            if (wantsToFlipUp && !isUpsideDown) { isUpsideDown = true; shouldExecuteFlip = true; }
            else if (wantsToFlipDown && isUpsideDown) { isUpsideDown = false; shouldExecuteFlip = true; }
            else if (keyboardToggle) { isUpsideDown = !isUpsideDown; shouldExecuteFlip = true; }

            if (shouldExecuteFlip)
            {
                lastFlipTime = Time.time;
                float gravityDirection = isUpsideDown ? 1f : -1f;
                Physics.gravity = new Vector3(0, gravityForce * gravityDirection, 0);
                
                float verticalVelocity = isUpsideDown ? flipVerticalBoost : -flipVerticalBoost;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, verticalVelocity, rb.linearVelocity.z);
            }
        }

        // 4. Apply the Rotations
        float targetXRotation = isUpsideDown ? 180f : 0f;
        currentXRotation = Mathf.LerpAngle(currentXRotation, targetXRotation, flipRotationSpeed * Time.deltaTime);
        
        transform.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
    }
    
    public void PrepareForHacking()
    {
        // 1. Reset visual steering
        currentYRotation = baseYRotation;
        currentLateralSpeed = 0f; 
        transform.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);

        // 2. Kill all physical momentum and freeze the Rigidbody so it doesn't bounce on the moving track
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 3. Shoot a laser to find the nearest floor/ceiling and snap the car perfectly to it
        Vector3 rayDir = isUpsideDown ? Vector3.up : Vector3.down;
        if (Physics.Raycast(transform.position, rayDir, out RaycastHit hit, 20f))
        {
            // Calculates exactly half the height of your car so it rests perfectly on the surface
            float yOffset = GetComponent<Collider>().bounds.extents.y;
            float targetY = isUpsideDown ? hit.point.y - yOffset : hit.point.y + yOffset;
            
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }
    }

    public void ResumeFromHacking()
    {
        // 1. Unfreeze the physical car
        rb.isKinematic = false;
        
        // 2. Ensure gravity is pulling in the correct direction when physics awaken
        float gravityDirection = isUpsideDown ? 1f : -1f;
        Physics.gravity = new Vector3(0, gravityForce * gravityDirection, 0);

        // 3. Log the exact millisecond the game resumed to filter out previous UI clicks
        timeWhenResumed = Time.realtimeSinceStartup;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("obstacle"))
        {
            if (GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}