using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGTerrainHeightShaper : MonoBehaviour
{
    public Terrain targetTerrain;

    [Header("Height Settings")]
    public float baseHeight = 0.03f;
    public float laneHeight = 0.045f;
    public float basePlatformHeight = 0.075f;
    public float jungleHeight = 0.055f;
    public float riverDepth = 0.005f;
    public float borderHeight = 0.12f;

    [ContextMenu("Shape Aura Of Gods Terrain Height")]
    public void ShapeTerrain()
    {
        if (targetTerrain == null)
            targetTerrain = FindFirstObjectByType<Terrain>();

        if (targetTerrain == null)
        {
            Debug.LogError("Terrain bulunamadı.");
            return;
        }

        TerrainData data = targetTerrain.terrainData;

        int width = data.heightmapResolution;
        int height = data.heightmapResolution;

        float[,] heights = new float[height, width];

        // Base terrain height
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 world = HeightToWorld(data, x, z);
                float h = baseHeight;

                // Outer border yükselsin
                float borderDist = DistanceToMapBorder(world);
                if (borderDist < 16f)
                {
                    float t = 1f - Mathf.Clamp01(borderDist / 16f);
                    h = Mathf.Lerp(h, borderHeight, t);
                }

                // Laneler hafif yükselsin
                h = Mathf.Max(h, PathInfluence(world, GetMidLane(), 10f, laneHeight));
                h = Mathf.Max(h, PathInfluence(world, GetTopLane(), 10f, laneHeight));
                h = Mathf.Max(h, PathInfluence(world, GetBotLane(), 10f, laneHeight));

                // Base platformları daha yüksek
                h = Mathf.Max(h, CircleInfluence(world, new Vector3(-105, 0, -78), 30f, basePlatformHeight));
                h = Mathf.Max(h, CircleInfluence(world, new Vector3(105, 0, 78), 30f, basePlatformHeight));

                // Jungle kampları hafif tümsek
                foreach (Vector3 camp in GetBlueCamps())
                {
                    h = Mathf.Max(h, CircleInfluence(world, camp, 15f, jungleHeight));
                    h = Mathf.Max(h, CircleInfluence(world, Mirror(camp), 15f, jungleHeight));
                }

                // River alanları alçalır
                float riverUpper = PathDistance(world, GetUpperRiver());
                float riverLower = PathDistance(world, GetLowerRiver());

                if (riverUpper < 16f || riverLower < 16f)
                {
                    float d = Mathf.Min(riverUpper, riverLower);
                    float t = 1f - Mathf.Clamp01(d / 16f);
                    h = Mathf.Lerp(h, riverDepth, t * 0.8f);
                }

                heights[z, x] = h;
            }
        }

        data.SetHeights(0, 0, heights);

#if UNITY_EDITOR
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
#endif

        Debug.Log("Aura of Gods terrain height şekillendirildi.");
    }

    Vector3 HeightToWorld(TerrainData data, int x, int z)
    {
        Vector3 terrainPos = targetTerrain.transform.position;

        float worldX = terrainPos.x + ((float)x / (data.heightmapResolution - 1)) * data.size.x;
        float worldZ = terrainPos.z + ((float)z / (data.heightmapResolution - 1)) * data.size.z;

        return new Vector3(worldX, 0, worldZ);
    }

    float DistanceToMapBorder(Vector3 world)
    {
        float left = Mathf.Abs(world.x - (-145));
        float right = Mathf.Abs(world.x - 145);
        float bottom = Mathf.Abs(world.z - (-112));
        float top = Mathf.Abs(world.z - 112);

        return Mathf.Min(left, right, bottom, top);
    }

    float PathInfluence(Vector3 world, Vector3[] points, float width, float targetHeight)
    {
        float d = PathDistance(world, points);

        if (d > width)
            return 0f;

        float t = 1f - Mathf.Clamp01(d / width);
        return Mathf.Lerp(baseHeight, targetHeight, Mathf.SmoothStep(0, 1, t));
    }

    float CircleInfluence(Vector3 world, Vector3 center, float radius, float targetHeight)
    {
        float d = Vector2.Distance(new Vector2(world.x, world.z), new Vector2(center.x, center.z));

        if (d > radius)
            return 0f;

        float t = 1f - Mathf.Clamp01(d / radius);
        return Mathf.Lerp(baseHeight, targetHeight, Mathf.SmoothStep(0, 1, t));
    }

    float PathDistance(Vector3 world, Vector3[] points)
    {
        float min = float.MaxValue;

        for (int i = 0; i < points.Length - 1; i++)
        {
            float d = DistancePointToSegment2D(world, points[i], points[i + 1]);
            if (d < min)
                min = d;
        }

        return min;
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

    Vector3 Mirror(Vector3 p)
    {
        return new Vector3(-p.x, p.y, -p.z);
    }
}