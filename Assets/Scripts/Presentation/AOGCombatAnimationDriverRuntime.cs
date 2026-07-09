using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Additive procedural combat animation layer for prototype and production rigs.
/// Drives readable windup, recoil and death motion without hard-coding a specific character model.
/// </summary>
public class AOGCombatAnimationDriverRuntime : MonoBehaviour
{
    private readonly HashSet<Champion> bound = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGCombatAnimationDriverRuntime>() != null)
            return;
        GameObject obj = new GameObject("AOG_Combat_Animation_Driver_Runtime");
        obj.AddComponent<AOGCombatAnimationDriverRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        foreach (Champion champion in Resources.FindObjectsOfTypeAll<Champion>())
        {
            if (champion == null || !champion.gameObject.scene.IsValid() || bound.Contains(champion))
                continue;

            bound.Add(champion);
            BindChampion(champion);
        }
    }

    private void BindChampion(Champion champion)
    {
        ChampionController controller = champion.GetComponent<ChampionController>();
        Transform visualRoot = FindVisualRoot(champion.transform);
        if (visualRoot == null)
            return;

        if (controller != null)
        {
            controller.OnBasicAttackWindup += () =>
            {
                if (visualRoot != null)
                    StartCoroutine(AttackWindup(visualRoot));
            };
            controller.OnBasicAttackResolved += () =>
            {
                if (visualRoot != null)
                    StartCoroutine(AttackRecovery(visualRoot));
            };
        }

        champion.OnDamaged += (damage, type) =>
        {
            if (visualRoot != null)
                StartCoroutine(HitReaction(visualRoot, Mathf.Clamp01(damage / 180f)));
        };

        champion.OnDeath += () =>
        {
            if (visualRoot != null)
                StartCoroutine(DeathReaction(visualRoot));
        };
    }

    private Transform FindVisualRoot(Transform championRoot)
    {
        Animator animator = championRoot.GetComponentInChildren<Animator>(true);
        if (animator != null)
            return animator.transform;

        foreach (Transform child in championRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == championRoot)
                continue;
            if (child.GetComponent<Renderer>() != null)
                return child;
        }

        return championRoot;
    }

    private IEnumerator AttackWindup(Transform root)
    {
        Quaternion start = root.localRotation;
        Quaternion end = start * Quaternion.Euler(-4f, -12f, 0f);
        yield return Rotate(root, start, end, 0.07f);
    }

    private IEnumerator AttackRecovery(Transform root)
    {
        Quaternion start = root.localRotation;
        Quaternion overshoot = start * Quaternion.Euler(3f, 22f, 0f);
        yield return Rotate(root, start, overshoot, 0.06f);
        if (root != null)
            yield return Rotate(root, root.localRotation, Quaternion.identity, 0.11f);
    }

    private IEnumerator HitReaction(Transform root, float strength)
    {
        Quaternion start = root.localRotation;
        Quaternion recoil = start * Quaternion.Euler(4f + 8f * strength, 0f, -3f - 6f * strength);
        yield return Rotate(root, start, recoil, 0.045f);
        if (root != null)
            yield return Rotate(root, root.localRotation, Quaternion.identity, 0.10f);
    }

    private IEnumerator DeathReaction(Transform root)
    {
        Quaternion start = root.localRotation;
        Quaternion fallen = Quaternion.Euler(72f, 0f, 18f);
        yield return Rotate(root, start, fallen, 0.28f);
    }

    private IEnumerator Rotate(Transform root, Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;
        while (root != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            t = t * t * (3f - 2f * t);
            root.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
        if (root != null)
            root.localRotation = to;
    }
}
