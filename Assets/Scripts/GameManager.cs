using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, FirewallHacking, Paused, GameOver, Tutorial }
    public GameState currentState;

    [Header("UI Canvases")]
    public GameObject mainMenuCanvas;
    public GameObject hudCanvas;
    public GameObject hackingCanvas; 
    public GameObject pauseCanvas; 
    public GameObject gameOverCanvas;
    public GameObject tutorialCanvas;
    
    [Header("UI Text References")]
    public TextMeshProUGUI hudGravityText; // Drag your Gravity Timer TMP here

    public TextMeshProUGUI hudTimerText;
    public TextMeshProUGUI gameOverScoreText; // Attach your Game Over TMP text here
    public TextMeshProUGUI hudScoreText;      // Optional: Attach a HUD TMP text to see score while playing
    public TextMeshProUGUI gameOverFirewallText;

    [Header("Score Settings")]
    public float scoreMultiplier = 10f; // Increase this to make the score tick up faster
    private float currentScore = 0f;
    private int firewallsHacked = 0;

    [Header("Save Data")]
    public int highScore = 0;
    public int highestFirewallsHacked = 0;

    [Header("Audio Sources & Clips")]
    public AudioSource sfxSource;       // Drag your AudioSource component here
    public AudioClip buttonClickClip;
    public AudioClip gravityFlipClip;
    public AudioClip explosionClip;
    public AudioClip puzzleWinClip;
    [Header("BGM & Audio Effects")]
    public AudioSource bgmSource; // The AudioSource playing your music
    public AudioLowPassFilter bgmLowPassFilter; // The component that muffles the sound
    
    // 22000 is maximum frequency (normal hearing). 800-1000 makes it sound underwater/muffled.
    public float normalCutoff = 22000f; 
    public float muffledCutoff = 800f;

    [Header("Game References")]
    public PlayerMovement playerMovement;
    public TrackManager trackManager;

    private GameState stateBeforePause; 

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        Application.targetFrameRate = 120; 
        // LOAD SAVED DATA HERE
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        highestFirewallsHacked = PlayerPrefs.GetInt("BestFirewalls", 0);
        ChangeState(GameState.MainMenu);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
        // --- SCORE LOGIC ---
        // Only increase score while the game is active (or in slow-mo hacking phase)
        if (currentState == GameState.Playing || currentState == GameState.FirewallHacking)
        {
            // Time.deltaTime automatically respects Time.timeScale, so it slows down during hacking!
            currentScore += Time.deltaTime * scoreMultiplier;
            
            if (hudScoreText != null)
            {
                // Formats the score to be a nice round integer on the HUD (e.g., 00150)
                hudScoreText.text = Mathf.FloorToInt(currentScore).ToString("D5"); 
            }
        }
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;

        if (currentState != GameState.Paused)
        {
            mainMenuCanvas.SetActive(false);
            hudCanvas.SetActive(false);
            hackingCanvas.SetActive(false);
            pauseCanvas.SetActive(false);
            gameOverCanvas.SetActive(false);
        }

        switch (currentState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f; 
                mainMenuCanvas.SetActive(true);
                tutorialCanvas.SetActive(false);
                playerMovement.enabled = false; 
                if (trackManager != null) trackManager.enabled = true;
                
                break;

            case GameState.Tutorial:
                Time.timeScale = 1f;
                tutorialCanvas.SetActive(true);
                mainMenuCanvas.SetActive(false);
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                hudCanvas.SetActive(true);
                playerMovement.enabled = true;
                if (playerMovement != null) playerMovement.ResumeFromHacking();
                if (trackManager != null) trackManager.enabled = true;
                // THE NEW AUDIO FIX: Restore the music to full quality
                if (bgmLowPassFilter != null) bgmLowPassFilter.cutoffFrequency = normalCutoff;
                break;

            case GameState.FirewallHacking:
                // THE FIX: Explicitly pull the slow-mo time scale from TrackManager to prevent permanent freezing after unpausing
                if (trackManager != null) Time.timeScale = trackManager.slowMotionTimeScale; 
                hackingCanvas.SetActive(true);
                hudCanvas.SetActive(true);
                if (playerMovement != null) playerMovement.PrepareForHacking();
                playerMovement.enabled = false;
                // THE NEW AUDIO FIX: Muffle the music during the hack!
                if (bgmLowPassFilter != null) bgmLowPassFilter.cutoffFrequency = muffledCutoff;
                break;

            case GameState.Paused:
                Time.timeScale = 0f; 
                hackingCanvas.SetActive(false);
                hudCanvas.SetActive(false); 
                pauseCanvas.SetActive(true);
                if (bgmLowPassFilter != null) bgmLowPassFilter.cutoffFrequency = muffledCutoff;
                break;

            case GameState.GameOver:
                Time.timeScale = 1f;
                gameOverCanvas.SetActive(true);
                playerMovement.enabled = false;
                if (trackManager != null) trackManager.enabled = false;
                // THE NEW AUDIO FIX: Muffle the music when you crash!
                if (bgmLowPassFilter != null) bgmLowPassFilter.cutoffFrequency = muffledCutoff;
                // --- NEW SAVE LOGIC ---
                int finalScoreInt = Mathf.FloorToInt(currentScore);
                bool isNewHighScore = false;

                // Check if we beat the high score
                if (finalScoreInt > highScore)
                {
                    highScore = finalScoreInt;
                    PlayerPrefs.SetInt("HighScore", highScore);
                    isNewHighScore = true; // You can use this boolean to trigger a "NEW HIGH SCORE!" visual effect later!
                }

                // Check if we beat the firewall record
                if (firewallsHacked > highestFirewallsHacked)
                {
                    highestFirewallsHacked = firewallsHacked;
                    PlayerPrefs.SetInt("BestFirewalls", highestFirewallsHacked);
                }

                // Save it to the device's physical storage
                PlayerPrefs.Save(); 

                // --- GAME OVER SCORE DISPLAY ---
                if (gameOverScoreText != null)
                {
                    gameOverScoreText.text = "FINAL SCORE: " + finalScoreInt.ToString() + 
                                             "\nHIGH SCORE: " + highScore.ToString();
                }
                
                if (gameOverFirewallText != null)
                {
                    gameOverFirewallText.text = "FIREWALLS BYPASSED: " + firewallsHacked.ToString() + 
                                                "\nBEST RUN: " + highestFirewallsHacked.ToString();
                }
                break;
        }
    }
    // Public method so our puzzles can award burst points!
    public void AddScore(float bonusAmount)
    {
        currentScore += bonusAmount;
    }
    public void TogglePause()
    {
        if (currentState == GameState.MainMenu || currentState == GameState.GameOver) return;

        if (currentState == GameState.Paused)
        {
            ChangeState(stateBeforePause);
        }
        else
        {
            stateBeforePause = currentState;
            ChangeState(GameState.Paused);
        }
    }

    public void StartInfiltration()
    {
        ChangeState(GameState.Playing);
    }
    public void TriggerTutorial()
    {
        ChangeState(GameState.Tutorial);
    }
    public void ExitTutorial()
    {
        ChangeState(GameState.MainMenu);
    }

    public void TriggerFirewallEncounter()
    {
        ChangeState(GameState.FirewallHacking);
    }

    public void TriggerGameOver()
    {
        ChangeState(GameState.GameOver);
    }
    
    public void RestartGame()
    {
        currentScore = 0f; // Reset score on restart
        firewallsHacked = 0;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    public void RegisterFirewallHacked()
    {
        firewallsHacked++;
    }
    public void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    public void UpdateTimerText(string message, float timeRemaining)
    {
        if (hudTimerText != null)
        {
            // "F2" formats the number to exactly 2 decimal places (e.g., 12.34 s)
            hudTimerText.text = message + timeRemaining.ToString("F2") + " s";
        }
    }
    public void UpdateGravityTimerText(float cooldownRemaining)
    {
        if (hudGravityText != null)
        {
            if (cooldownRemaining > 0)
            {
                // Shows exactly 2 decimal places while counting down
                hudGravityText.text = cooldownRemaining.ToString("F2") + "s" + "\nGravity Flip Cooldown";
            }
            else
            {
                hudGravityText.text = "Gravity Flip Ready";
            }
        }
    }
    public void ExitGame()
    {
        Application.Quit();
    }
}