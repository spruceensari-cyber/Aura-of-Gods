using UnityEngine;

public class AOGSealWorldBar : MonoBehaviour
{
    private AOGStrategicLaneSeal seal;
    private Transform root;
    private Transform fill;

    private void Start()
    {
        seal = GetComponent<AOGStrategicLaneSeal>();
        AOGObjectiveWorldBar generic = GetComponent<AOGObjectiveWorldBar>();
        if (generic != null) generic.enabled = false;
        Build();
    }

    private void LateUpdate()
    {
        if (seal == null || root == null || fill == null) return;
        float ratio = Mathf.Clamp01(seal.hp / Mathf.Max(1f,seal.maxHp));
        fill.localScale = new Vector3(ratio,1f,1f);
        fill.localPosition = new Vector3(-(1f-ratio)*0.5f,0f,-0.03f);
        root.gameObject.SetActive(seal.State == AOGSealState.Active);
        if (Camera.main != null) root.rotation = Camera.main.transform.rotation;
    }

    private void Build()
    {
        GameObject rootObject = new GameObject("AOG_Seal_HP_Bar");
        rootObject.transform.SetParent(transform,false);
        rootObject.transform.localPosition = new Vector3(0f,4.8f,0f);
        root = rootObject.transform;

        GameObject border = Cube("Border",root,new Vector3(3.3f,0.32f,0.08f),new Color(0.01f,0.015f,0.025f));
        GameObject bg = Cube("Background",border.transform,new Vector3(0.95f,0.56f,0.75f),new Color(0.06f,0.075f,0.085f));
        Color color = seal != null && seal.team == MinionTeam.Red ? new Color(1f,0.20f,0.24f) : new Color(0.18f,0.62f,1f);
        fill = Cube("Fill",bg.transform,new Vector3(1f,0.74f,0.65f),color).transform;
    }

    private static GameObject Cube(string name,Transform parent,Vector3 scale,Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent,false);
        go.transform.localScale = scale;
        Collider col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { color=color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }
}

public class AOGTowerDamageStageRuntime : MonoBehaviour
{
    private TowerHealth tower;
    private Renderer[] renderers;
    private int stage = -1;
    private float nextPulse;

    private void Awake()
    {
        tower = GetComponent<TowerHealth>();
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        if (tower == null) return;
        float ratio = Mathf.Clamp01(tower.hp / Mathf.Max(1f,tower.maxHp));
        int newStage = ratio > 0.75f ? 0 : ratio > 0.50f ? 1 : ratio > 0.25f ? 2 : tower.hp > 0f ? 3 : 4;
        if (newStage != stage)
        {
            stage = newStage;
            ApplyStage();
        }

        if (stage >= 2 && tower.hp > 0f && Time.time >= nextPulse)
        {
            nextPulse = Time.time + (stage == 3 ? 1.1f : 2.0f);
            Color c = tower.towerTeam == MinionTeam.Blue ? new Color(0.22f,0.62f,1f) : new Color(1f,0.20f,0.24f);
            GameObject ring = AOGAbilityVisuals.CreateRing("Tower_Unstable_Pulse",transform.position+Vector3.up*0.05f,1.4f+stage*0.25f,c,0.04f);
            Destroy(ring,0.35f);
        }
    }

    private void ApplyStage()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        float emission = stage == 0 ? 1f : stage == 1 ? 1.6f : stage == 2 ? 2.4f : stage == 3 ? 3.3f : 0.2f;
        Color teamColor = tower.towerTeam == MinionTeam.Blue ? new Color(0.18f,0.58f,1f) : new Color(1f,0.16f,0.22f);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            foreach (Material material in renderer.materials)
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor",teamColor*emission);
                }
                if (stage >= 2 && material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness",Mathf.Max(0.12f,0.55f-stage*0.10f));
            }
        }
        GameObject ring = AOGAbilityVisuals.CreateRing("Tower_Damage_Stage_"+stage,transform.position+Vector3.up*0.06f,1.8f+stage*0.35f,teamColor,0.06f);
        Destroy(ring,0.8f);
    }
}

public class AOGNexusFinalStatePresentationRuntime : MonoBehaviour
{
    private AOGNexusCore nexus;
    private int stage = -1;
    private float nextPulse;

    private void Awake() { nexus = GetComponent<AOGNexusCore>(); }

    private void Update()
    {
        if (nexus == null) return;
        float ratio = Mathf.Clamp01(nexus.hp/Mathf.Max(1f,nexus.maxHp));
        int newStage = ratio > 0.66f ? 0 : ratio > 0.33f ? 1 : nexus.hp > 0f ? 2 : 3;
        if (newStage != stage)
        {
            stage = newStage;
            Color c = nexus.team == MinionTeam.Blue ? new Color(0.22f,0.72f,1f) : new Color(1f,0.22f,0.30f);
            GameObject ring = AOGAbilityVisuals.CreateRing("Nexus_State_"+stage,transform.position+Vector3.up*0.05f,3.4f+stage*0.6f,c,0.10f);
            Destroy(ring,1.2f);
        }
        if (stage >= 1 && nexus.hp > 0f && Time.time >= nextPulse)
        {
            nextPulse = Time.time + (stage == 2 ? 0.8f : 1.5f);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time*8f)*0.018f*(stage+1));
        }
    }
}

public class AOGStructurePresentationBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGStructurePresentationBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Structure_Presentation_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGStructurePresentationBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 1.25f;

        foreach (AOGStrategicLaneSeal seal in AOGWorldRegistry.Seals)
            if (seal != null && seal.GetComponent<AOGSealWorldBar>() == null) seal.gameObject.AddComponent<AOGSealWorldBar>();

        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
            if (tower != null && tower.GetComponent<AOGTowerDamageStageRuntime>() == null) tower.gameObject.AddComponent<AOGTowerDamageStageRuntime>();

        foreach (AOGNexusCore nexus in AOGWorldRegistry.Nexuses)
            if (nexus != null && nexus.GetComponent<AOGNexusFinalStatePresentationRuntime>() == null) nexus.gameObject.AddComponent<AOGNexusFinalStatePresentationRuntime>();
    }
}
