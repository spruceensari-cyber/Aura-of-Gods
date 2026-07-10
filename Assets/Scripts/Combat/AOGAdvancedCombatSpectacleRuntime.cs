using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Local-only combat freeze for champion impacts. This never changes Time.timeScale and
/// therefore cannot stall the match, AI, projectiles or networking-style timers.
/// </summary>
public class AOGLocalHitStopRuntime : MonoBehaviour
{
    private Coroutine freezeRoutine;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
    }

    public void Pulse(float duration)
    {
        if (duration <= 0f)
            return;

        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);
        freezeRoutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        float originalSpeed = animator != null ? animator.speed : 1f;
        if (animator != null)
            animator.speed = 0f;

        float until = Time.unscaledTime + duration;
        while (Time.unscaledTime < until)
            yield return null;

        if (animator != null)
            animator.speed = originalSpeed <= 0f ? 1f : originalSpeed;
        freezeRoutine = null;
    }
}

/// <summary>
/// Short pre-cast language used by ChampionPresentationController. Presentation-only.
/// </summary>
public class AOGAbilityCastAnticipationRuntime : MonoBehaviour
{
    private AOGActiveChampion active;

    private void Awake()
    {
        active = GetComponent<AOGActiveChampion>();
    }

    public void PlayAnticipation(int slot)
    {
        if (active == null)
            active = GetComponent<AOGActiveChampion>();

        Color accent = active != null ? active.accentColor : new Color(0.42f,0.62f,1f);
        string id = active != null && !string.IsNullOrEmpty(active.championId)
            ? active.championId.ToLowerInvariant()
            : string.Empty;

        float radius = slot == 3 ? 2.1f : 1.15f + slot * 0.14f;
        float width = slot == 3 ? 0.12f : 0.055f;
        GameObject ring = AOGAbilityVisuals.CreateRing(
            "Cast_Anticipation_" + slot,
            transform.position + Vector3.up * 0.06f,
            radius,
            accent,
            width);
        Destroy(ring, slot == 3 ? 0.58f : 0.28f);

        Vector3 focus = transform.position + Vector3.up * 1.55f + transform.forward * 0.38f;
        if (id.Contains("pyrelle"))
            SpawnFocusOrb(focus, 0.28f, new Color(1f,0.24f,0.03f), 0.34f);
        else if (id.Contains("selene"))
            SpawnStarCross(focus, accent, 0.34f);
        else if (id.Contains("nyra"))
            SpawnSpiritPair(focus, accent, 0.36f);
        else if (id.Contains("kaelith"))
            SpawnFractureCross(focus, accent, 0.30f);
        else if (id.Contains("auron"))
            SpawnFocusOrb(focus, 0.36f, Color.Lerp(accent,Color.white,0.45f), 0.38f);
        else
            SpawnFocusOrb(focus, 0.18f + slot * 0.035f, accent, 0.28f);
    }

    private static void SpawnFocusOrb(Vector3 position,float size,Color color,float life)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Cast_Focus_Orb";
        orb.transform.position = position;
        orb.transform.localScale = Vector3.one * size;
        Renderer renderer = orb.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = Emissive(color,4.2f);
        Collider collider = orb.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Destroy(orb,life);
    }

    private static void SpawnStarCross(Vector3 position,Color color,float life)
    {
        for (int i=0;i<4;i++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Astral_Cast_Line_" + i;
            line.transform.position = position;
            line.transform.rotation = Quaternion.Euler(0f,i*45f,45f);
            line.transform.localScale = new Vector3(0.045f,0.72f,0.045f);
            line.GetComponent<Renderer>().sharedMaterial = Emissive(Color.Lerp(color,Color.white,0.35f),3.8f);
            Collider c = line.GetComponent<Collider>(); if (c != null) Destroy(c);
            Destroy(line,life);
        }
    }

    private static void SpawnSpiritPair(Vector3 position,Color color,float life)
    {
        SpawnFocusOrb(position+new Vector3(-0.28f,0.10f,0f),0.16f,color,life);
        SpawnFocusOrb(position+new Vector3(0.28f,-0.05f,0.05f),0.13f,Color.Lerp(color,Color.white,0.45f),life);
    }

    private static void SpawnFractureCross(Vector3 position,Color color,float life)
    {
        for (int i=0;i<2;i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "Eclipse_Cast_Fracture";
            shard.transform.position = position;
            shard.transform.rotation = Quaternion.Euler(0f,0f,i==0?48f:-48f);
            shard.transform.localScale = new Vector3(0.08f,0.85f,0.10f);
            shard.GetComponent<Renderer>().sharedMaterial = Emissive(color,3.6f);
            Collider c = shard.GetComponent<Collider>(); if (c != null) Destroy(c);
            Destroy(shard,life);
        }
    }

    private static Material Emissive(Color color,float strength)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color=color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",color*strength);
        }
        return material;
    }
}

/// <summary>
/// Event-driven combat spectacle: local hit-stop on champion impacts and a readable
/// death dissolve/fragment presentation during the existing respawn presentation window.
/// </summary>
public class AOGAdvancedCombatSpectacleRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGAdvancedCombatSpectacleRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Advanced_Combat_Spectacle_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGAdvancedCombatSpectacleRuntime>();
    }

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit += OnBasicHit;
        AOGCombatEvents.AbilityHit += OnAbilityHit;
        AOGCombatEvents.ChampionDeath += OnChampionDeath;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit -= OnBasicHit;
        AOGCombatEvents.AbilityHit -= OnAbilityHit;
        AOGCombatEvents.ChampionDeath -= OnChampionDeath;
    }

    private void OnBasicHit(AOGCombatHitEvent hit)
    {
        if (hit.source == null || hit.target == null)
            return;

        if (hit.targetKind != AOGCombatTargetKind.Champion)
            return;

        ApplyLocalHitStop(hit.source,0.032f);
        ApplyLocalHitStop(hit.target,0.047f);
        SpawnImpactBurst(hit.target.transform.position+Vector3.up*0.65f,ResolveAccent(hit.source),0.72f,0.24f);
    }

    private void OnAbilityHit(AOGCombatHitEvent hit)
    {
        if (hit.source == null || hit.target == null)
            return;

        if (hit.targetKind != AOGCombatTargetKind.Champion)
            return;

        float strength = Mathf.Clamp(hit.damage/220f,0.55f,1.35f);
        ApplyLocalHitStop(hit.source,0.028f+strength*0.018f);
        ApplyLocalHitStop(hit.target,0.052f+strength*0.022f);
        SpawnImpactBurst(hit.target.transform.position+Vector3.up*0.72f,ResolveAccent(hit.source),0.92f+strength*0.35f,0.28f);
    }

    private void OnChampionDeath(AOGChampionDeathEvent death)
    {
        if (death.victim == null)
            return;
        StartCoroutine(ChampionDeathPresentation(death.victim));
    }

    private IEnumerator ChampionDeathPresentation(AOGCharacterStats victim)
    {
        if (victim == null)
            yield break;

        Renderer[] renderers = victim.GetComponentsInChildren<Renderer>(true);
        List<Material> materials = new List<Material>();
        List<Color> originalColors = new List<Color>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.gameObject.name.ToLowerInvariant().Contains("hp_bar"))
                continue;

            foreach (Material material in renderer.materials)
            {
                if (material == null) continue;
                materials.Add(material);
                originalColors.Add(material.color);
            }
        }

        Color accent = ResolveAccent(victim.gameObject);
        Vector3 origin = victim.transform.position + Vector3.up*0.65f;
        for (int i=0;i<6;i++)
        {
            float angle = i*Mathf.PI*2f/6f;
            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragment.name = "Champion_Death_Fragment_"+i;
            fragment.transform.position = origin + new Vector3(Mathf.Cos(angle)*0.55f,0.15f*i,Mathf.Sin(angle)*0.55f);
            fragment.transform.localScale = new Vector3(0.08f,0.34f,0.08f);
            fragment.transform.rotation = Quaternion.Euler(i*17f,angle*Mathf.Rad2Deg,i*23f);
            fragment.GetComponent<Renderer>().sharedMaterial = Emissive(accent,3.2f);
            Collider c = fragment.GetComponent<Collider>(); if (c != null) Destroy(c);
            fragment.AddComponent<AOGDeathFragmentMotionRuntime>().velocity = new Vector3(Mathf.Cos(angle)*0.7f,1.3f+0.16f*i,Mathf.Sin(angle)*0.7f);
            Destroy(fragment,1.25f);
        }

        float duration = Mathf.Min(1.25f,Mathf.Max(0.55f,victim.deathPresentationDuration*0.82f));
        float elapsed = 0f;
        while (elapsed < duration && victim != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed/duration);
            for (int i=0;i<materials.Count;i++)
            {
                Material material = materials[i];
                if (material == null) continue;
                Color original = originalColors[i];
                Color target = Color.Lerp(original,Color.Lerp(accent,Color.black,0.45f),t);
                material.color = target;
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor",Color.Lerp(accent*1.8f,Color.black,t));
            }
            yield return null;
        }
    }

    private static void ApplyLocalHitStop(GameObject target,float duration)
    {
        if (target == null) return;
        AOGCharacterStats stats = target.GetComponentInParent<AOGCharacterStats>();
        GameObject root = stats != null ? stats.gameObject : target;
        AOGLocalHitStopRuntime stop = root.GetComponent<AOGLocalHitStopRuntime>();
        if (stop == null) stop = root.AddComponent<AOGLocalHitStopRuntime>();
        stop.Pulse(duration);
    }

    private static void SpawnImpactBurst(Vector3 point,Color color,float radius,float life)
    {
        GameObject ring = AOGAbilityVisuals.CreateRing("Advanced_Impact_Shock",point,radius,color,0.08f);
        Destroy(ring,life);

        for (int i=0;i<3;i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "Advanced_Impact_Shard";
            shard.transform.position = point;
            shard.transform.rotation = Quaternion.Euler(Random.Range(-35f,35f),i*120f+Random.Range(-15f,15f),Random.Range(-55f,55f));
            shard.transform.localScale = new Vector3(0.045f,0.55f+0.18f*i,0.045f);
            shard.GetComponent<Renderer>().sharedMaterial = Emissive(color,4f);
            Collider c = shard.GetComponent<Collider>(); if (c != null) Destroy(c);
            Destroy(shard,life*0.85f);
        }
    }

    private static Color ResolveAccent(GameObject source)
    {
        if (source == null) return new Color(0.42f,0.62f,1f);
        AOGActiveChampion champion = source.GetComponentInParent<AOGActiveChampion>();
        return champion != null ? champion.accentColor : new Color(0.42f,0.62f,1f);
    }

    private static Material Emissive(Color color,float strength)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color=color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",color*strength);
        }
        return material;
    }
}

public class AOGDeathFragmentMotionRuntime : MonoBehaviour
{
    public Vector3 velocity = Vector3.up;
    private void Update()
    {
        velocity += Vector3.down*1.6f*Time.deltaTime;
        transform.position += velocity*Time.deltaTime;
        transform.Rotate(new Vector3(70f,95f,55f)*Time.deltaTime,Space.Self);
    }
}
