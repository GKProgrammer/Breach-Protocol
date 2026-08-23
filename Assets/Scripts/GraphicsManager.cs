using UnityEngine;
using TMPro;

public class GraphicsManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown qualityDropdown;

    private void Start()
    {
        // Load saved quality (Defaulting to 2, which is 'High', if no save exists)
        int savedQuality = PlayerPrefs.GetInt("GraphicsQuality", 2);
        
        // Apply the setting to the engine
        QualitySettings.SetQualityLevel(savedQuality);
        
        // Sync the Dropdown UI to show the correct saved option
        if (qualityDropdown != null)
        {
            qualityDropdown.value = savedQuality;
            qualityDropdown.RefreshShownValue();
        }
    }

    // This method will be called by the Dropdown when the player makes a choice
    public void ChangeQualityLevel(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("GraphicsQuality", qualityIndex);
        PlayerPrefs.Save();
        
        Debug.Log("Graphics set to level: " + qualityIndex);
    }
}