using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds a lightweight humanoid silhouette layer to authoritative roster champions. The existing
/// champion model remains intact; this fills missing limb/boot/weapon silhouette gaps and adds
/// low-cost procedural locomotion for desktop readability.
/// </summary>
[DefaultExecutionOrder(15960)]
public class AOGCleanChampionRigPolishRuntime : MonoBehaviour
{
    private static readonly Dictionary<string,Material> bodyMaterials = new Dictionary<string,Material>();
    private static readonly Dictionary<string,Material> energyMaterials = new Dictionary<string,Material>();
    private readonly HashSet<AOGTeamMemberIdentity> processed = new HashSet<AOGTeamMemberIdentity>();
    private float nextAttach;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCleanChampionRigPolishRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Clean_Champion_Rig_Polish_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCleanChampionRigPolishRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextAttach) return;
        nextAttach = Time.unscaledTime + 0.8f;

        foreach (AOGTeamMemberIdentity member in AOGWorldRegistry.TeamMembers)
        {
            if (member == null || processed.Contains(member)) continue;
            Build(member);
            processed.Add(member);
        }
    }

    private static void Build(AOGTeamMemberIdentity member)
    {
        Transform model = member.transform.Find("AOG_Original_Champion_Model");
        if (model == null) return;
        if (model.Find("Clean_Humanoid_Rig") != null) return;

        Color accent = ResolveAccent(member.championId,member.team);
        Material body = GetBodyMaterial(member.championId,member.team,accent);
        Material energy = GetEnergyMaterial(member.championId,member.team,accent);

        Transform rig = new GameObject("Clean_Humanoid_Rig").transform;
        rig.SetParent(model,false);

        Transform armL = CreateLimb(rig,"Arm_L",new Vector3(-0.60f,1.48f,0f),new Vector3(0.22f,0.66f,0.22f),body,8f);
        Transform armR = CreateLimb(rig,"Arm_R",new Vector3(0.60f,1.48f,0f),new Vector3(0.22f,0.66f,0.22f),body,-8f);
        Transform legL = CreateLimb(rig,"Leg_L",new Vector3(-0.23f,-0.30f,0f),new Vector3(0.26f,0.78f,0.26f),body,0f);
        Transform legR = CreateLimb(rig,"Leg_R",new Vector3(0.23f,-0.30f,0f),new Vector3(0.26f,0.78f,0.26f),body,0f);

        CreatePart(rig,PrimitiveType.Cube,"Boot_L",new Vector3(-0.23f,-1.05f,0.15f),new Vector3(0.32f,0.22f,0.48f),body);
        CreatePart(rig,PrimitiveType.Cube,"Boot_R",new Vector3(0.23f,-1.05f,0.15f),new Vector3(0.32f,0.22f,0.48f),body);
        CreatePart(rig,PrimitiveType.Sphere,"Chest_Accent",new Vector3(0f,1.62f,0.43f),Vector3.one*0.13f,energy);

        BuildWeaponIdentity(rig,member.championId,energy,body);

        AOGCleanChampionLocomotionRuntime locomotion = member.GetComponent<AOGCleanChampionLocomotionRuntime>();
        if (locomotion == null) locomotion = member.gameObject.AddComponent<AOGCleanChampionLocomotionRuntime>();
        locomotion.armL = armL;
        locomotion.armR = armR;
        locomotion.legL = legL;
        locomotion.legR = legR;
    }

    private static Transform CreateLimb(Transform parent,string name,Vector3 position,Vector3 scale,Material material,float zRotation)
    {
        GameObject limb = CreatePart(parent,PrimitiveType.Capsule,name,position,scale,material);
        limb.transform.localRotation = Quaternion.Euler(0f,0f,zRotation);
        return limb.transform;
    }

    private static void BuildWeaponIdentity(Transform root,string id,Material energy,Material body)
    {
        if (id == "kaelith" || id == "auron" || id == "dravenor" || id == "nocthyr")
        {
            GameObject weapon = CreatePart(root,PrimitiveType.Cube,"Clean_Melee_Weapon",new Vector3(0.78f,0.80f,0.18f),new Vector3(0.12f,1.18f,0.16f),energy);
            weapon.transform.localRotation = Quaternion.Euler(0f,0f,-18f);
        }
        else if (id == "vesper")
        {
            GameObject bow = CreatePart(root,PrimitiveType.Torus,"Clean_Ranged_Bow",new Vector3(0.72f,1.10f,0.15f),new Vector3(0.52f,0.72f,0.10f),energy);
            bow.transform.localRotation = Quaternion.Euler(90f,0f,0f);
        }
        else
        {
            GameObject focus = CreatePart(root,PrimitiveType.Sphere,"Clean_Caster_Focus",new Vector3(0.72f,1.20f,0.22f),Vector3.one*0.22f,energy);
            AOGCleanOrbitAccentRuntime orbit = focus.AddComponent<AOGCleanOrbitAccentRuntime>();
            orbit.radius = 0.12f;
            orbit.speed = 55f;
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
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        return go;
    }

    private static Material GetBodyMaterial(string id,MinionTeam team,Color accent)
    {
        string key = id + "_" + team + "_body";
        if (bodyMaterials.TryGetValue(key,out Material mat) && mat != null) return mat;
        Color baseColor = team == MinionTeam.Blue
            ? Color.Lerp(new Color(0.045f,0.055f,0.075f),accent,0.18f)
            : Color.Lerp(new Color(0.065f,0.025f,0.035f),accent,0.18f);
        mat = MakeLit(key,baseColor,0.38f,0.22f,false);
        bodyMaterials[key] = mat;
        return mat;
    }

    private static Material GetEnergyMaterial(string id,MinionTeam team,Color accent)
    {
        string key = id + "_" + team + "_energy";
        if (energyMaterials.TryGetValue(key,out Material mat) && mat != null) return mat;
        mat = MakeLit(key,accent,0.48f,0.12f,true);
        energyMaterials[key] = mat;
        return mat;
    }

    private static Material MakeLit(string name,Color color,float smoothness,float metallic,bool emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = color, enableInstancing = true };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",metallic);
        if (emission && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",color*3.2f);
        }
        return mat;
    }

    private static Color ResolveAccent(string id,MinionTeam team)
    {
        switch (id)
        {
            case "lyra": return new Color(0.62f,0.28f,0.92f);
            case "kaelith": return new Color(0.36f,0.18f,0.86f);
            case "auron": return new Color(1f,0.56f,0.12f);
            case "vesper": return new Color(0.12f,0.78f,0.95f);
            case "nyra": return new Color(0.94f,0.28f,0.74f);
            case "pyrelle": return new Color(1f,0.22f,0.035f);
            case "selene": return new Color(0.36f,0.68f,1f);
            case "seris": return new Color(0.24f,0.88f,0.95f);
            case "mireva": return new Color(0.18f,0.82f,0.42f);
            case "dravenor": return new Color(0.92f,0.28f,0.08f);
            case "nocthyr": return new Color(0.28f,0.18f,0.72f);
            default: return team == MinionTeam.Blue ? new Color(0.18f,0.62f,1f) : new Color(1f,0.18f,0.22f);
        }
    }
}

public class AOGCleanChampionLocomotionRuntime : MonoBehaviour
{
    public Transform armL;
    public Transform armR;
    public Transform legL;
    public Transform legR;

    private Vector3 lastPosition;
    private float stridePhase;

    private void OnEnable() { lastPosition = transform.position; }

    private void Update()
    {
        Vector3 delta = transform.position-lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;
        float speed = delta.magnitude/Mathf.Max(Time.deltaTime,0.001f);
        float blend = Mathf.Clamp01(speed/3.5f);
        stridePhase += Time.deltaTime*Mathf.Lerp(2f,9f,blend);
        float swing = Mathf.Sin(stridePhase)*28f*blend;

        if (armL != null) armL.localRotation = Quaternion.Euler(swing,0f,8f);
        if (armR != null) armR.localRotation = Quaternion.Euler(-swing,0f,-8f);
        if (legL != null) legL.localRotation = Quaternion.Euler(-swing,0f,0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(swing,0f,0f);
    }
}

public class AOGCleanOrbitAccentRuntime : MonoBehaviour
{
    public float radius = 0.12f;
    public float speed = 55f;
    private float angle;
    private Vector3 basePosition;

    private void Start() { basePosition = transform.localPosition; }

    private void Update()
    {
        angle += speed*Time.deltaTime*Mathf.Deg2Rad;
        transform.localPosition = basePosition+new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle*0.7f)*radius*0.4f,Mathf.Sin(angle)*radius);
    }
}
