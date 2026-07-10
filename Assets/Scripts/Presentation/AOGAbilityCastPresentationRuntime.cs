using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds cast anticipation and class-specific pre-release presentation for active champions.
/// Does not invoke abilities or apply damage; it only reacts to player key input for visual timing.
/// </summary>
[DefaultExecutionOrder(16010)]
public class AOGAbilityCastPresentationRuntime : MonoBehaviour
{
    private readonly HashSet<AOGActiveChampion> processed = new HashSet<AOGActiveChampion>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGAbilityCastPresentationRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Ability_Cast_Presentation_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGAbilityCastPresentationRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.75f;
        foreach (AOGActiveChampion champion in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (champion == null || processed.Contains(champion)) continue;
            if (champion.GetComponent<AOGChampionCastAnticipationRuntime>() == null)
                champion.gameObject.AddComponent<AOGChampionCastAnticipationRuntime>();
            processed.Add(champion);
        }
    }
}

public class AOGChampionCastAnticipationRuntime : MonoBehaviour
{
    private AOGActiveChampion champion;
    private AOGCharacterStats stats;
    private bool casting;

    private void Awake()
    {
        champion = GetComponent<AOGActiveChampion>();
        stats = GetComponent<AOGCharacterStats>();
    }

    private void Update()
    {
        if (champion == null || !champion.IsActiveChampion || casting)
            return;

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) StartCoroutine(CastCue(0));
        else if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) StartCoroutine(CastCue(1));
        else if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) StartCoroutine(CastCue(2));
        else if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) StartCoroutine(CastCue(3));
    }

    private IEnumerator CastCue(int slot)
    {
        casting = true;
        Color color = champion != null ? champion.accentColor : new Color(0.6f,0.35f,1f);
        string id = champion != null && champion.championId != null ? champion.championId.ToLowerInvariant() : string.Empty;
        float radius = slot == 3 ? 2.3f : slot == 1 ? 1.6f : 1.15f;
        float life = slot == 3 ? 0.52f : 0.28f;

        Vector3 origin = transform.position + Vector3.up * 0.08f;
        GameObject ground = AOGAbilityVisuals.CreateRing("Cast_Anticipation_" + slot, origin, radius, color, slot == 3 ? 0.13f : 0.07f);
        Destroy(ground, life + 0.12f);

        if (id.Contains("lyra")) BuildLyraCue(slot,color);
        else if (id.Contains("nyra")) BuildNyraCue(slot,color);
        else if (id.Contains("pyrelle")) BuildPyrelleCue(slot,color);
        else if (id.Contains("selene")) BuildSeleneCue(slot,color);
        else if (id.Contains("kaelith")) BuildKaelithCue(slot,color);
        else if (id.Contains("auron")) BuildAuronCue(slot,color);
        else BuildGenericCue(slot,color);

        Vector3 baseScale = transform.localScale;
        Quaternion baseRot = transform.localRotation;
        float duration = slot == 3 ? 0.26f : 0.14f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.Sin((t / duration) * Mathf.PI);
            transform.localScale = baseScale * (1f + 0.025f * k);
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, (slot == 3 ? 2.2f : 1.0f) * k);
            yield return null;
        }
        transform.localScale = baseScale;
        transform.localRotation = baseRot;
        casting = false;
    }

    private void BuildLyraCue(int slot, Color color)
    {
        Slash(transform.position + transform.forward * 1.1f + Vector3.up * 1.0f, Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0f,0f,38f), new Vector3(0.08f, slot==3?2.2f:1.25f,0.08f), color, slot==3?0.45f:0.22f);
    }

    private void BuildNyraCue(int slot, Color color)
    {
        int count = slot == 3 ? 7 : 3;
        for (int i=0;i<count;i++)
        {
            float a = i*Mathf.PI*2f/count;
            Orb(transform.position + new Vector3(Mathf.Cos(a)*0.85f,1.0f+0.18f*i,Mathf.Sin(a)*0.85f),0.13f,Color.Lerp(color,Color.white,0.25f),0.42f);
        }
    }

    private void BuildPyrelleCue(int slot, Color color)
    {
        int count = slot == 3 ? 10 : 5;
        for (int i=0;i<count;i++)
        {
            float a = i*Mathf.PI*2f/count;
            Slash(transform.position + new Vector3(Mathf.Cos(a)*0.65f,0.8f,Mathf.Sin(a)*0.65f), Quaternion.Euler(0f,a*Mathf.Rad2Deg,Random.Range(-25f,25f)), new Vector3(0.08f,0.7f,0.08f), new Color(1f,0.26f,0.03f),0.34f);
        }
    }

    private void BuildSeleneCue(int slot, Color color)
    {
        int count = slot == 3 ? 8 : 5;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2f/count;
            Orb(transform.position+new Vector3(Mathf.Cos(a)*1.1f,1.35f,Mathf.Sin(a)*1.1f),0.10f,Color.Lerp(color,Color.white,0.45f),0.48f);
        }
    }

    private void BuildKaelithCue(int slot, Color color)
    {
        Slash(transform.position + transform.forward * 0.8f + Vector3.up*1.0f, Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0f,0f,45f), new Vector3(0.13f,slot==3?2.2f:1.35f,0.13f), color,0.32f);
        Slash(transform.position + transform.forward * 0.8f + Vector3.up*1.0f, Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0f,0f,-45f), new Vector3(0.10f,slot==3?1.7f:1.0f,0.10f), Color.Lerp(color,Color.black,0.25f),0.32f);
    }

    private void BuildAuronCue(int slot, Color color)
    {
        GameObject ring = AOGAbilityVisuals.CreateRing("Auron_Solar_Cast_Cue", transform.position + Vector3.up*0.10f, slot==3?2.8f:1.5f, Color.Lerp(color,Color.white,0.35f), slot==3?0.17f:0.09f);
        Destroy(ring, slot==3?0.55f:0.30f);
        Orb(transform.position+Vector3.up*1.6f,slot==3?0.34f:0.22f,Color.Lerp(color,Color.white,0.45f),0.42f);
    }

    private void BuildGenericCue(int slot, Color color)
    {
        Orb(transform.position + Vector3.up*1.4f, slot==3?0.28f:0.18f, color,0.32f);
    }

    private static void Slash(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float life)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Cast_Cue_Slash";
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Emissive(color, 3.8f);
        Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
        Destroy(go, life);
    }

    private static void Orb(Vector3 position, float size, Color color, float life)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Cast_Cue_Orb";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<Renderer>().sharedMaterial = Emissive(color, 4.2f);
        Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
        Destroy(go, life);
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
