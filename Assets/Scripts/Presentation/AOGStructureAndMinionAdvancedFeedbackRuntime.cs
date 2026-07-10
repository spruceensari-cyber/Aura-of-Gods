using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(16080)]
public class AOGStructureAndMinionAdvancedFeedbackRuntime : MonoBehaviour
{
    private readonly Dictionary<TowerHealth,float> towerHp = new Dictionary<TowerHealth,float>();
    private readonly Dictionary<AOGNexusCore,float> nexusHp = new Dictionary<AOGNexusCore,float>();
    private readonly Dictionary<AOGStrategicLaneSeal,float> sealHp = new Dictionary<AOGStrategicLaneSeal,float>();
    private readonly Dictionary<Minion,float> minionHp = new Dictionary<Minion,float>();
    private readonly HashSet<Minion> dyingMinions = new HashSet<Minion>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGStructureAndMinionAdvancedFeedbackRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Structure_And_Minion_Advanced_Feedback");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGStructureAndMinionAdvancedFeedbackRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.12f;
        WatchTowers();
        WatchNexus();
        WatchSeals();
        WatchMinions();
    }

    private void WatchTowers()
    {
        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tower == null) continue;
            if (!towerHp.TryGetValue(tower, out float previous))
            {
                towerHp[tower] = tower.hp;
                continue;
            }
            if (tower.hp < previous - 0.1f)
                StartCoroutine(StructureHitPulse(tower.transform, tower.towerTeam == MinionTeam.Blue ? new Color(0.22f,0.66f,1f) : new Color(1f,0.18f,0.22f), Mathf.Clamp((previous-tower.hp)/120f,0.8f,2.6f)));
            towerHp[tower] = tower.hp;
        }
    }

    private void WatchNexus()
    {
        foreach (AOGNexusCore nexus in FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (nexus == null) continue;
            if (!nexusHp.TryGetValue(nexus, out float previous))
            {
                nexusHp[nexus] = nexus.hp;
                continue;
            }
            if (nexus.hp < previous - 0.1f)
                StartCoroutine(StructureHitPulse(nexus.transform, nexus.team == MinionTeam.Blue ? new Color(0.22f,0.76f,1f) : new Color(1f,0.18f,0.28f), Mathf.Clamp((previous-nexus.hp)/160f,1.0f,3.2f)));
            nexusHp[nexus] = nexus.hp;
        }
    }

    private void WatchSeals()
    {
        foreach (AOGStrategicLaneSeal seal in FindObjectsByType<AOGStrategicLaneSeal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (seal == null) continue;
            if (!sealHp.TryGetValue(seal, out float previous))
            {
                sealHp[seal] = seal.hp;
                continue;
            }
            if (seal.hp < previous - 0.1f)
                StartCoroutine(StructureHitPulse(seal.transform, seal.team == MinionTeam.Blue ? new Color(0.26f,0.72f,1f) : new Color(1f,0.22f,0.30f), Mathf.Clamp((previous-seal.hp)/110f,0.9f,2.8f)));
            sealHp[seal] = seal.hp;
        }
    }

    private void WatchMinions()
    {
        foreach (Minion minion in Minion.Active)
        {
            if (minion == null) continue;
            if (!minionHp.TryGetValue(minion, out float previous))
            {
                minionHp[minion] = minion.hp;
                continue;
            }

            if (minion.hp < previous - 0.1f && minion.hp > 0f)
                StartCoroutine(MinionHitPulse(minion.transform, minion.team == MinionTeam.Blue ? new Color(0.22f,0.62f,1f) : new Color(1f,0.20f,0.26f)));

            if (minion.hp <= 0f && previous > 0f && !dyingMinions.Contains(minion))
            {
                dyingMinions.Add(minion);
                StartCoroutine(MinionCollapse(minion.gameObject, minion.team == MinionTeam.Blue ? new Color(0.22f,0.62f,1f) : new Color(1f,0.20f,0.26f)));
            }

            minionHp[minion] = minion.hp;
        }
    }

    private IEnumerator StructureHitPulse(Transform target, Color color, float power)
    {
        if (target == null) yield break;
        Vector3 baseScale = target.localScale;
        GameObject ring = AOGAbilityVisuals.CreateRing("Advanced_Structure_Hit", target.position + Vector3.up * 0.07f, 1.8f * power, color, 0.09f);
        Destroy(ring, 0.45f);
        for (float t = 0f; t < 0.18f && target != null; t += Time.deltaTime)
        {
            float k = Mathf.Sin((t / 0.18f) * Mathf.PI);
            target.localScale = baseScale * (1f + 0.018f * power * k);
            yield return null;
        }
        if (target != null) target.localScale = baseScale;
    }

    private IEnumerator MinionHitPulse(Transform target, Color color)
    {
        if (target == null) yield break;
        Vector3 baseScale = target.localScale;
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "Minion_Hit_Flash";
        flash.transform.position = target.position + Vector3.up * 0.75f;
        flash.transform.localScale = Vector3.one * 0.32f;
        flash.GetComponent<Renderer>().sharedMaterial = Emissive(Color.Lerp(color, Color.white, 0.55f), 3.8f);
        Collider c = flash.GetComponent<Collider>(); if (c != null) Destroy(c);
        Destroy(flash, 0.16f);

        for (float t = 0f; t < 0.10f && target != null; t += Time.deltaTime)
        {
            float k = Mathf.Sin((t / 0.10f) * Mathf.PI);
            target.localScale = baseScale * (1f + 0.07f * k);
            yield return null;
        }
        if (target != null) target.localScale = baseScale;
    }

    private IEnumerator MinionCollapse(GameObject minion, Color color)
    {
        if (minion == null) yield break;
        Transform target = minion.transform;
        Renderer[] renderers = minion.GetComponentsInChildren<Renderer>(true);
        Vector3 baseScale = target.localScale;
        GameObject ring = AOGAbilityVisuals.CreateRing("Minion_Collapse_Ring", target.position + Vector3.up * 0.04f, 0.95f, color, 0.055f);
        Destroy(ring, 0.32f);

        for (float t = 0f; t < 0.36f && target != null; t += Time.deltaTime)
        {
            float k = Mathf.Clamp01(t / 0.36f);
            target.localScale = Vector3.Lerp(baseScale, new Vector3(baseScale.x * 0.82f, baseScale.y * 0.12f, baseScale.z * 0.82f), k);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (Material mat in renderer.materials)
                {
                    if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * Mathf.Lerp(2.2f, 0f, k));
                }
            }
            yield return null;
        }
    }

    private static Material Emissive(Color color, float strength)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * strength);
        }
        return mat;
    }
}
