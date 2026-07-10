using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One-time art-direction and cleanup pass for the current three-lane map. Gameplay routes remain
/// intact; the pass improves materials, removes obsolete procedural creature placeholders and reduces
/// decorative physics cost.
/// </summary>
[DefaultExecutionOrder(16200)]
public class AOGBenchmarkMapArtDirectionRuntime : MonoBehaviour
{
    private static Material laneStone;
    private static Material laneEdge;
    private static Material ground;
    private static Material foliage;
    private static Material trunk;
    private static Material river;
    private static Material blueEnergy;
    private static Material redEnergy;
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGBenchmarkMapArtDirectionRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Benchmark_Map_Art_Direction_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGBenchmarkMapArtDirectionRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        ApplyWhenReady();
    }

    private void ApplyWhenReady()
    {
        if (applied) return;
        GameObject map = GameObject.Find("AOG_Symmetric_Reference_Map");
        if (map == null)
        {
            StartCoroutine(Retry());
            return;
        }

        applied = true;
        BuildMaterials();
        RestyleMap(map.transform);
        DisableDecorativeColliders(map.transform);
        HideProceduralCreaturePlaceholders(map.transform);
        ImproveWorldLighting();
    }

    private IEnumerator Retry()
    {
        yield return new WaitForSecondsRealtime(1f);
        ApplyWhenReady();
    }

    private static void BuildMaterials()
    {
        if (laneStone != null) return;
        laneStone = Lit("AOG_Benchmark_Lane_Stone",new Color(0.25f,0.245f,0.22f),0.30f,0.08f);
        laneEdge = Lit("AOG_Benchmark_Lane_Edge",new Color(0.095f,0.09f,0.085f),0.24f,0.14f);
        ground = Lit("AOG_Benchmark_Ground",new Color(0.025f,0.105f,0.055f),0.16f,0.02f);
        foliage = Lit("AOG_Benchmark_Foliage",new Color(0.018f,0.13f,0.065f),0.12f,0f);
        trunk = Lit("AOG_Benchmark_Trunk",new Color(0.13f,0.075f,0.042f),0.10f,0f);
        river = Lit("AOG_Benchmark_Aether_River",new Color(0.018f,0.16f,0.22f),0.62f,0.06f);
        blueEnergy = Emissive("AOG_Benchmark_Blue_Energy",new Color(0.08f,0.46f,1f),2.8f);
        redEnergy = Emissive("AOG_Benchmark_Red_Energy",new Color(0.90f,0.055f,0.08f),2.6f);
    }

    private static void RestyleMap(Transform map)
    {
        foreach (Renderer renderer in map.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            string n = renderer.gameObject.name.ToLowerInvariant();

            if (n.Contains("main_dark_ground")) renderer.sharedMaterial = ground;
            else if (n.Contains("_stone_") || n.Contains("lane_centered_stone") || n.Contains("lane_outer_stone")) renderer.sharedMaterial = laneStone;
            else if (n.Contains("_edge_") || n.Contains("road_edge")) renderer.sharedMaterial = laneEdge;
            else if (n.Contains("river") || n.Contains("pool")) renderer.sharedMaterial = river;
            else if (n.Contains("tree") && !n.Contains("trunk")) renderer.sharedMaterial = foliage;
            else if (n.Contains("trunk")) renderer.sharedMaterial = trunk;
            else if (n.Contains("blue") && (n.Contains("core") || n.Contains("crystal") || n.Contains("orb") || n.Contains("head"))) renderer.sharedMaterial = blueEnergy;
            else if (n.Contains("red") && (n.Contains("core") || n.Contains("crystal") || n.Contains("orb") || n.Contains("head"))) renderer.sharedMaterial = redEnergy;
        }
    }

    private static void DisableDecorativeColliders(Transform map)
    {
        foreach (Collider collider in map.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null) continue;
            string n = collider.gameObject.name.ToLowerInvariant();

            bool decorative = n.Contains("dark_tree_crown") ||
                              n.Contains("small_gold_rune") ||
                              n.Contains("broken_road_stone") ||
                              n.Contains("camp_ruin_pillar") ||
                              n.Contains("torch") ||
                              n.Contains("rune") ||
                              n.Contains("aura") ||
                              n.Contains("trim_stone");

            if (decorative) collider.enabled = false;
        }
    }

    private static void HideProceduralCreaturePlaceholders(Transform map)
    {
        List<AOGNeutralMonsterRuntime> monsters = new List<AOGNeutralMonsterRuntime>(AOGWorldRegistry.NeutralMonsters);
        List<AOGNeutralBossAI> bosses = new List<AOGNeutralBossAI>(AOGWorldRegistry.Bosses);

        foreach (Transform child in map.GetComponentsInChildren<Transform>(true))
        {
            if (child == null) continue;
            string n = child.name.ToLowerInvariant();
            bool campPlaceholder = n.Contains("camp_creature_core");
            bool bossPlaceholder = n.EndsWith("_creature");
            if (!campPlaceholder && !bossPlaceholder) continue;

            bool hasReal = false;
            if (campPlaceholder)
            {
                foreach (AOGNeutralMonsterRuntime monster in monsters)
                {
                    if (monster != null && Vector3.Distance(monster.transform.position,child.position) <= 12f)
                    {
                        hasReal = true;
                        break;
                    }
                }
            }
            else
            {
                foreach (AOGNeutralBossAI boss in bosses)
                {
                    if (boss != null && Vector3.Distance(boss.transform.position,child.position) <= 18f)
                    {
                        hasReal = true;
                        break;
                    }
                }
            }

            if (hasReal) child.gameObject.SetActive(false);
        }
    }

    private static void ImproveWorldLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f,0.26f,0.31f);
        RenderSettings.ambientEquatorColor = new Color(0.11f,0.16f,0.15f);
        RenderSettings.ambientGroundColor = new Color(0.035f,0.05f,0.04f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.055f,0.075f,0.08f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0038f;

        Light directional = null;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (light == null) continue;
            if (light.type == LightType.Directional && directional == null) directional = light;
            if (light.type == LightType.Point && light.range <= 9f) light.shadows = LightShadows.None;
        }

        if (directional != null)
        {
            directional.intensity = Mathf.Clamp(directional.intensity,1.05f,1.35f);
            directional.color = new Color(0.96f,0.95f,0.90f);
            directional.shadows = LightShadows.Soft;
        }
    }

    private static Material Lit(string name,Color color,float smoothness,float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = color, enableInstancing = true };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",metallic);
        return mat;
    }

    private static Material Emissive(string name,Color color,float strength)
    {
        Material mat = Lit(name,color,0.42f,0.16f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",color*strength);
        }
        return mat;
    }
}
