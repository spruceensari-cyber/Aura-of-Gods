using UnityEngine;

[DefaultExecutionOrder(120)]
public class AOGMinionIdentityPolishRuntime : MonoBehaviour
{
    private bool applied;

    private void Start()
    {
        Apply();
    }

    private void Apply()
    {
        if (applied) return;
        Minion minion = GetComponent<Minion>();
        if (minion == null) return;

        Transform visual = transform.Find("AOG_Minon_Visual");
        if (visual == null) return;

        applied = true;
        visual.localScale = minion.role == MinionRole.Cannon ? Vector3.one * 0.78f : Vector3.one * 0.68f;

        if (minion.role == MinionRole.Melee)
        {
            Transform weapon = FindRecursive(visual, "Weapon");
            if (weapon != null)
            {
                Renderer renderer = weapon.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
            }
            BuildMeleeClaws(visual, minion.team);
        }
        else if (minion.role == MinionRole.Ranged)
        {
            Transform weapon = FindRecursive(visual, "Weapon");
            if (weapon != null)
                weapon.localScale *= 0.72f;
            BuildFloatingOrb(visual, minion.team);
        }
        else
        {
            Transform weapon = FindRecursive(visual, "Weapon");
            if (weapon != null)
                weapon.localScale = Vector3.Scale(weapon.localScale, new Vector3(1.35f, 1.35f, 1.35f));
        }
    }

    private static void BuildMeleeClaws(Transform visual, MinionTeam team)
    {
        Color c = team == MinionTeam.Blue ? new Color(0.12f,0.52f,1f) : new Color(1f,0.14f,0.20f);
        Material mat = BuildMaterial(c);
        Transform rightArm = FindRecursive(visual, "Arm_R");
        if (rightArm == null) return;

        for (int i = -1; i <= 1; i++)
        {
            GameObject claw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            claw.name = "Energy_Claw_" + i;
            claw.transform.SetParent(rightArm, false);
            claw.transform.localPosition = new Vector3(i * 0.09f, -0.50f, 0.28f);
            claw.transform.localScale = new Vector3(0.045f, 0.34f, 0.055f);
            claw.transform.localRotation = Quaternion.Euler(18f, 0f, i * 10f);
            claw.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(claw.GetComponent<Collider>());
        }
    }

    private static void BuildFloatingOrb(Transform visual, MinionTeam team)
    {
        Color c = team == MinionTeam.Blue ? new Color(0.16f,0.62f,1f) : new Color(1f,0.18f,0.26f);
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Ranged_Orb";
        orb.transform.SetParent(visual, false);
        orb.transform.localPosition = new Vector3(0f, 2.25f, 0.18f);
        orb.transform.localScale = Vector3.one * 0.22f;
        orb.GetComponent<Renderer>().sharedMaterial = BuildMaterial(c);
        Object.Destroy(orb.GetComponent<Collider>());
        AOGOrbitAnimator orbit = orb.AddComponent<AOGOrbitAnimator>();
        orbit.speed = team == MinionTeam.Blue ? 36f : -36f;
    }

    private static Material BuildMaterial(Color c)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { color = c };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * 4f);
        }
        return mat;
    }

    private static Transform FindRecursive(Transform root, string exact)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == exact) return t;
        return null;
    }
}

public class AOGMinionIdentityPolishBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Minion_Identity_Polish_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMinionIdentityPolishBootstrap>();
    }

    private void Update()
    {
        foreach (Minion minion in Minion.Active)
        {
            if (minion != null && minion.GetComponent<AOGMinionIdentityPolishRuntime>() == null)
                minion.gameObject.AddComponent<AOGMinionIdentityPolishRuntime>();
        }
    }
}
