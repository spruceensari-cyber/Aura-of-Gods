using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Channelled recall system. Press B to channel, damage or death cancels, success returns champion to team base.
/// </summary>
public class AOGRecallRuntime : MonoBehaviour
{
    [SerializeField] private float recallDuration = 8f;

    private readonly Dictionary<Champion, Coroutine> activeRecalls = new();
    private readonly HashSet<Champion> bound = new();
    private Transform blueBase;
    private Transform redBase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGRecallRuntime>() != null)
            return;

        GameObject obj = new GameObject("AOG_Recall_Runtime");
        obj.AddComponent<AOGRecallRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        ResolveBases();
        BindChampions();

        if (Input.GetKeyDown(KeyCode.B))
        {
            Champion local = FindLocalChampion();
            if (local != null)
                BeginRecall(local);
        }
    }

    private void ResolveBases()
    {
        if (blueBase != null && redBase != null)
            return;

        MinionSpawner spawner = FindObjectOfType<MinionSpawner>();
        if (spawner == null)
            return;

        blueBase = spawner.blueBaseSpawn;
        redBase = spawner.redBaseSpawn;
    }

    private void BindChampions()
    {
        foreach (Champion champion in Resources.FindObjectsOfTypeAll<Champion>())
        {
            if (champion == null || !champion.gameObject.scene.IsValid() || !bound.Add(champion))
                continue;

            champion.OnDamaged += (damage, type) => CancelRecall(champion);
            champion.OnDeath += () => CancelRecall(champion);
        }
    }

    private Champion FindLocalChampion()
    {
        ChampionController controller = FindObjectOfType<ChampionController>();
        return controller != null ? controller.GetComponent<Champion>() : null;
    }

    public bool BeginRecall(Champion champion)
    {
        if (champion == null || !champion.IsAlive || activeRecalls.ContainsKey(champion))
            return false;

        Coroutine routine = StartCoroutine(RecallRoutine(champion));
        activeRecalls[champion] = routine;
        return true;
    }

    public void CancelRecall(Champion champion)
    {
        if (champion == null || !activeRecalls.TryGetValue(champion, out Coroutine routine))
            return;

        if (routine != null)
            StopCoroutine(routine);
        activeRecalls.Remove(champion);
        AOGAnnouncerRuntime announcer = FindObjectOfType<AOGAnnouncerRuntime>();
        announcer?.Announce("RECALL INTERRUPTED", AOGAudioCue.UIBack, 0.7f);
    }

    private IEnumerator RecallRoutine(Champion champion)
    {
        AOGAnnouncerRuntime announcer = FindObjectOfType<AOGAnnouncerRuntime>();
        announcer?.Announce("RECALLING", AOGAudioCue.AbilityCast, 0.8f);

        Vector3 startPosition = champion.transform.position;
        GameObject marker = CreateRecallMarker(champion.transform.position);
        float elapsed = 0f;

        while (champion != null && champion.IsAlive && elapsed < recallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (Vector3.Distance(champion.transform.position, startPosition) > 0.6f)
            {
                if (marker != null) Destroy(marker);
                activeRecalls.Remove(champion);
                yield break;
            }

            if (marker != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.08f;
                marker.transform.localScale = new Vector3(2.4f * pulse, 0.03f, 2.4f * pulse);
            }
            yield return null;
        }

        if (champion == null)
            yield break;

        Transform destination = champion.Team == TeamType.Red ? redBase : blueBase;
        if (destination != null)
            champion.transform.position = destination.position;

        champion.Heal(champion.MaxHealth);
        AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.UIConfirm, champion.transform.position);
        announcer?.Announce("RECALL COMPLETE", AOGAudioCue.UIConfirm, 0.7f);

        if (marker != null) Destroy(marker);
        activeRecalls.Remove(champion);
    }

    private static GameObject CreateRecallMarker(Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "AOG_Recall_Marker";
        marker.transform.position = position + Vector3.up * 0.05f;
        marker.transform.localScale = new Vector3(2.4f, 0.03f, 2.4f);
        Collider col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return marker;
    }
}
