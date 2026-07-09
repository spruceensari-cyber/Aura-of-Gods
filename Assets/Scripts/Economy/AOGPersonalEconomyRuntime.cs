using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grants champions opening and passive personal gold so the item loop can function independently of team accounting.
/// </summary>
public class AOGPersonalEconomyRuntime : MonoBehaviour
{
    [SerializeField] private int startingGold = 500;
    [SerializeField] private float passiveGoldPerSecond = 1.8f;

    private readonly HashSet<Champion> initialized = new();
    private readonly Dictionary<Champion, float> fractionalGold = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGPersonalEconomyRuntime>() != null)
            return;

        GameObject obj = new GameObject("AOG_Personal_Economy_Runtime");
        obj.AddComponent<AOGPersonalEconomyRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        foreach (Champion champion in FindObjectsByType<Champion>(FindObjectsSortMode.None))
        {
            if (champion == null)
                continue;

            if (initialized.Add(champion))
            {
                champion.GainGold(startingGold);
                fractionalGold[champion] = 0f;
            }

            if (!champion.IsAlive)
                continue;

            float value = fractionalGold.TryGetValue(champion, out float current) ? current : 0f;
            value += passiveGoldPerSecond * Time.deltaTime;
            int whole = Mathf.FloorToInt(value);
            if (whole > 0)
            {
                champion.GainGold(whole);
                value -= whole;
            }
            fractionalGold[champion] = value;
        }
    }
}
