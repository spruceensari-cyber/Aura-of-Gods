using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime future-mythic presentation pass.
/// Adds subtle lane energy rails, objective pit halos and atmospheric anchors without replacing authored art.
/// </summary>
public class AOGFutureWorldRuntime : MonoBehaviour
{
    private const string RuntimeName = "AOG_Future_World_Runtime";
    private readonly List<GameObject> spawned = new();
    private Material cyan;
    private Material violet;
    private Material dragon;
    private Material medusa;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGFutureWorldRuntime>() != null)
            return;

        GameObject obj = new GameObject(RuntimeName);
        obj.AddComponent<AOGFutureWorldRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildMaterials();
    }

    void Start()
    {
        BuildWorldPass();
    }

    void Update()
    {
        AnimateAccents();
    }

    private void BuildMaterials()
    {
        cyan = MakeMaterial("Future_Cyan", new Color(0.06f, 0.72f, 1f), 1.8f);
        violet = MakeMaterial("Future_Violet", new Color(0.46f, 0.16f, 1f), 1.5f);
        dragon = MakeMaterial("Dragon_Ember", new Color(1f, 0.16f, 0.03f), 2.0f);
        medusa = MakeMaterial("Medusa_Gaze", new Color(0.24f, 1f, 0.42f), 1.7f);
    }

    private void BuildWorldPass()
    {
        MinionSpawner spawner = FindObjectOfType<MinionSpawner>();
        if (spawner != null)
        {
            BuildLaneRails(spawner.topLaneWaypoints, cyan, 0.24f);
            BuildLaneRails(spawner.midLaneWaypoints, violet, 0.28f);
            BuildLaneRails(spawner.botLaneWaypoints, cyan, 0.24f);
        }

        ObjectiveManager objectives = FindObjectOfType<ObjectiveManager>();
        if (objectives != null)
        {
            if (objectives.DragonObject != null)
                CreateHalo("Dragon_Pit_Halo", objectives.DragonObject.transform.position, 9f, dragon);
            if (objectives.MedusaObject != null)
                CreateHalo("Medusa_Pit_Halo", objectives.MedusaObject.transform.position, 9f, medusa);
        }
    }

    private void BuildLaneRails(Transform[] waypoints, Material material, float width)
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Transform a = waypoints[i];
            Transform b = waypoints[i + 1];
            if (a == null || b == null)
                continue;

            Vector3 direction = b.position - a.position;
            float length = direction.magnitude;
            if (length < 0.1f)
                continue;

            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "AOG_Lane_Energy_Rail";
            rail.transform.position = Vector3.Lerp(a.position, b.position, 0.5f) + Vector3.up * 0.06f;
            rail.transform.rotation = Quaternion.LookRotation(direction.normalized);
            rail.transform.localScale = new Vector3(width, 0.035f, length);
            rail.GetComponent<Renderer>().sharedMaterial = material;
            Collider col = rail.GetComponent<Collider>();
            if (col != null) Destroy(col);
            spawned.Add(rail);
        }
    }

    private void CreateHalo(string name, Vector3 position, float radius, Material material)
    {
        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        halo.name = name;
        halo.transform.position = position + Vector3.up * 0.05f;
        halo.transform.localScale = new Vector3(radius, 0.025f, radius);
        halo.GetComponent<Renderer>().sharedMaterial = material;
        Collider col = halo.GetComponent<Collider>();
        if (col != null) Destroy(col);
        spawned.Add(halo);
    }

    private void AnimateAccents()
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.04f;
        foreach (GameObject obj in spawned)
        {
            if (obj == null) continue;
            if (obj.name.Contains("Halo"))
                obj.transform.Rotate(Vector3.up, Time.unscaledDeltaTime * 12f, Space.World);
            else
            {
                Vector3 s = obj.transform.localScale;
                obj.transform.localScale = new Vector3(s.x, 0.035f * pulse, s.z);
            }
        }
    }

    private Material MakeMaterial(string name, Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }
        return mat;
    }
}
