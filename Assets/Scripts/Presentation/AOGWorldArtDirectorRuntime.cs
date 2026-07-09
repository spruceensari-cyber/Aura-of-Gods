using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(15000)]
public class AOGWorldArtDirectorRuntime : MonoBehaviour
{
    private static AOGWorldArtDirectorRuntime instance;
    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private Transform artRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (instance != null)
            instance.StartCoroutine(instance.RebuildAfterStartup());
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        AOGWorldArtDirectorRuntime existing = FindFirstObjectByType<AOGWorldArtDirectorRuntime>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject host = new GameObject("AOG_World_Art_Director");
        instance = host.AddComponent<AOGWorldArtDirectorRuntime>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator RebuildAfterStartup()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        BuildArtLayer();
    }

    private void BuildArtLayer()
    {
        if (artRoot != null)
            Destroy(artRoot.gameObject);

        GameObject rootObject = new GameObject("AOG_Runtime_World_Art_Layer");
        artRoot = rootObject.transform;

        BuildLaneLandmarks();
        BuildTowerAccents();
        BuildBaseAccents();
        ImproveExistingRendererSettings();
    }

    private void BuildLaneLandmarks()
    {
        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner == null)
            return;

        BuildForLane(spawner.topLaneWaypoints, 0);
        BuildForLane(spawner.midLaneWaypoints, 1);
        BuildForLane(spawner.botLaneWaypoints, 2);
    }

    private void BuildForLane(Transform[] waypoints, int laneIndex)
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform waypoint = waypoints[i];
            if (waypoint == null)
                continue;

            Vector3 forward;
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                forward = waypoints[i + 1].position - waypoint.position;
            else if (i > 0 && waypoints[i - 1] != null)
                forward = waypoint.position - waypoints[i - 1].position;
            else
                forward = Vector3.forward;

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            if (i % 2 == 0)
            {
                BuildStoneCluster(waypoint.position + right * 4.8f, laneIndex * 31 + i * 17);
                BuildStoneCluster(waypoint.position - right * 5.1f, laneIndex * 47 + i * 23 + 3);
            }

            if (i % 3 == 1)
            {
                BuildAetherLantern(waypoint.position + right * 3.9f, laneIndex == 1 ? new Color(0.52f, 0.30f, 0.90f) : new Color(0.22f, 0.62f, 0.92f));
                BuildAetherLantern(waypoint.position - right * 3.9f, laneIndex == 1 ? new Color(0.52f, 0.30f, 0.90f) : new Color(0.22f, 0.62f, 0.92f));
            }

            BuildGrassTufts(waypoint.position + right * 5.8f, forward, laneIndex * 13 + i * 7);
            BuildGrassTufts(waypoint.position - right * 5.8f, -forward, laneIndex * 19 + i * 11);
        }
    }

    private void BuildStoneCluster(Vector3 position, int seed)
    {
        Material stone = GetLitMaterial("WorldStone", new Color(0.16f, 0.20f, 0.21f), 0.16f, 0.04f);
        int count = 3 + Mathf.Abs(seed % 3);

        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Repeat(seed * 0.73f + i * 2.1f, Mathf.PI * 2f);
            float radius = 0.35f + Mathf.Repeat(seed * 0.17f + i * 0.31f, 0.65f);
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Lane_Rock";
            rock.transform.SetParent(artRoot, false);
            rock.transform.position = position + new Vector3(Mathf.Cos(angle) * radius, 0.15f, Mathf.Sin(angle) * radius);
            float scale = 0.35f + Mathf.Repeat(seed * 0.11f + i * 0.23f, 0.65f);
            rock.transform.localScale = new Vector3(scale, scale * 0.65f, scale * 1.15f);
            rock.transform.rotation = Quaternion.Euler(12f + i * 7f, seed * 19f + i * 37f, i * 11f);
            rock.GetComponent<Renderer>().sharedMaterial = stone;
            Destroy(rock.GetComponent<Collider>());
        }
    }

    private void BuildGrassTufts(Vector3 position, Vector3 forward, int seed)
    {
        Material grass = GetLitMaterial("WorldGrassTuft", new Color(0.10f, 0.28f, 0.15f), 0.04f, 0f);
        const int count = 5;

        for (int i = 0; i < count; i++)
        {
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Grass_Tuft";
            blade.transform.SetParent(artRoot, false);
            float lateral = (i - 2) * 0.20f;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x).normalized;
            blade.transform.position = position + right * lateral + forward * Mathf.Sin(seed + i * 1.7f) * 0.25f + Vector3.up * 0.45f;
            blade.transform.localScale = new Vector3(0.09f, 0.86f + i * 0.05f, 0.09f);
            blade.transform.rotation = Quaternion.Euler(8f + i * 3f, seed * 13f + i * 29f, (i - 2) * 7f);
            blade.GetComponent<Renderer>().sharedMaterial = grass;
            blade.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Destroy(blade.GetComponent<Collider>());
        }
    }

    private void BuildAetherLantern(Vector3 position, Color color)
    {
        Material metal = GetLitMaterial("LanternMetal", new Color(0.055f, 0.07f, 0.085f), 0.56f, 0.64f);
        Material energy = GetEmissionMaterial("LanternEnergy_" + color.ToString(), color, 4f);

        GameObject root = new GameObject("Aether_Lantern");
        root.transform.SetParent(artRoot, false);
        root.transform.position = position;

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.transform.SetParent(root.transform, false);
        post.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        post.transform.localScale = new Vector3(0.10f, 0.9f, 0.10f);
        post.GetComponent<Renderer>().sharedMaterial = metal;
        Destroy(post.GetComponent<Collider>());

        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crystal.name = "Aether_Crystal";
        crystal.transform.SetParent(root.transform, false);
        crystal.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        crystal.transform.localScale = new Vector3(0.34f, 0.58f, 0.34f);
        crystal.GetComponent<Renderer>().sharedMaterial = energy;
        Destroy(crystal.GetComponent<Collider>());

        Light light = crystal.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 0.55f;
        light.range = 4.5f;
        light.shadows = LightShadows.None;

        AOGOrbitAnimator spin = crystal.AddComponent<AOGOrbitAnimator>();
        spin.speed = 24f;
    }

    private void BuildTowerAccents()
    {
        TowerHealth[] towers = FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (TowerHealth tower in towers)
        {
            if (tower == null)
                continue;

            Color color = tower.towerTeam == MinionTeam.Blue ? new Color(0.18f, 0.62f, 1f) : new Color(1f, 0.18f, 0.22f);
            Material energy = GetEmissionMaterial("TowerGroundRune_" + tower.towerTeam, color, 3.2f);

            GameObject rune = new GameObject(tower.name + "_Ground_Rune");
            rune.transform.SetParent(artRoot, false);
            rune.transform.position = tower.transform.position + Vector3.up * 0.06f;
            LineRenderer line = rune.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = false;
            line.positionCount = 48;
            line.startWidth = 0.055f;
            line.endWidth = 0.055f;
            line.sharedMaterial = energy;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / line.positionCount;
                float r = 2.2f + Mathf.Sin(a * 6f) * 0.15f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            AOGOrbitAnimator orbit = rune.AddComponent<AOGOrbitAnimator>();
            orbit.speed = tower.towerTeam == MinionTeam.Blue ? 4f : -4f;
        }
    }

    private void BuildBaseAccents()
    {
        AOGNexusCore[] nexuses = FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGNexusCore nexus in nexuses)
        {
            if (nexus == null)
                continue;

            Color color = nexus.team == MinionTeam.Blue ? new Color(0.18f, 0.62f, 1f) : new Color(1f, 0.18f, 0.22f);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                Vector3 pos = nexus.transform.position + new Vector3(Mathf.Cos(angle) * 5.2f, 0f, Mathf.Sin(angle) * 5.2f);
                BuildAetherLantern(pos, color);
            }
        }
    }

    private void ImproveExistingRendererSettings()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.receiveShadows = true;
            if (!(renderer is ParticleSystemRenderer) && renderer.shadowCastingMode != ShadowCastingMode.Off)
                renderer.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    private Material GetLitMaterial(string key, Color color, float smoothness, float metallic)
    {
        if (materials.TryGetValue(key, out Material cached) && cached != null)
            return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = key, color = color, enableInstancing = true };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        materials[key] = material;
        return material;
    }

    private Material GetEmissionMaterial(string key, Color color, float strength)
    {
        if (materials.TryGetValue(key, out Material cached) && cached != null)
            return cached;

        Material material = GetLitMaterial(key, color, 0.45f, 0.12f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
        }
        return material;
    }
}
