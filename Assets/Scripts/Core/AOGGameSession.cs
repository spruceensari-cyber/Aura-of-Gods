using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-970)]
public sealed class AOGGameSession : MonoBehaviour
{
    public static AOGGameSession Instance { get; private set; }

    public string SelectedPlayerChampionId { get; private set; } = string.Empty;
    public MinionTeam PlayerTeam { get; private set; } = MinionTeam.Blue;
    public AOGRole PlayerRole { get; private set; } = AOGRole.Mid;
    public AOGMatchState MatchState { get; private set; } = AOGMatchState.Loading;
    public bool SelectionCommitted { get; private set; }

    public event Action<AOGChampionDefinition> SelectionCommittedChanged;
    public event Action<AOGMatchState> MatchStateChanged;

    private int activeSceneHandle = int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    public static AOGGameSession EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        AOGGameSession existing = FindFirstObjectByType<AOGGameSession>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject host = new GameObject("AOG_Game_Session");
        Instance = host.AddComponent<AOGGameSession>();
        DontDestroyOnLoad(host);
        return Instance;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance().BeginScene(scene);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void BeginScene(Scene scene)
    {
        if (!scene.IsValid() || activeSceneHandle == scene.handle)
            return;

        activeSceneHandle = scene.handle;
        SelectedPlayerChampionId = string.Empty;
        PlayerTeam = MinionTeam.Blue;
        PlayerRole = AOGRole.Mid;
        SelectionCommitted = false;
        SetMatchState(AOGMatchState.Loading);
        AOGPlayerChampionAuthority.Instance?.ClearForSceneChange();
    }

    public void BeginChampionSelection()
    {
        if (!SelectionCommitted)
            SetMatchState(AOGMatchState.ChampionSelect);
    }

    public bool TryCommitSelection(AOGChampionDefinition definition)
    {
        if (definition == null || SelectionCommitted)
            return false;

        SelectedPlayerChampionId = definition.id;
        PlayerTeam = MinionTeam.Blue;
        PlayerRole = definition.primaryRole;
        SelectionCommitted = true;
        SelectionCommittedChanged?.Invoke(definition);
        return true;
    }

    public void CancelSelection()
    {
        if (!SelectionCommitted)
            return;

        SelectedPlayerChampionId = string.Empty;
        PlayerTeam = MinionTeam.Blue;
        PlayerRole = AOGRole.Mid;
        SelectionCommitted = false;
        SetMatchState(AOGMatchState.ChampionSelect);
    }

    public void SetMatchState(AOGMatchState state)
    {
        if (MatchState == state)
            return;

        MatchState = state;
        MatchStateChanged?.Invoke(state);
    }
}
