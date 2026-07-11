using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Keeps minions clearly secondary to champions while giving Melee, Ranged and Cannon roles
/// different silhouettes. All added presentation pieces are collider-free and use shared materials.
/// </summary>
[DefaultExecutionOrder(16050)]
public class AOGMinionSecondaryUnitRefinementRuntime : MonoBehaviour
{
    private readonly HashSet<Minion> processed = new HashSet<Minion>();
    private static readonly Dictionary<string,Material> materialCache = new Dictionary<string,Material>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGMinionSecondaryUnitRefinementRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Minion_Secondary_Unit_Refinement");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMinionSecondaryUnitRefinementRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.65f;

        foreach (Minion minion in Minion.Active)
        {
            if (minion == null || processed.Contains(minion)) continue;
            Refine(minion);
            processed.Add(minion);
        }

        processed.RemoveWhere(m => m == null);
    }

    private static void Refine(Minion minion)
    {
        Transform visual = minion.transform.Find("AOG_Minon_Visual");
        if (visual == null) return;
        if (visual.Find("Clean_Role_Accents") != null) return;

        visual.localScale = minion.role == MinionRole.Cannon
            ? Vector3.one * 0.82f
            : minion.role == MinionRole.Ranged
                ? Vector3.one * 0.69f
                : Vector3.one * 0.72f;

        Color team = minion.team == MinionTeam.Blue
            ? new Color(0.10f,0.48f,1f)
            : new Color(1f,0.14f,0.20f);
        Material energy = SharedMaterial("MinionCleanEnergy_" + minion.team,team,true);
        Material armor = SharedMaterial(
            "MinionCleanArmor_" + minion.team,
            minion.team == MinionTeam.Blue ? new Color(0.055f,0.095f,0.16f) : new Color(0.16f,0.035f,0.05f),
            false);

        Transform accents = new GameObject("Clean_Role_Accents").transform;
        accents.SetParent(visual,false);

        if (minion.role == MinionRole.Melee)
        {
            Transform weapon = FindDeep(visual,"Weapon");
            if (weapon != null) weapon.gameObject.SetActive(false);

            CreatePart(accents,PrimitiveType.Cube,"Impact_Gauntlet_R",new Vector3(0.52f,0.66f,0.24f),new Vector3(0.28f,0.32f,0.34f),energy);
            CreatePart(accents,PrimitiveType.Cube,"Impact_Gauntlet_L",new Vector3(-0.52f,0.66f,0.24f),new Vector3(0.28f,0.32f,0.34f),energy);
            GameObject shield = CreatePart(accents,PrimitiveType.Cylinder,"Compact_Shield",new Vector3(-0.64f,0.98f,0.28f),new Vector3(0.44f,0.09f,0.44f),armor);
            shield.transform.localRotation = Quaternion.Euler(90f,0f,0f);
        }
        else if (minion.role == MinionRole.Ranged)
        {
            Transform weapon = FindDeep(visual,"Weapon");
            if (weapon != null) weapon.localScale = new Vector3(weapon.localScale.x*0.72f,weapon.localScale.y*0.78f,weapon.localScale.z*0.72f);
            Transform body = FindDeep(visual,"Body");
            if (body != null) body.localScale = new Vector3(body.localScale.x*0.86f,body.localScale.y*1.08f,body.localScale.z*0.86f);

            CreatePart(accents,PrimitiveType.Sphere,"Ranged_Focus",new Vector3(0f,1.58f,0.34f),Vector3.one*0.22f,energy);
            GameObject backFin = CreatePart(accents,PrimitiveType.Cube,"Ranged_Back_Fin",new Vector3(0f,1.05f,-0.34f),new Vector3(0.10f,0.70f,0.30f),armor);
            backFin.transform.localRotation = Quaternion.Euler(12f,0f,0f);
        }
        else
        {
            Transform body = FindDeep(visual,"Body");
            if (body != null) body.localScale *= 1.06f;

            CreatePart(accents,PrimitiveType.Cube,"Cannon_Side_Pod_L",new Vector3(-0.92f,0.72f,0f),new Vector3(0.34f,0.42f,0.70f),armor);
            CreatePart(accents,PrimitiveType.Cube,"Cannon_Side_Pod_R",new Vector3(0.92f,0.72f,0f),new Vector3(0.34f,0.42f,0.70f),armor);
            CreatePart(accents,PrimitiveType.Sphere,"Cannon_Reactor",new Vector3(0f,1.38f,0.05f),Vector3.one*0.32f,energy);
        }
    }

    private static GameObject CreatePart(Transform parent,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        return go;
    }

    private static Transform FindDeep(Transform root,string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private static Material SharedMaterial(string key,Color color,bool emission)
    {
        if (materialCache.TryGetValue(key,out Material cached) && cached != null) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = key, color = color, enableInstancing = true };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",emission ? 0.38f : 0.28f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",emission ? 0.08f : 0.30f);
        if (emission && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",color*2.8f);
        }
        materialCache[key] = mat;
        return mat;
    }
}
