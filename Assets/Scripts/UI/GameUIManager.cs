using UnityEngine;
using TMPro;

/// <summary>
In-game UI with game timer, team scores, objective tracker
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI blueScoreText;
    [SerializeField] private TextMeshProUGUI redScoreText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    
    private GameStateManager gameStateManager;
    
    void Start()
    {
        gameStateManager = FindObjectOfType<GameStateManager>();
    }
    
    void Update()
    {
        UpdateGameTimer();
        UpdateScores();
    }
    
    private void UpdateGameTimer()
    {
        if (gameStateManager == null) return;
        
        float gameTime = gameStateManager.GetGameTime();
        int minutes = (int)(gameTime / 60f);
        int seconds = (int)(gameTime % 60f);
        
        if (gameTimerText != null)
            gameTimerText.text = $"{minutes:D2}:{seconds:D2}";
    }
    
    private void UpdateScores()
    {
        if (gameStateManager == null) return;
        
        TeamStats blueStats = gameStateManager.GetTeamStats(TeamType.Blue);
        TeamStats redStats = gameStateManager.GetTeamStats(TeamType.Red);
        
        if (blueScoreText != null && blueStats != null)
            blueScoreText.text = $"BLUE: {blueStats.Kills}";
        
        if (redScoreText != null && redStats != null)
            redScoreText.text = $"RED: {redStats.Kills}";
    }
}
