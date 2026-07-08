using UnityEngine;

/// <summary>
Debug visualization for MOBA systems - shows game state in real-time
/// </summary>
public class MOBADebugVisualizer : MonoBehaviour
{
    [SerializeField] private bool enableDebugDraw = true;
    [SerializeField] private bool showChampions = true;
    [SerializeField] private bool showMinions = true;
    [SerializeField] private bool showTowers = true;
    [SerializeField] private bool showAbilityRanges = true;
    [SerializeField] private Color blueColor = Color.blue;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color neutralColor = Color.gray;
    
    private GameStateManager gameState;
    private Camera mainCamera;
    
    void Start()
    {
        gameState = FindObjectOfType<GameStateManager>();
        mainCamera = Camera.main;
    }
    
    void OnDrawGizmos()
    {
        if (!enableDebugDraw) return;
        
        if (showChampions)
            DrawChampions();
        
        if (showMinions)
            DrawMinions();
        
        if (showTowers)
            DrawTowers();
        
        if (showAbilityRanges)
            DrawAbilityRanges();
    }
    
    void Update()
    {
        if (enableDebugDraw)
            DebugDrawStats();
    }
    
    private void DrawChampions()
    {
        Champion[] champions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        
        foreach (Champion champ in champions)
        {
            Color color = champ.Team == TeamType.Blue ? blueColor : redColor;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(champ.transform.position, 1.5f);
        }
    }
    
    private void DrawMinions()
    {
        CombatUnit[] units = FindObjectsByType<CombatUnit>(FindObjectsSortMode.None);
        
        foreach (CombatUnit unit in units)
        {
            if (unit.UnitType == UnitType.Minion)
            {
                Color color = unit.UnitTeam == TeamType.Blue ? blueColor : redColor;
                Gizmos.color = color;
                Gizmos.DrawWireCube(unit.transform.position, Vector3.one * 0.5f);
            }
        }
    }
    
    private void DrawTowers()
    {
        CombatUnit[] units = FindObjectsByType<CombatUnit>(FindObjectsSortMode.None);
        
        foreach (CombatUnit unit in units)
        {
            if (unit.UnitType == UnitType.Tower)
            {
                Color color = unit.UnitTeam == TeamType.Blue ? blueColor : redColor;
                Gizmos.color = color;
                Gizmos.DrawWireCube(unit.transform.position, Vector3.one * 2f);
                
                // Draw tower attack range
                Gizmos.color = new Color(color.r, color.g, color.b, 0.2f);
                DrawWireCircle(unit.transform.position, 12f, 20);
            }
        }
    }
    
    private void DrawAbilityRanges()
    {
        Champion[] champions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        
        foreach (Champion champ in champions)
        {
            // Draw ability range (estimated 10 units)
            Color color = champ.Team == TeamType.Blue ? blueColor : redColor;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.1f);
            DrawWireCircle(champ.transform.position, 10f, 30);
        }
    }
    
    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 lastPoint = center + Vector3.right * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }
    
    private void DebugDrawStats()
    {
        if (gameState == null) return;
        
        // Draw game timer
        float gameTime = gameState.GetGameTime();
        int minutes = (int)(gameTime / 60f);
        int seconds = (int)(gameTime % 60f);
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box($"Game Time: {minutes:D2}:{seconds:D2}");
        
        TeamStats blueStats = gameState.GetTeamStats(TeamType.Blue);
        TeamStats redStats = gameState.GetTeamStats(TeamType.Red);
        
        if (blueStats != null)
            GUILayout.Label($"Blue - Kills: {blueStats.Kills}, Deaths: {blueStats.Deaths}");
        
        if (redStats != null)
            GUILayout.Label($"Red - Kills: {redStats.Kills}, Deaths: {redStats.Deaths}");
        
        GUILayout.EndArea();
    }
}
