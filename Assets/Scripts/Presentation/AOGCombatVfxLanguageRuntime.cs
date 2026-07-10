using UnityEngine;

/// <summary>
/// Converts unified combat hit events into restrained champion-specific impact language.
/// This complements existing ability visuals without replacing skill logic.
/// </summary>
public class AOGCombatVfxLanguageRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCombatVfxLanguageRuntime>() != null) return;
        GameObject host=new GameObject("AOG_Combat_VFX_Language_Runtime"); DontDestroyOnLoad(host); host.AddComponent<AOGCombatVfxLanguageRuntime>();
    }

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit += OnBasicHit;
        AOGCombatEvents.AbilityHit += OnAbilityHit;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit -= OnBasicHit;
        AOGCombatEvents.AbilityHit -= OnAbilityHit;
    }

    private void OnBasicHit(AOGCombatHitEvent data)
    {
        if(data.source==null||data.target==null)return;
        AOGActiveChampion champion=data.source.GetComponentInParent<AOGActiveChampion>();
        if(champion==null)return;
        SpawnIdentityImpact(champion,data.target.transform.position+Vector3.up*0.55f,false,data.damage);
    }

    private void OnAbilityHit(AOGCombatHitEvent data)
    {
        if(data.source==null||data.target==null)return;
        AOGActiveChampion champion=data.source.GetComponentInParent<AOGActiveChampion>();
        if(champion==null)return;
        SpawnIdentityImpact(champion,data.target.transform.position+Vector3.up*0.65f,true,data.damage);
    }

    private static void SpawnIdentityImpact(AOGActiveChampion champion,Vector3 point,bool ability,float damage)
    {
        string id=champion.championId!=null?champion.championId.ToLowerInvariant():string.Empty;
        Color accent=champion.accentColor;
        float scale=Mathf.Clamp(0.9f+damage/220f,0.9f,2.6f);

        if(id.Contains("lyra"))
        {
            Ring(point,1.05f*scale,accent,0.07f,0.32f);
            Slash(point,Quaternion.Euler(20f,35f,55f),new Vector3(0.10f,1.35f*scale,0.08f),accent,0.26f);
        }
        else if(id.Contains("nyra"))
        {
            for(int i=0;i<3;i++)
            {
                float a=i*Mathf.PI*2f/3f;
                Orb(point+new Vector3(Mathf.Cos(a)*0.55f,0.2f+0.15f*i,Mathf.Sin(a)*0.55f),0.16f*scale,Color.Lerp(accent,Color.white,0.25f),0.38f);
            }
        }
        else if(id.Contains("pyrelle"))
        {
            Ring(point,1.2f*scale,new Color(1f,0.20f,0.02f),0.13f,0.42f);
            for(int i=0;i<4;i++) Slash(point,Quaternion.Euler(Random.Range(-15f,35f),i*90f,Random.Range(-25f,25f)),new Vector3(0.11f,0.95f*scale,0.11f),new Color(1f,0.42f,0.04f),0.34f);
        }
        else if(id.Contains("selene"))
        {
            Ring(point,1.35f*scale,accent,0.055f,0.45f);
            for(int i=0;i<4;i++) Slash(point,Quaternion.Euler(0f,i*45f,45f),new Vector3(0.055f,1.1f*scale,0.055f),Color.Lerp(accent,Color.white,0.45f),0.42f);
        }
        else if(id.Contains("kaelith"))
        {
            Ring(point,1.1f*scale,accent,0.10f,0.30f);
            Slash(point,Quaternion.Euler(0f,0f,45f),new Vector3(0.14f,1.5f*scale,0.14f),accent,0.28f);
            Slash(point,Quaternion.Euler(0f,0f,-45f),new Vector3(0.10f,1.1f*scale,0.10f),Color.Lerp(accent,Color.black,0.25f),0.28f);
        }
        else if(id.Contains("auron"))
        {
            Ring(point,1.45f*scale,Color.Lerp(accent,Color.white,0.40f),0.16f,0.45f);
            Orb(point,0.34f*scale,Color.Lerp(accent,Color.white,0.55f),0.40f);
        }
        else
        {
            Ring(point,0.95f*scale,accent,0.06f,ability?0.38f:0.25f);
        }
    }

    private static void Ring(Vector3 point,float radius,Color color,float width,float life)
    {
        GameObject ring=AOGAbilityVisuals.CreateRing("Identity_Impact_Ring",point,radius,color,width); Object.Destroy(ring,life);
    }

    private static void Slash(Vector3 point,Quaternion rotation,Vector3 scale,Color color,float life)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name="Identity_Impact_Slash"; go.transform.position=point; go.transform.rotation=rotation; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=Emissive(color,4f); Collider c=go.GetComponent<Collider>(); if(c!=null)Object.Destroy(c); Object.Destroy(go,life);
    }

    private static void Orb(Vector3 point,float size,Color color,float life)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name="Identity_Impact_Orb"; go.transform.position=point; go.transform.localScale=Vector3.one*size; go.GetComponent<Renderer>().sharedMaterial=Emissive(color,4.5f); Collider c=go.GetComponent<Collider>(); if(c!=null)Object.Destroy(c); Object.Destroy(go,life);
    }

    private static Material Emissive(Color color,float strength)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null)shader=Shader.Find("Standard"); Material mat=new Material(shader){color=color}; if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);} return mat;
    }
}
