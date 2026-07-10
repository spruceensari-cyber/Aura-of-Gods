using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps minions visually secondary to champions and differentiates role silhouettes.
/// Melee minion sword pieces are hidden and replaced by short forearm impact gauntlets.
/// </summary>
[DefaultExecutionOrder(16050)]
public class AOGMinionSecondaryUnitRefinementRuntime : MonoBehaviour
{
    private readonly HashSet<Minion> processed = new HashSet<Minion>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGMinionSecondaryUnitRefinementRuntime>() != null) return;
        GameObject host=new GameObject("AOG_Minion_Secondary_Unit_Refinement"); DontDestroyOnLoad(host); host.AddComponent<AOGMinionSecondaryUnitRefinementRuntime>();
    }

    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+0.75f;
        foreach(Minion minion in Minion.Active)
        {
            if(minion==null||processed.Contains(minion))continue;
            Refine(minion); processed.Add(minion);
        }
    }

    private static void Refine(Minion minion)
    {
        Transform visual=minion.transform.Find("AOG_Minon_Visual");
        if(visual==null)return;

        visual.localScale = minion.role == MinionRole.Cannon ? Vector3.one*0.82f : minion.role == MinionRole.Ranged ? Vector3.one*0.72f : Vector3.one*0.74f;

        if(minion.role==MinionRole.Melee)
        {
            Transform weapon=FindDeep(visual,"Weapon");
            if(weapon!=null)weapon.gameObject.SetActive(false);

            Transform right=FindDeep(visual,"Arm_R");
            if(right!=null)
            {
                Color team=minion.team==MinionTeam.Blue?new Color(0.12f,0.48f,1f):new Color(1f,0.16f,0.22f);
                GameObject gauntlet=GameObject.CreatePrimitive(PrimitiveType.Cube);
                gauntlet.name="Impact_Gauntlet"; gauntlet.transform.SetParent(right,false); gauntlet.transform.localPosition=new Vector3(0f,-0.42f,0.18f); gauntlet.transform.localScale=new Vector3(0.24f,0.36f,0.28f); gauntlet.GetComponent<Renderer>().sharedMaterial=Emissive(team,2.2f); Collider c=gauntlet.GetComponent<Collider>(); if(c!=null)Object.Destroy(c);
            }
        }
        else if(minion.role==MinionRole.Ranged)
        {
            Transform weapon=FindDeep(visual,"Weapon");
            if(weapon!=null)weapon.localScale*=0.72f;
            Transform body=FindDeep(visual,"Body");
            if(body!=null)body.localScale=new Vector3(body.localScale.x*0.88f,body.localScale.y*1.06f,body.localScale.z*0.88f);
        }
        else
        {
            Transform body=FindDeep(visual,"Body");
            if(body!=null)body.localScale*=1.08f;
        }
    }

    private static Transform FindDeep(Transform root,string name)
    {
        foreach(Transform child in root.GetComponentsInChildren<Transform>(true))if(child.name==name)return child;
        return null;
    }

    private static Material Emissive(Color color,float strength)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null)shader=Shader.Find("Standard"); Material mat=new Material(shader){color=color,enableInstancing=true}; if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);} return mat;
    }
}
