using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(15600)]
public class AOGPremiumMapVisualRuntime : MonoBehaviour
{
    private readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    private Transform premiumRoot;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Premium_Map_Visual_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPremiumMapVisualRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        BuildPremiumPass();
    }

    private void BuildPremiumPass()
    {
        if (built)
            return;

        Transform map = FindMapRoot();
        if (map == null)
            return;

        built = true;
        premiumRoot = new GameObject("AOG_Premium_Map_Pass").transform;
        premiumRoot.SetParent(map, false);

        UpgradeExistingMaterials(map);
        BuildLaneEdgeRunes(map);
        BuildObjectiveAtmosphere();
        BuildBaseAtmosphere();
        ApplyGlobalLook();
    }

    private Transform FindMapRoot()
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == "AOG_Symmetric_Reference_Map")
                return t;
        return null;
    }

    private void UpgradeExistingMaterials(Transform map)
    {
        foreach (Renderer renderer in map.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            string lower = renderer.gameObject.name.ToLowerInvariant();
            Material mat;

            if (lower.Contains("ground") || lower.Contains("grass") || lower.Contains("jungle"))
                mat = GetLit("Premium_Ground", new Color(0.035f, 0.12f, 0.075f), 0.12f, 0.02f);
            else if (lower.Contains("lane") || lower.Contains("road"))
                mat = GetLit("Premium_Lane", new Color(0.19f, 0.20f, 0.20f), 0.28f, 0.08f);
            else if (lower.Contains("river") || lower.Contains("pool"))
                mat = GetEmission("Premium_River", new Color(0.025f, 0.24f, 0.34f), 1.4f);
            else if (lower.Contains("blue") || lower.Contains("celestial"))
                mat = GetEmission("Premium_Blue", new Color(0.08f, 0.40f, 1f), 2.8f);
            else if (lower.Contains("red") || lower.Contains("fallen"))
                mat = GetEmission("Premium_Red", new Color(0.85f, 0.07f, 0.045f), 2.8f);
            else if (lower.Contains("void") || lower.Contains("medusa"))
                mat = GetEmission("Premium_Void", new Color(0.34f, 0.08f, 0.62f), 2.2f);
            else
                continue;

            renderer.sharedMaterial = mat;
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    private void BuildLaneEdgeRunes(Transform map)
    {
        string[] laneTokens = { "TOP_LANE_OUTER", "MID_LANE_CENTERED", "BOT_LANE_OUTER" };
        foreach (string token in laneTokens)
        {
            Transform lane = FindChildExact(map, token);
            if (lane == null)
                continue;

            Renderer[] pieces = lane.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < pieces.Length; i += 3)
            {
                Renderer piece = pieces[i];
                if (piece == null)
                    continue;

                Vector3 p = piece.bounds.center + Vector3.up * 0.08f;
                Color c = token.Contains("MID") ? new Color(0.42f, 0.18f, 0.82f) : new Color(0.08f, 0.46f, 0.72f);
                GameObject rune = AOGAbilityVisuals.CreateRing("Lane_Rune", p, 0.7f, c, 0.035f);
                rune.transform.SetParent(premiumRoot, true);
            }
        }
    }

    private void BuildObjectiveAtmosphere()
    {
        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            Color accent = boss.bossType == AOGNeutralBossType.Dragon
                ? new Color(1f, 0.16f, 0.03f)
                : new Color(0.52f, 0.12f, 0.90f);

            GameObject outer = AOGAbilityVisuals.CreateRing(boss.bossType + "_Pit_Outer", boss.transform.position + Vector3.up * 0.06f, 8.5f, accent, 0.12f);
            outer.transform.SetParent(premiumRoot, true);
            GameObject inner = AOGAbilityVisuals.CreateRing(boss.bossType + "_Pit_Inner", boss.transform.position + Vector3.up * 0.07f, 5.5f, accent * 0.85f, 0.055f);
            inner.transform.SetParent(premiumRoot, true);

            Light light = boss.GetComponentInChildren<Light>();
            if (light == null)
            {
                GameObject lightObject = new GameObject(boss.bossType + "_Arena_Light");
                lightObject.transform.SetParent(boss.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 5f, 0f);
                light = lightObject.AddComponent<Light>();
            }
            light.type = LightType.Point;
            light.color = accent;
            light.intensity = boss.bossType == AOGNeutralBossType.Dragon ? 2.0f : 1.6f;
            light.range = 18f;
            light.shadows = LightShadows.Soft;
        }
    }

    private void BuildBaseAtmosphere()
    {
        foreach (AOGNexusCore nexus in FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            Color accent = nexus.team == MinionTeam.Blue ? new Color(0.06f, 0.50f, 1f) : new Color(1f, 0.06f, 0.04f);
            for (int i = 0; i < 3; i++)
            {
                GameObject ring = AOGAbilityVisuals.CreateRing("Nexus_Aura_" + i, nexus.transform.position + Vector3.up * (0.08f + i * 0.04f), 6f + i * 2.5f, accent, 0.05f + i * 0.02f);
                ring.transform.SetParent(premiumRoot, true);
            }
        }
    }

    private void ApplyGlobalLook()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0048f;
        RenderSettings.fogColor = new Color(0.045f, 0.075f, 0.085f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.30f, 0.38f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.17f, 0.19f);
        RenderSettings.ambientGroundColor = new Color(0.035f, 0.055f, 0.05f);
        RenderSettings.reflectionIntensity = 0.85f;

        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional)
                continue;
            light.color = new Color(0.88f, 0.94f, 1f);
            light.intensity = Mathf.Max(light.intensity, 1.25f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.78f;
        }
    }

    private Transform FindChildExact(Transform root, string exact)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == exact)
                return t;
        return null;
    }

    private Material GetLit(string key, Color color, float smoothness, float metallic)
    {
        if (cache.TryGetValue(key, out Material existing) && existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = key, color = color, enableInstancing = true };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        cache[key] = mat;
        return mat;
    }

    private Material GetEmission(string key, Color color, float strength)
    {
        Material mat = GetLit(key, color, 0.38f, 0.08f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * strength);
        }
        return mat;
    }
}
