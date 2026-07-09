using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(15800)]
public class AOGPremiumCharacterAndBossLookRuntime : MonoBehaviour
{
    private readonly Dictionary<Material, Material> materialCache = new Dictionary<Material, Material>();
    private float nextPass;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Premium_Character_And_Boss_Look");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPremiumCharacterAndBossLookRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextPass)
            return;

        nextPass = Time.unscaledTime + 2f;
        PolishChampions();
        PolishBosses();
    }

    private void PolishChampions()
    {
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero == null)
                continue;

            AOGActiveChampion active = hero.GetComponent<AOGActiveChampion>();
            Color accent = active != null ? active.accentColor : new Color(0.42f, 0.32f, 1f);
            AddRimLight(hero.transform, "AOG_Hero_Rim", accent, 1.0f, 5.5f, new Vector3(-1.2f, 2.8f, -1.0f));
            UpgradeRenderers(hero.GetComponentsInChildren<Renderer>(true), accent, 0.12f);
        }
    }

    private void PolishBosses()
    {
        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (boss == null)
                continue;

            Color accent = boss.bossType == AOGNeutralBossType.Dragon
                ? new Color(1f, 0.18f, 0.035f)
                : new Color(0.58f, 0.18f, 0.92f);

            AddRimLight(boss.transform, "AOG_Boss_Key_Light", accent, boss.bossType == AOGNeutralBossType.Dragon ? 1.8f : 1.35f, boss.bossType == AOGNeutralBossType.Dragon ? 9f : 7f, new Vector3(-2.5f, 5f, -2f));
            UpgradeRenderers(boss.GetComponentsInChildren<Renderer>(true), accent, 0.20f);
        }
    }

    private void UpgradeRenderers(Renderer[] renderers, Color accent, float emissionMix)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            Material[] source = renderer.sharedMaterials;
            Material[] upgraded = new Material[source.Length];
            bool changed = false;

            for (int i = 0; i < source.Length; i++)
            {
                Material original = source[i];
                if (original == null)
                {
                    upgraded[i] = null;
                    continue;
                }

                if (!materialCache.TryGetValue(original, out Material copy) || copy == null)
                {
                    copy = new Material(original) { name = original.name + "_PremiumLook" };
                    if (copy.HasProperty("_Smoothness"))
                        copy.SetFloat("_Smoothness", Mathf.Clamp01(copy.GetFloat("_Smoothness") + 0.10f));
                    if (copy.HasProperty("_Metallic"))
                        copy.SetFloat("_Metallic", Mathf.Clamp01(copy.GetFloat("_Metallic") + 0.06f));
                    if (copy.HasProperty("_EmissionColor"))
                    {
                        Color baseEmission = copy.GetColor("_EmissionColor");
                        copy.EnableKeyword("_EMISSION");
                        copy.SetColor("_EmissionColor", Color.Lerp(baseEmission, accent * 1.8f, emissionMix));
                    }
                    materialCache[original] = copy;
                }

                upgraded[i] = copy;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = upgraded;

            renderer.receiveShadows = true;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    private static void AddRimLight(Transform root, string name, Color color, float intensity, float range, Vector3 localPosition)
    {
        Transform existing = root.Find(name);
        if (existing != null)
            return;

        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(root, false);
        lightObject.transform.localPosition = localPosition;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }
}
