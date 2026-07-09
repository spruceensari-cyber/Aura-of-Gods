using UnityEngine;

public static class AOGChampionVisualFactory
{
    public static void BuildKaelithVisual(Transform parent)
    {
        if (parent == null || parent.Find("Kaelith_Procedural_Visual") != null)
            return;

        Color armorColor = new Color(0.075f, 0.06f, 0.13f, 1f);
        Color energyColor = new Color(0.42f, 0.14f, 0.88f, 1f);
        Material armor = CreateMaterial("Kaelith_Armor", armorColor, 0.58f, 0.48f, false);
        Material energy = CreateMaterial("Kaelith_Energy", energyColor, 0.42f, 0.08f, true);

        GameObject root = new GameObject("Kaelith_Procedural_Visual");
        root.transform.SetParent(parent, false);

        CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.74f, 1.05f, 0.56f), armor);
        CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f, 2.55f, 0f), new Vector3(0.58f, 0.68f, 0.58f), armor);
        CreatePrimitive(PrimitiveType.Cube, "Shoulder_L", root.transform, new Vector3(-0.68f, 1.92f, 0f), new Vector3(0.55f, 0.28f, 0.72f), armor);
        CreatePrimitive(PrimitiveType.Cube, "Shoulder_R", root.transform, new Vector3(0.68f, 1.92f, 0f), new Vector3(0.55f, 0.28f, 0.72f), armor);

        Transform bladeLeft = CreatePrimitive(PrimitiveType.Cube, "VoidBlade_L", root.transform, new Vector3(-0.78f, 1.05f, 0.2f), new Vector3(0.13f, 1.15f, 0.18f), energy).transform;
        bladeLeft.localRotation = Quaternion.Euler(18f, 0f, 22f);
        Transform bladeRight = CreatePrimitive(PrimitiveType.Cube, "VoidBlade_R", root.transform, new Vector3(0.78f, 1.05f, 0.2f), new Vector3(0.13f, 1.15f, 0.18f), energy).transform;
        bladeRight.localRotation = Quaternion.Euler(18f, 0f, -22f);

        GameObject crown = new GameObject("Eclipse_Crown");
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0f, 3.05f, 0f);
        LineRenderer ring = crown.AddComponent<LineRenderer>();
        ring.loop = true;
        ring.useWorldSpace = false;
        ring.positionCount = 48;
        ring.startWidth = 0.055f;
        ring.endWidth = 0.055f;
        ring.sharedMaterial = energy;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.72f, Mathf.Sin(a) * 0.20f, Mathf.Sin(a) * 0.72f));
        }

        AOGOrbitAnimator orbit = crown.AddComponent<AOGOrbitAnimator>();
        orbit.localAxis = Vector3.up;
        orbit.speed = 36f;
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        return go;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, float metallic, bool emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (emission && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 4f);
        }
        return mat;
    }
}
