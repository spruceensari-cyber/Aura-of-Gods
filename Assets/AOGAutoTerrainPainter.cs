using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGAutoTerrainPainter : MonoBehaviour
{
    [Header("Auto Terrain Painting")]
    public Terrain targetTerrain;

    [Header("Paint Widths")]
    public float roadPaintWidth = 9f;
    public float roadEdgeWidth = 17f;
    public float riverPaintWidth = 18f;
    public float basePaintRadius = 28f;

    [Header("Layer Indexes")]
    public int grassLayer = 0;
    public int dirtLayer = 1;
    public int stoneLayer = 2;
    public int riverBankLayer = 3;

    [ContextMenu("Auto Paint Aura Of Gods Terrain")]
    public void AutoPaintTerrain()
    {
        if (targetTerrain == null)
            targetTerrain = FindFirstObjectByType<Terrain>();

        if (targetTerrain == null)
        {
            Debug.LogError("Terrain bulunamadı.");
            return;
        }

        TerrainData data = targetTerrain.terrainData;

        if (data.terrainLayers == null || data.terrainLayers.Length < 4)
        {
            Debug.LogError("Terrain üzerinde en az 4 layer olmalı: Grass, Dirt, Stone, River Bank.");
            return;
        }

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        int layers = data.alphamapLayers;

        float[,,] map = new float[width, height, layers];

        // 1. Her yeri dark grass yap
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                SetLayer(map, x, z, grassLayer, 1f);
            }
        }

        // 2. Dirt geçişleri: yolların biraz geniş alanı
        PaintPath(map, data, GetMidLane(), roadEdgeWidth, dirtLayer, 0.55f);
        PaintPath(map, data, GetTopLane(), roadEdgeWidth, dirtLayer, 0.55f);
        PaintPath(map, data, GetBotLane(), roadEdgeWidth, dirtLayer, 0.55f);

        // 3. Stone ana yollar
        PaintPath(map, data, GetMidLane(), roadPaintWidth, stoneLayer, 1f);
        PaintPath(map, data, GetTopLane(), roadPaintWidth, stoneLayer, 1f);
        PaintPath(map, data, GetBotLane(), roadPaintWidth, stoneLayer, 1f);

        // 4. River bank alanları
        PaintPath(map, data, GetUpperRiver(), riverPaintWidth, riverBankLayer, 0.9f);
        PaintPath(map, data, GetLowerRiver(), riverPaintWidth, riverBankLayer, 0.9f);

        // 5. Base alanları stone + dirt
        PaintCircle(map, data, new Vector3(-105, 0, -78), basePaintRadius + 8f, dirtLayer, 0.45f);
        PaintCircle(map, data, new Vector3(105, 0, 78), basePaintRadius + 8f, dirtLayer, 0.45f);

        PaintCircle(map, data, new Vector3(-105, 0, -78), basePaintRadius, stoneLayer, 1f);
        PaintCircle(map, data, new Vector3(105, 0, 78), basePaintRadius, stoneLayer, 1f);

        // 6. Jungle camp çevrelerine dirt
        foreach (Vector3 camp in GetBlueCamps())
        {
            PaintCircle(map, data, camp, 16f, dirtLayer, 0.55f);
            PaintCircle(map, data, Mirror(camp), 16f, dirtLayer, 0.55f);
        }

        // 7. Objective çevreleri
        PaintCircle(map, data, new Vector3(-38, 0, 36), 22f, riverBankLayer, 0.75f);
        PaintCircle(map, data, new Vector3(38, 0, -36), 22f, riverBankLayer, 0.75f);

        NormalizeMap(map, width, height, layers);

        data.SetAlphamaps(0, 0, map);

#if UNITY_EDITOR
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
#endif

        Debug.Log("Aura of Gods terrain otomatik boyandı.");
    }

    Vector3[] GetMidLane()
    {
        return new Vector3[]
        {
            new Vector3(-105, 0, -78),
            new Vector3(-60, 0, -44),
            new Vector3(-25, 0, -18),
            new Vector3(0, 0, 0),
            new Vector3(25, 0, 18),
            new Vector3(60, 0, 44),
            new Vector3(105, 0, 78)
        };
    }

    Vector3[] GetTopLane()
    {
        return new Vector3[]
        {
            new Vector3(-105, 0, -78),
            new Vector3(-118, 0, -52),
            new Vector3(-112, 0, 10),
            new Vector3(-92, 0, 50),
            new Vector3(-48, 0, 88),
            new Vector3(10, 0, 96),
            new Vector3(58, 0, 108),
            new Vector3(105, 0, 78)
        };
    }

    Vector3[] GetBotLane()
    {
        return new Vector3[]
        {
            new Vector3(-105, 0, -78),
            new Vector3(-58, 0, -108),
            new Vector3(-10, 0, -96),
            new Vector3(48, 0, -88),
            new Vector3(92, 0, -50),
            new Vector3(112, 0, 10),
            new Vector3(118, 0, 52),
            new Vector3(105, 0, 78)
        };
    }

    Vector3[] GetUpperRiver()
    {
        return new Vector3[]
        {
            new Vector3(-78, 0, 24),
            new Vector3(-55, 0, 42),
            new Vector3(-25, 0, 39),
            new Vector3(0, 0, 24),
            new Vector3(25, 0, 20)
        };
    }

    Vector3[] GetLowerRiver()
    {
        return new Vector3[]
        {
            new Vector3(-25, 0, -20),
            new Vector3(0, 0, -24),
            new Vector3(25, 0, -39),
            new Vector3(55, 0, -42),
            new Vector3(78, 0, -24)
        };
    }

    Vector3[] GetBlueCamps()
    {
        return new Vector3[]
        {
            new Vector3(-78, 0, 34),
            new Vector3(-70, 0, -14),
            new Vector3(-42, 0, -66),
            new Vector3(-12, 0, 58),
            new Vector3(-20, 0, -22)
        };
    }

    void PaintPath(float[,,] map, TerrainData data, Vector3[] points, float width, int layer, float strength)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            PaintSegment(map, data, points[i], points[i + 1], width, layer, strength);
        }
    }

    void PaintSegment(float[,,] map, TerrainData data, Vector3 a, Vector3 b, float width, int layer, float strength)
    {
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;

        for (int x = 0; x < alphaWidth; x++)
        {
            for (int z = 0; z < alphaHeight; z++)
            {
                Vector3 world = AlphaToWorld(data, x, z);
                float distance = DistancePointToSegment2D(world, a, b);

                if (distance <= width)
                {
                    float falloff = 1f - Mathf.Clamp01(distance / width);
                    float finalStrength = strength * Mathf.SmoothStep(0f, 1f, falloff);
                    BlendLayer(map, x, z, layer, finalStrength);
                }
            }
        }
    }

    void PaintCircle(float[,,] map, TerrainData data, Vector3 center, float radius, int layer, float strength)
    {
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;

        for (int x = 0; x < alphaWidth; x++)
        {
            for (int z = 0; z < alphaHeight; z++)
            {
                Vector3 world = AlphaToWorld(data, x, z);
                float distance = Vector2.Distance(new Vector2(world.x, world.z), new Vector2(center.x, center.z));

                if (distance <= radius)
                {
                    float falloff = 1f - Mathf.Clamp01(distance / radius);
                    float finalStrength = strength * Mathf.SmoothStep(0f, 1f, falloff);
                    BlendLayer(map, x, z, layer, finalStrength);
                }
            }
        }
    }

    Vector3 AlphaToWorld(TerrainData data, int x, int z)
    {
        Vector3 terrainPos = targetTerrain.transform.position;

        float worldX = terrainPos.x + ((float)x / data.alphamapWidth) * data.size.x;
        float worldZ = terrainPos.z + ((float)z / data.alphamapHeight) * data.size.z;

        return new Vector3(worldX, 0, worldZ);
    }

    float DistancePointToSegment2D(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 va = new Vector2(a.x, a.z);
        Vector2 vb = new Vector2(b.x, b.z);

        Vector2 ab = vb - va;
        float t = Vector2.Dot(p - va, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);

        Vector2 closest = va + t * ab;
        return Vector2.Distance(p, closest);
    }

    void SetLayer(float[,,] map, int x, int z, int layer, float value)
    {
        int layers = map.GetLength(2);

        for (int i = 0; i < layers; i++)
            map[x, z, i] = 0f;

        if (layer >= 0 && layer < layers)
            map[x, z, layer] = value;
    }

    void BlendLayer(float[,,] map, int x, int z, int layer, float strength)
    {
        int layers = map.GetLength(2);

        if (layer < 0 || layer >= layers)
            return;

        strength = Mathf.Clamp01(strength);

        for (int i = 0; i < layers; i++)
        {
            if (i == layer)
                map[x, z, i] = Mathf.Max(map[x, z, i], strength);
            else
                map[x, z, i] *= (1f - strength);
        }
    }

    void NormalizeMap(float[,,] map, int width, int height, int layers)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float total = 0f;

                for (int l = 0; l < layers; l++)
                    total += map[x, z, l];

                if (total <= 0f)
                {
                    map[x, z, grassLayer] = 1f;
                    continue;
                }

                for (int l = 0; l < layers; l++)
                    map[x, z, l] /= total;
            }
        }
    }

    Vector3 Mirror(Vector3 p)
    {
        return new Vector3(-p.x, p.y, -p.z);
    }
}