using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGTerrainLayerSetup : MonoBehaviour
{
    [ContextMenu("Create And Assign AOG Terrain Layers")]
    public void CreateAndAssignLayers()
    {
        Terrain terrain = FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("Sahnede Terrain bulunamadı.");
            return;
        }

        TerrainLayer darkGrass = CreateLayer("AOG_Terrain_Dark_Grass", new Color32(12, 38, 18, 255));
        TerrainLayer dirt = CreateLayer("AOG_Terrain_Dark_Dirt", new Color32(48, 38, 28, 255));
        TerrainLayer stone = CreateLayer("AOG_Terrain_Ancient_Stone", new Color32(92, 78, 58, 255));
        TerrainLayer riverBank = CreateLayer("AOG_Terrain_River_Bank", new Color32(28, 32, 34, 255));

        terrain.terrainData.terrainLayers = new TerrainLayer[]
        {
            darkGrass,
            dirt,
            stone,
            riverBank
        };

        Debug.Log("AOG terrain layerları oluşturuldu ve Terrain'e atandı.");
    }

    TerrainLayer CreateLayer(string layerName, Color color)
    {
        string folderPath = "Assets/AOG_Terrain_Textures";

#if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "AOG_Terrain_Textures");
        }
#endif

        Texture2D tex = new Texture2D(64, 64);
        tex.name = layerName + "_Texture";

        Color[] pixels = new Color[64 * 64];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        tex.SetPixels(pixels);
        tex.Apply();

#if UNITY_EDITOR
        string texPath = folderPath + "/" + tex.name + ".asset";
        AssetDatabase.CreateAsset(tex, texPath);
#endif

        TerrainLayer layer = new TerrainLayer();
        layer.name = layerName;
        layer.diffuseTexture = tex;
        layer.tileSize = new Vector2(18, 18);

#if UNITY_EDITOR
        string layerPath = folderPath + "/" + layerName + ".terrainlayer";
        AssetDatabase.CreateAsset(layer, layerPath);
        AssetDatabase.SaveAssets();
#endif

        return layer;
    }
}