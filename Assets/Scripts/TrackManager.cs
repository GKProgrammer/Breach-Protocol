using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    private int lastTrackIndex = -1;
    public static TrackManager Instance { get; private set; }

    [Header("Track Prefabs (Assign in Inspector)")]
    public GameObject menuTrackPrefab; 
    public GameObject firewallTrackPrefab; 
    public GameObject[] regularTrackPrefabs; 

    [Header("Track Settings")]
    public int initialSectionsCount = 6; 
    public float sectionLength = 30f; 
    
    [Header("Movement Settings")]
    public float currentSpeed = 15f; 

    [Header("Firewall Settings")]
    public float timeBetweenFirewalls = 60f; 
    public float firewallEncounterDistance = 20f; 
    public float slowMotionTimeScale = 0.1f; 

    private Queue<GameObject> menuPool = new Queue<GameObject>();
    private Queue<GameObject> firewallPool = new Queue<GameObject>();
    
    // THE FIX 1: Array of queues so each prefab variation gets its own dedicated pool!
    private Queue<GameObject>[] regularPools; 
    
    private List<GameObject> activeSections = new List<GameObject>();

    private float playTimer = 0f;
    private bool isFirewallQueued = false;
    private GameObject activeFirewall = null;
    private FirewallPuzzleController activePuzzle = null; 

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        InitializePools();
        for (int i = 0; i < initialSectionsCount; i++)
        {
            SpawnSection(i * sectionLength);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.currentState == GameManager.GameState.Playing)
        {
            if (activeFirewall == null && !isFirewallQueued)
            {
                // Counting down to spawn
                float timeRemaining = Mathf.Max(0, timeBetweenFirewalls - playTimer);
                GameManager.Instance.UpdateTimerText("Approaching Firewall in\n", timeRemaining);
            }
            else if (activeFirewall != null)
            {
                // The wall has spawned! Calculate exact time until impact based on distance and speed
                float distance = Mathf.Max(0, activeFirewall.transform.position.z - firewallEncounterDistance);
                float timeToHit = distance / currentSpeed;
                GameManager.Instance.UpdateTimerText("Firewall Intercept in\n", timeToHit);
            }
        }

        MoveTrack();
        CheckForRecycle();
        HandleFirewallTiming();
    }

    private void InitializePools()
    {
        for (int i = 0; i < initialSectionsCount + 2; i++)
        {
            GameObject obj = Instantiate(menuTrackPrefab, transform);
            obj.SetActive(false);
            menuPool.Enqueue(obj);
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject obj = Instantiate(firewallTrackPrefab, transform);
            obj.SetActive(false);
            firewallPool.Enqueue(obj);
        }

        // THE FIX 1 (cont): Setup the separate queues for each regular track variation
        regularPools = new Queue<GameObject>[regularTrackPrefabs.Length];
        for (int i = 0; i < regularTrackPrefabs.Length; i++)
        {
            regularPools[i] = new Queue<GameObject>();
            for (int j = 0; j < 3; j++) 
            {
                GameObject obj = Instantiate(regularTrackPrefabs[i], transform);
                obj.SetActive(false);
                regularPools[i].Enqueue(obj);
            }
        }
    }

    private void HandleFirewallTiming()
    {
        if (GameManager.Instance.currentState == GameManager.GameState.Playing)
        {
            playTimer += Time.deltaTime;

            if (playTimer >= timeBetweenFirewalls && !isFirewallQueued)
            {
                isFirewallQueued = true;
            }

            if (activeFirewall != null && activeFirewall.transform.position.z <= firewallEncounterDistance)
            {
                TriggerFirewallEncounter();
            }
        }
    }

    // This is the function where tracks actually get spawned!
    private void SpawnSection(float zPosition)
    {
        GameObject sectionToSpawn = null;

        // Note: I left GameState.Tutorial here in case you added it to your GameState enum!
        if (GameManager.Instance.currentState == GameManager.GameState.MainMenu || GameManager.Instance.currentState.ToString() == "Tutorial")
        {
            sectionToSpawn = GetFromPool(menuPool, menuTrackPrefab);
        }
        else if (isFirewallQueued && activeFirewall == null)
        {
            sectionToSpawn = GetFromPool(firewallPool, firewallTrackPrefab);
            activeFirewall = sectionToSpawn; 

            activePuzzle = activeFirewall.GetComponentInChildren<FirewallPuzzleController>();
            
            isFirewallQueued = false;
        }
        else
        {
            // THE FIX 3: Non-repeating random math
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, regularTrackPrefabs.Length);
            } 
            while (randomIndex == lastTrackIndex && regularTrackPrefabs.Length > 1);

            lastTrackIndex = randomIndex; // Remember the choice for next time
            
            // Pull from the specific queue that matches the random index!
            sectionToSpawn = GetFromPool(regularPools[randomIndex], regularTrackPrefabs[randomIndex]);
        }

        sectionToSpawn.transform.position = new Vector3(0, 0, zPosition);
        sectionToSpawn.SetActive(true);
        activeSections.Add(sectionToSpawn);
    }

    private GameObject GetFromPool(Queue<GameObject> pool, GameObject fallbackPrefab)
    {
        if (pool.Count == 0)
        {
            GameObject obj = Instantiate(fallbackPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        return pool.Dequeue();
    }

    private void MoveTrack()
    {
        for (int i = 0; i < activeSections.Count; i++)
        {
            activeSections[i].transform.Translate(Vector3.back * (currentSpeed * Time.deltaTime));
        }
    }

    private void CheckForRecycle()
    {
        if (activeSections[0].transform.position.z < -sectionLength)
        {
            GameObject oldSection = activeSections[0];
            activeSections.RemoveAt(0);
            oldSection.SetActive(false);
            
            // THE FIX 4: Route the old section back into the CORRECT separate pool
            if (oldSection.name.Contains(menuTrackPrefab.name)) 
            {
                menuPool.Enqueue(oldSection);
            }
            else if (oldSection.name.Contains(firewallTrackPrefab.name)) 
            {
                firewallPool.Enqueue(oldSection);
            }
            else 
            {
                // Loop through our array to figure out which pool this track belongs in
                for (int i = 0; i < regularTrackPrefabs.Length; i++)
                {
                    if (oldSection.name.Contains(regularTrackPrefabs[i].name))
                    {
                        regularPools[i].Enqueue(oldSection);
                        break;
                    }
                }
            }

            float nextZPosition = activeSections[activeSections.Count - 1].transform.position.z + sectionLength;
            SpawnSection(nextZPosition);
        }
    }

    private void TriggerFirewallEncounter()
    {
        Time.timeScale = slowMotionTimeScale;

        if (activePuzzle != null)
        {
            activePuzzle.StartDecryptionPuzzle();
        }

        playTimer = 0f;
        activeFirewall = null; 
    }

    public void ResolveActiveFirewall()
    {
        if (activePuzzle != null)
        {
            activePuzzle.OnPuzzleSolved();
        }
    }
}