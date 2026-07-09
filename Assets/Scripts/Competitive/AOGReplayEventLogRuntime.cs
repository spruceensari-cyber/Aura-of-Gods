using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AOGReplayEvent
{
    public float Time;
    public string Type;
    public string Actor;
    public string Target;
    public Vector3 Position;
}

public class AOGReplayEventLogRuntime : MonoBehaviour
{
    public static AOGReplayEventLogRuntime Instance { get; private set; }
    public IReadOnlyList<AOGReplayEvent> Events => events;
    readonly List<AOGReplayEvent> events = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGReplayEventLogRuntime>() != null) return;
        new GameObject("AOG_Replay_Event_Log_Runtime").AddComponent<AOGReplayEventLogRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Record(string type, string actor, string target, Vector3 position)
    {
        events.Add(new AOGReplayEvent
        {
            Time = Time.unscaledTime,
            Type = type,
            Actor = actor,
            Target = target,
            Position = position
        });
    }

    public void Clear() => events.Clear();
}
