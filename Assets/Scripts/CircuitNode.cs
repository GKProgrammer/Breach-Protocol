using UnityEngine;
using UnityEngine.UI;

public class CircuitNode : MonoBehaviour
{
    [Header("Visuals")]
    public Image nodeImage;
    public Sprite powerlessSprite;
    public Sprite poweredSprite;

    [Header("Base Connections (Check boxes in Inspector)")]
    public bool connectsTop;    // 0
    public bool connectsRight;  // 1
    public bool connectsBottom; // 2
    public bool connectsLeft;   // 3

    public bool[] activeConnections = new bool[4]; 
    public bool isPowered = false;
    
    public int gridX, gridY;
    private CircuitManager manager;

    public void Initialize(CircuitManager mng, int x, int y)
    {
        manager = mng;
        gridX = x;
        gridY = y;

        activeConnections[0] = connectsTop;
        activeConnections[1] = connectsRight;
        activeConnections[2] = connectsBottom;
        activeConnections[3] = connectsLeft;

        GetComponent<Button>().onClick.AddListener(RotateNode);
    }

    // Called by the Player's Button Click
    public void RotateNode()
    {
        PerformSilentRotation();
        
        // Only ask the manager to check the power if the player clicked it
        if (manager != null)
        {
            manager.EvaluatePowerFlow();
        }
    }

    // Called by the Manager during generation to scramble without crashing
    public void PerformSilentRotation()
    {
        transform.Rotate(0, 0, -90f);

        bool temp = activeConnections[3];
        activeConnections[3] = activeConnections[2];
        activeConnections[2] = activeConnections[1];
        activeConnections[1] = activeConnections[0];
        activeConnections[0] = temp;
    }

    public void SetPowerState(bool state)
    {
        // Safety check to prevent NREs if an image reference is missing
        if (nodeImage == null) return;

        isPowered = state;
        nodeImage.sprite = isPowered ? poweredSprite : powerlessSprite;
    }
}