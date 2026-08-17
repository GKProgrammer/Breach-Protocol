using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CircuitManager : MonoBehaviour
{
    [Header("Grid Layout")]
    public Transform gridParent; // Assign a UI panel with a GridLayoutGroup component
    public GridLayoutGroup gridLayout;

    [Header("Tile Prefabs")]
    public GameObject startPrefab;
    public GameObject finishPrefab;
    public GameObject[] wirePrefabs; // Array containing your I, L, and T prefabs

    [Header("Thematic Targets")]
    public TextMeshProUGUI targetPortText; // Optional UI text to display "TARGET: PORT 80"
    private int[] targetPorts = { 80, 21, 443, 22, 8080 }; // HTTP, FTP, HTTPS, SSH, Alt-HTTP

    private CircuitNode[,] grid;
    private int currentSize;
    private CircuitNode startNode;
    private CircuitNode finishNode;

    private void OnEnable()
    {
        // Generates a new puzzle every time the Firewall Hacking phase begins
        GenerateRandomPuzzle();
    }

    public void GenerateRandomPuzzle()
    {
        // 1. Pick a random difficulty (3x3, 4x4, or 5x5)
        int[] sizes = { 3, 4, 5 };
        currentSize = sizes[Random.Range(0, sizes.Length)];
        
        // Adjust UI Grid cell size dynamically based on the grid dimensions
        float panelWidth = gridParent.GetComponent<RectTransform>().rect.width;
        float cellSize = (panelWidth / currentSize) - gridLayout.spacing.x;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.constraintCount = currentSize;

        // 2. Clear old grid
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        grid = new CircuitNode[currentSize, currentSize];

        // 3. Populate the grid
        for (int y = 0; y < currentSize; y++)
        {
            for (int x = 0; x < currentSize; x++)
            {
                GameObject tileToSpawn;

                // Simple generation: Start is Top-Left, Finish is Bottom-Right
                if (x == 0 && y == 0) tileToSpawn = startPrefab;
                else if (x == currentSize - 1 && y == currentSize - 1) tileToSpawn = finishPrefab;
                else tileToSpawn = wirePrefabs[Random.Range(0, wirePrefabs.Length)];

                GameObject newTile = Instantiate(tileToSpawn, gridParent);
                CircuitNode node = newTile.GetComponent<CircuitNode>();
                node.Initialize(this, x, y);
                grid[x, y] = node;

                if (x == 0 && y == 0) startNode = node;
                if (x == currentSize - 1 && y == currentSize - 1) finishNode = node;

                // 4. The Scramble: Randomly rotate every tile 0 to 3 times
                int randomRotations = Random.Range(0, 4);
                for (int r = 0; r < randomRotations; r++)
                {
                    // THE FIX: Call the silent rotation instead of RotateNode()
                    node.PerformSilentRotation(); 
                }
            }
        }

        // Set thematic UI text
        if (targetPortText != null) 
            targetPortText.text = $"TARGET PORT: {targetPorts[Random.Range(0, targetPorts.Length)]}";

        EvaluatePowerFlow();
    }

    public void EvaluatePowerFlow()
    {
        // 1. Reset all nodes to unpowered
        foreach (CircuitNode node in grid) node.SetPowerState(false);

        // 2. Start a Flood-Fill from the Start Node
        Queue<CircuitNode> nodesToCheck = new Queue<CircuitNode>();
        nodesToCheck.Enqueue(startNode);
        startNode.SetPowerState(true);

        while (nodesToCheck.Count > 0)
        {
            CircuitNode current = nodesToCheck.Dequeue();

            // Check Top (0)
            if (current.activeConnections[0] && current.gridY > 0)
                CheckConnection(current, grid[current.gridX, current.gridY - 1], 0, 2, nodesToCheck);
            
            // Check Right (1)
            if (current.activeConnections[1] && current.gridX < currentSize - 1)
                CheckConnection(current, grid[current.gridX + 1, current.gridY], 1, 3, nodesToCheck);
            
            // Check Bottom (2)
            if (current.activeConnections[2] && current.gridY < currentSize - 1)
                CheckConnection(current, grid[current.gridX, current.gridY + 1], 2, 0, nodesToCheck);
            
            // Check Left (3)
            if (current.activeConnections[3] && current.gridX > 0)
                CheckConnection(current, grid[current.gridX - 1, current.gridY], 3, 1, nodesToCheck);
        }

        // 3. Win Condition Check
        if (finishNode.isPowered)
        {
            Debug.Log("Firewall Decrypted!");
            // Lock the puzzle from further clicks
            foreach (CircuitNode node in grid) node.GetComponent<Button>().interactable = false;
            
            float scoreBonus = currentSize * 100f;
            GameManager.Instance.AddScore(scoreBonus);
            
            // NEW: Tell the GameManager to increase the successful hack counter!
            GameManager.Instance.RegisterFirewallHacked();
            
            // Interface directly with the fixed TrackManager
            TrackManager.Instance.ResolveActiveFirewall();
        }
    }

    private void CheckConnection(CircuitNode current, CircuitNode neighbor, int dirFromCurrent, int dirFromNeighbor, Queue<CircuitNode> queue)
    {
        // If the current node has a wire pointing to the neighbor, AND the neighbor has a wire pointing back, AND the neighbor isn't already powered
        if (neighbor.activeConnections[dirFromNeighbor] && !neighbor.isPowered)
        {
            neighbor.SetPowerState(true);
            queue.Enqueue(neighbor);
        }
    }
}