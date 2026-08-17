using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FirewallPuzzleController : MonoBehaviour
{
    [Header("Door References")]
    [SerializeField] private Transform doorMesh; 

    [Header("Door Animation")]
    // This defines how far the door slides. Positive X is usually to the right.
    [SerializeField] private Vector3 openLocalPositionOffset = new Vector3(20f, 0f, 0f); 
    [SerializeField] private float transitionSpeed = 12f;

    [Header("Puzzle Settings")]
    [SerializeField] private float timeLimit = 10f; 
    
    // We now cache the Vector3 Position instead of the Quaternion Rotation
    private Vector3 closedLocalPosition; 
    private float countdownTimer;
    private bool puzzleActive = false;
    private bool isHacked = false;

    private void Awake()
    {
        if (doorMesh != null)
        {
            // Remember exactly where the door is when it's closed
            closedLocalPosition = doorMesh.localPosition;
        }
    }

    private void OnEnable()
    {
        ResetFirewallState();
    }

    private void Update()
    {
        if (!puzzleActive || isHacked) return;

        countdownTimer -= Time.unscaledDeltaTime;
        GameManager.Instance.UpdateTimerText("Timeout in\n", countdownTimer);
        
        if (countdownTimer <= 0)
        {
            TriggerFailure();
        }
    }

    public void StartDecryptionPuzzle()
    {
        if (isHacked) return;

        countdownTimer = timeLimit;
        puzzleActive = true;
        
        GameManager.Instance.TriggerFirewallEncounter();
    }

    public void OnPuzzleSolved()
    {
        if (!puzzleActive || isHacked) return;

        isHacked = true;
        puzzleActive = false;

        Time.timeScale = 1f;
        GameManager.Instance.ChangeState(GameManager.GameState.Playing);

        StartCoroutine(OpenDoorAnimation());
    }

    private void TriggerFailure()
    {
        puzzleActive = false;
        Time.timeScale = 1f; 
        GameManager.Instance.TriggerGameOver();
    }

    private IEnumerator OpenDoorAnimation()
    {
        // Calculate the final open position relative to its starting point
        Vector3 targetPos = closedLocalPosition + openLocalPositionOffset;

        // Slide the door until it's practically at the target position
        while (Vector3.Distance(doorMesh.localPosition, targetPos) > 0.01f)
        {
            doorMesh.localPosition = Vector3.Lerp(
                doorMesh.localPosition, 
                targetPos, 
                transitionSpeed * Time.unscaledDeltaTime
            );
            yield return null;
        }

        // Snap perfectly into place at the end
        doorMesh.localPosition = targetPos;
    }

    private void ResetFirewallState()
    {
        StopAllCoroutines();
        puzzleActive = false;
        isHacked = false;
        countdownTimer = timeLimit;

        if (doorMesh != null)
        {
            // Reset the physical position back to closed when the track recycles
            doorMesh.localPosition = closedLocalPosition;
        }
    }
}