using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGRedBaseBuilder : MonoBehaviour
{
    [Header("Base Position")]
    public Vector3 redBaseCenter = new Vector3(105f, 0f, 78f);

    [Header("Optional Guardian Prefab")]
    public GameObject fallenAngelGuardianPrefab;

    [Header("Scale Settings")]
    public float platformScale = 1f;
    public float guardianScale = 8f;

    private Material darkStoneMat;
    private Material redCrystalMat;
    private Material goldMat;
    private Material blackMetalMat;

    [ContextMenu("Build Red Base Visual")]
    public void BuildRedBaseVisual()
    {
        ClearRedBaseVisual();
        CreateMaterials();

        GameObject root = new GameObject("Red_Base_Visual");
        root.transform.SetParent(transform);

        // 1. Main platform
        CreateCylinder(
            "Red_Base_Main_Platform",
            redBaseCenter + new Vector3(0, 0.05f, 0),
            new Vector3(14f, 0.25f, 14f) * platformScale,
            darkStoneMat,
            root.transform
        );

        // 2. Outer ring platform
        CreateCylinder(
            "Red_Base_Outer_Ring",
            redBaseCenter + new Vector3(0, 0.35f, 0),
            new Vector3(10.5f, 0.22f, 10.5f) * platformScale,
            blackMetalMat,
            root.transform
        );

        // 3. Inner ring platform
        CreateCylinder(
            "Red_Base_Inner_Ring",
            redBaseCenter + new Vector3(0, 0.62f, 0),
            new Vector3(7f, 0.18f, 7f) * platformScale,
            darkStoneMat,
            root.transform
        );

        // 4. Nexus core base
        CreateCylinder(
            "Red_Nexus_Core_Base",
            redBaseCenter + new Vector3(0, 0.9f, 0),
            new Vector3(3.2f, 0.35f, 3.2f) * platformScale,
            blackMetalMat,
            root.transform
        );

        // 5. Red crystal core
        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crystal.name = "Red_Nexus_Crystal_Core";
        crystal.transform.SetParent(root.transform);
        crystal.transform.position = redBaseCenter + new Vector3(0, 2.3f, 0);
        crystal.transform.rotation = Quaternion.Euler(0, 0, 45);
        crystal.transform.localScale = new Vector3(1.1f, 2.2f, 1.1f);
        crystal.GetComponent<Renderer>().sharedMaterial = redCrystalMat;

        // 6. Top crystal sphere glow
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Red_Nexus_Glow_Orb";
        orb.transform.SetParent(root.transform);
        orb.transform.position = redBaseCenter + new Vector3(0, 4.6f, 0);
        orb.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
        orb.GetComponent<Renderer>().sharedMaterial = redCrystalMat;

        // 7. 8 pillars around base
        int pillarCount = 8;
        float radius = 8.5f * platformScale;

        for (int i = 0; i < pillarCount; i++)
        {
            float angle = i * Mathf.PI * 2f / pillarCount;
            Vector3 pos = redBaseCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            GameObject pillar = CreateCylinder(
                "Red_Base_Pillar_" + (i + 1),
                pos + new Vector3(0, 1.2f, 0),
                new Vector3(0.55f, 2.3f, 0.55f),
                blackMetalMat,
                root.transform
            );

            GameObject pillarOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pillarOrb.name = "Red_Base_Pillar_Glow_" + (i + 1);
            pillarOrb.transform.SetParent(root.transform);
            pillarOrb.transform.position = pos + new Vector3(0, 2.55f, 0);
            pillarOrb.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            pillarOrb.GetComponent<Renderer>().sharedMaterial = redCrystalMat;
        }

        // 8. Gold rune small plates
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Vector3 pos = redBaseCenter + new Vector3(Mathf.Cos(angle) * 5.2f, 1.05f, Mathf.Sin(angle) * 5.2f);

            GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rune.name = "Red_Base_Gold_Rune_" + (i + 1);
            rune.transform.SetParent(root.transform);
            rune.transform.position = pos;
            rune.transform.rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            rune.transform.localScale = new Vector3(0.8f, 0.05f, 0.18f);
            rune.GetComponent<Renderer>().sharedMaterial = goldMat;
        }

        // 9. Fallen angel guardian behind nexus
        if (fallenAngelGuardianPrefab != null)
        {
            Vector3 guardianPos = redBaseCenter + new Vector3(0f, 0f, 7.5f);

            GameObject guardian = Instantiate(fallenAngelGuardianPrefab, guardianPos, Quaternion.Euler(0, 180, 0), root.transform);
            guardian.name = "Red_Base_Back_Guardian";
            guardian.transform.localScale = Vector3.one * guardianScale;
        }

        Debug.Log("Red base visual built.");
    }

    [ContextMenu("Clear Red Base Visual")]
    public void ClearRedBaseVisual()
    {
        Transform old = transform.Find("Red_Base_Visual");

        if (old != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(old.gameObject);
            else
                Destroy(old.gameObject);
#else
            Destroy(old.gameObject);
#endif
        }
    }

    GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = scale;

        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
            r.sharedMaterial = mat;

        return obj;
    }

    void CreateMaterials()
    {
        darkStoneMat = CreateMaterial("AOG_Base_Dark_Stone", new Color(0.06f, 0.055f, 0.05f));
        blackMetalMat = CreateMaterial("AOG_Base_Black_Metal", new Color(0.015f, 0.012f, 0.012f));
        goldMat = CreateMaterial("AOG_Base_Dark_Gold", new Color(0.95f, 0.55f, 0.12f));
        redCrystalMat = CreateMaterial("AOG_Base_Red_Crystal", new Color(1f, 0.05f, 0.02f));
    }

    Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;

        if (name.Contains("Crystal"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.5f);
        }

        return mat;
    }
}