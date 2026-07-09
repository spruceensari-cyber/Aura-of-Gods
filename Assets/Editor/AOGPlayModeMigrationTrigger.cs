#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class AOGPlayModeMigrationTrigger
{
    static AOGPlayModeMigrationTrigger()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        EditorApplication.delayCall += AOGLegacyRosterAutoMigration.RepairCurrentSceneNow;
    }
}
#endif
