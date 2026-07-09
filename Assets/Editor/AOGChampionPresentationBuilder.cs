#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class AOGChampionPresentationBuilder
{
    private const string Root = "Assets/Resources/AOGChampions";

    private const string Idle = "Assets/Sword and Shield Pack/sword and shield idle.fbx";
    private const string Run = "Assets/Sword and Shield Pack/sword and shield run.fbx";
    private const string Attack = "Assets/Sword and Shield Pack/sword and shield attack.fbx";
    private const string Slash2 = "Assets/Sword and Shield Pack/sword and shield slash (2).fbx";
    private const string Slash3 = "Assets/Sword and Shield Pack/sword and shield slash (3).fbx";
    private const string Kick = "Assets/Sword and Shield Pack/sword and shield kick.fbx";
    private const string Cast = "Assets/Sword and Shield Pack/sword and shield casting.fbx";
    private const string PowerUp = "Assets/Sword and Shield Pack/sword and shield power up.fbx";
    private const string Block = "Assets/Sword and Shield Pack/sword and shield block.fbx";
    private const string Impact = "Assets/Sword and Shield Pack/sword and shield impact.fbx";
    private const string Death = "Assets/Sword and Shield Pack/sword and shield death.fbx";

    static AOGChampionPresentationBuilder()
    {
        EditorApplication.delayCall += EnsureGeneratedAssets;
    }

    [MenuItem("Aura of Gods/Rebuild Champion Presentation Assets")]
    public static void RebuildAll()
    {
        EnsureFolders();
        BuildChampion("lyra", "Lyra", ChampionArchetype.Duelist, true);
        BuildChampion("astryn", "Astryn", ChampionArchetype.Duelist, true);
        BuildChampion("nyxara", "Nyxara", ChampionArchetype.ArcaneCaster, true);
        BuildChampion("vorcalis", "Vorcalis", ChampionArchetype.Guardian, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Aura of Gods: Lyra and the original champion presentation assets were rebuilt.");
    }

    public static void EnsureGeneratedAssets()
    {
        EnsureFolders();
        BuildChampion("lyra", "Lyra", ChampionArchetype.Duelist, false);
        BuildChampion("astryn", "Astryn", ChampionArchetype.Duelist, false);
        BuildChampion("nyxara", "Nyxara", ChampionArchetype.ArcaneCaster, false);
        BuildChampion("vorcalis", "Vorcalis", ChampionArchetype.Guardian, false);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder(Root))
            AssetDatabase.CreateFolder("Assets/Resources", "AOGChampions");
    }

    private static void BuildChampion(string id, string displayName, ChampionArchetype archetype, bool force)
    {
        string controllerPath = Root + "/" + displayName + "_Presentation.controller";
        string profilePath = Root + "/" + displayName + "_Presentation.asset";

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null || force)
        {
            if (controller != null)
                AssetDatabase.DeleteAsset(controllerPath);

            controller = BuildController(controllerPath, archetype);
        }

        ChampionPresentationProfile profile = AssetDatabase.LoadAssetAtPath<ChampionPresentationProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<ChampionPresentationProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        profile.championId = id;
        profile.displayName = displayName;
        profile.archetype = archetype;
        profile.animatorController = controller;

        switch (archetype)
        {
            case ChampionArchetype.Duelist:
                profile.basicAttackWindup = id == "lyra" ? 0.18f : 0.16f;
                profile.basicAttackRecovery = id == "lyra" ? 0.16f : 0.14f;
                profile.basicAttackVariants = 3;
                break;
            case ChampionArchetype.ArcaneCaster:
                profile.basicAttackWindup = 0.28f;
                profile.basicAttackRecovery = 0.22f;
                profile.basicAttackVariants = 2;
                break;
            default:
                profile.basicAttackWindup = 0.34f;
                profile.basicAttackRecovery = 0.28f;
                profile.basicAttackVariants = 3;
                break;
        }

        EditorUtility.SetDirty(profile);
    }

    private static AnimatorController BuildController(string path, ChampionArchetype archetype)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("SkillQ", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("SkillW", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("SkillE", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("SkillR", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Recall", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimationClip idle = LoadClip(Idle);
        AnimationClip run = LoadClip(Run);

        BlendTree locomotionTree = new BlendTree
        {
            name = "LocomotionBlend",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(locomotionTree, controller);
        if (idle != null) locomotionTree.AddChild(idle, 0f);
        if (run != null) locomotionTree.AddChild(run, 1f);

        AnimatorState locomotion = stateMachine.AddState("Locomotion");
        locomotion.motion = locomotionTree;
        stateMachine.defaultState = locomotion;

        AnimationClip attack0 = archetype == ChampionArchetype.ArcaneCaster ? LoadClip(Cast) : LoadClip(Attack);
        AnimationClip attack1 = archetype == ChampionArchetype.Guardian ? LoadClip(Block) : LoadClip(Slash2);
        AnimationClip attack2 = archetype == ChampionArchetype.ArcaneCaster ? LoadClip(PowerUp) : LoadClip(Slash3);

        AddAttackState(stateMachine, locomotion, "Attack_A", attack0, 0);
        AddAttackState(stateMachine, locomotion, "Attack_B", attack1, 1);
        AddAttackState(stateMachine, locomotion, "Attack_C", attack2, 2);

        if (archetype == ChampionArchetype.Duelist)
        {
            AddActionState(stateMachine, locomotion, "Skill_Q", LoadClip(Slash2), "SkillQ");
            AddActionState(stateMachine, locomotion, "Skill_W", LoadClip(Kick), "SkillW");
            AddActionState(stateMachine, locomotion, "Skill_E", LoadClip(Slash3), "SkillE");
            AddActionState(stateMachine, locomotion, "Skill_R", LoadClip(PowerUp), "SkillR");
        }
        else if (archetype == ChampionArchetype.ArcaneCaster)
        {
            AddActionState(stateMachine, locomotion, "Skill_Q", LoadClip(Cast), "SkillQ");
            AddActionState(stateMachine, locomotion, "Skill_W", LoadClip(PowerUp), "SkillW");
            AddActionState(stateMachine, locomotion, "Skill_E", LoadClip(Cast), "SkillE");
            AddActionState(stateMachine, locomotion, "Skill_R", LoadClip(PowerUp), "SkillR");
        }
        else
        {
            AddActionState(stateMachine, locomotion, "Skill_Q", LoadClip(Block), "SkillQ");
            AddActionState(stateMachine, locomotion, "Skill_W", LoadClip(Attack), "SkillW");
            AddActionState(stateMachine, locomotion, "Skill_E", LoadClip(Impact), "SkillE");
            AddActionState(stateMachine, locomotion, "Skill_R", LoadClip(PowerUp), "SkillR");
        }

        AddActionState(stateMachine, locomotion, "Hit", LoadClip(Impact), "Hit", 0.04f);
        AddActionState(stateMachine, locomotion, "Recall", LoadClip(PowerUp), "Recall", 0.12f);
        AddDeathState(stateMachine, "Death", LoadClip(Death));

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddAttackState(AnimatorStateMachine stateMachine, AnimatorState locomotion, string stateName, Motion motion, int attackIndex)
    {
        AnimatorState state = stateMachine.AddState(stateName);
        state.motion = motion;

        AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = 0.06f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        enter.AddCondition(AnimatorConditionMode.Equals, attackIndex, "AttackIndex");

        AnimatorStateTransition exit = state.AddTransition(locomotion);
        exit.hasExitTime = true;
        exit.exitTime = 0.88f;
        exit.duration = 0.08f;
    }

    private static void AddActionState(AnimatorStateMachine stateMachine, AnimatorState locomotion, string stateName, Motion motion, string trigger, float transitionDuration = 0.07f)
    {
        AnimatorState state = stateMachine.AddState(stateName);
        state.motion = motion;

        AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = transitionDuration;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);

        AnimatorStateTransition exit = state.AddTransition(locomotion);
        exit.hasExitTime = true;
        exit.exitTime = 0.9f;
        exit.duration = 0.09f;
    }

    private static void AddDeathState(AnimatorStateMachine stateMachine, string stateName, Motion motion)
    {
        AnimatorState state = stateMachine.AddState(stateName);
        state.motion = motion;

        AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = 0.08f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, "Death");
    }

    private static AnimationClip LoadClip(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__"))
                return clip;
        }

        Debug.LogWarning("Aura of Gods: animation clip missing at " + assetPath);
        return null;
    }
}
#endif
