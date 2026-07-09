using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-650)]
public class AOGJungleCampRuntime : MonoBehaviour
{
    private bool built;
    private readonly List<Minion> monsters = new List<Minion>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Jungle_Camp_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGJungleCampRuntime>();
    }

    private void Update()
    {
        if (built) return;
        if (AOGMatchDirector.Instance == null) return;
        built = true;
        BuildCamp(new Vector3(-38f,0.2f,26f), MinionTeam.Red, "Ashfang Camp", new Color(1f,0.22f,0.06f));
        BuildCamp(new Vector3(38f,0.2f,-26f), MinionTeam.Blue, "Frostclaw Camp", new Color(0.12f,0.62f,1f));
        BuildCamp(new Vector3(-18f,0.2f,-34f), MinionTeam.Red, "Voidling Camp", new Color(0.54f,0.16f,0.92f));
        BuildCamp(new Vector3(18f,0.2f,34f), MinionTeam.Blue, "Aetherling Camp", new Color(0.18f,0.86f,0.92f));
    }

    private void BuildCamp(Vector3 center, MinionTeam team, string campName, Color accent)
    {
        GameObject root = new GameObject(campName);
        root.transform.position = center;

        for (int i = 0; i < 3; i++)
        {
            GameObject go = new GameObject(campName + "_Monster_" + i);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3((i - 1) * 1.6f, 0f, i == 1 ? 0.8f : 0f);

            Minion monster = go.AddComponent<Minion>();
            monster.team = team;
            monster.role = i == 1 ? MinionRole.Cannon : MinionRole.Melee;
            monster.path = new Vector3[0];
            monster.maxHp = i == 1 ? 900f : 420f;
            monster.hp = monster.maxHp;
            monster.damage = i == 1 ? 42f : 24f;
            monster.speed = 2.7f;
            monster.aggroRange = 7f;
            monster.attackRange = i == 1 ? 4.8f : 2.2f;
            monster.attackRate = i == 1 ? 1.5f : 1.05f;
            monsters.Add(monster);

            AOGMinionVisualFactory.Build(monster);
            AddAura(go.transform, accent, i == 1 ? 1.3f : 0.8f);
        }

        GameObject ring = AOGAbilityVisuals.CreateRing(campName + "_Arena", center + Vector3.up * 0.05f, 5.2f, accent, 0.08f);
        ring.transform.SetParent(root.transform, true);
    }

    private static void AddAura(Transform target, Color color, float intensity)
    {
        GameObject lightObject = new GameObject("Camp_Aura");
        lightObject.transform.SetParent(target, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = 5f;
        light.shadows = LightShadows.None;
    }
}
