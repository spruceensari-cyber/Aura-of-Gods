using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds combat events to the central audio director without coupling gameplay scripts to clip assets.
/// </summary>
public class AOGCombatAudioBinderRuntime : MonoBehaviour
{
    private readonly HashSet<Champion> boundChampions = new();
    private readonly HashSet<ChampionController> boundControllers = new();
    private readonly HashSet<ChampionAbility> boundAbilities = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGCombatAudioBinderRuntime>() != null)
            return;
        GameObject obj = new GameObject("AOG_Combat_Audio_Binder_Runtime");
        obj.AddComponent<AOGCombatAudioBinderRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        BindChampions();
        BindControllers();
        BindAbilities();
    }

    private void BindChampions()
    {
        foreach (Champion champion in Resources.FindObjectsOfTypeAll<Champion>())
        {
            if (champion == null || !champion.gameObject.scene.IsValid() || boundChampions.Contains(champion))
                continue;

            boundChampions.Add(champion);
            champion.OnDamaged += (damage, type) =>
            {
                AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.ChampionHit, champion.transform.position);
            };
            champion.OnDeath += () =>
            {
                AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.ChampionDeath, champion.transform.position);
            };
        }
    }

    private void BindControllers()
    {
        foreach (ChampionController controller in Resources.FindObjectsOfTypeAll<ChampionController>())
        {
            if (controller == null || !controller.gameObject.scene.IsValid() || boundControllers.Contains(controller))
                continue;

            boundControllers.Add(controller);
            controller.OnBasicAttackWindup += () =>
            {
                AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.BasicAttack, controller.transform.position);
            };
            controller.OnBasicAttackResolved += () =>
            {
                AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.AbilityImpact, controller.transform.position);
            };
        }
    }

    private void BindAbilities()
    {
        foreach (ChampionAbility ability in Resources.FindObjectsOfTypeAll<ChampionAbility>())
        {
            if (ability == null || !ability.gameObject.scene.IsValid() || boundAbilities.Contains(ability))
                continue;

            boundAbilities.Add(ability);
            ability.OnCastStarted += _ =>
            {
                AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.AbilityCast, ability.transform.position);
            };
            ability.OnCastCompleted += _ =>
            {
                AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.AbilityImpact, ability.transform.position);
            };
        }
    }
}
