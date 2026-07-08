using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Modern MOBA minimap with team colors and unit positioning
/// </summary>
public class MinimapRenderer : MonoBehaviour
{
    [SerializeField] private RenderTexture minimapTexture;
    [SerializeField] private float mapScale = 0.01f;
    [SerializeField] private Color blueTeamColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color redTeamColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color visionColor = new Color(0.4f, 1f, 0.4f);
    
    private Camera minimapCamera;
    private Dictionary<Champion, Renderer> championMarkers = new();
    
    void Start()
    {
        SetupMinimapCamera();
    }
    
    private void SetupMinimapCamera()
    {
        GameObject minimapCamObj = new GameObject("Minimap Camera");
        minimapCamera = minimapCamObj.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = 150f;
        minimapCamera.targetTexture = minimapTexture;
        minimapCamera.cullingMask = LayerMask.GetMask("Minimap");
        minimapCamObj.transform.position = new Vector3(0, 200, 0);
        minimapCamObj.transform.rotation = Quaternion.Euler(90, 0, 0);
    }
    
    public void RegisterChampion(Champion champion)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.localScale = Vector3.one * 2f;
        marker.GetComponent<Renderer>().material.color = champion.Team == TeamType.Blue ? blueTeamColor : redTeamColor;
        marker.layer = LayerMask.NameToLayer("Minimap");
        
        championMarkers[champion] = marker.GetComponent<Renderer>();
    }
    
    void Update()
    {
        foreach (var kvp in championMarkers)
        {
            Champion champ = kvp.Key;
            Renderer renderer = kvp.Value;
            
            if (champ == null)
                continue;
            
            Vector3 pos = champ.transform.position;
            renderer.transform.position = pos;
        }
    }
}
