using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime combat feedback layer: floating damage numbers, hit flashes and lightweight screen impulse.
/// Presentation is event-driven and replaceable by production VFX/audio later.
/// </summary>
public class AOGCombatFeedbackRuntime : MonoBehaviour
{
    private const string RuntimeName = "AOG_Combat_Feedback_Runtime";
    private readonly HashSet<Champion> boundChampions = new();
    private Camera mainCamera;
    private Vector3 cameraBaseLocalPosition;
    private Coroutine shakeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGCombatFeedbackRuntime>() != null)
            return;

        GameObject obj = new GameObject(RuntimeName);
        obj.AddComponent<AOGCombatFeedbackRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
                cameraBaseLocalPosition = mainCamera.transform.localPosition;
        }

        BindChampions();
    }

    private void BindChampions()
    {
        Champion[] champions = Resources.FindObjectsOfTypeAll<Champion>();
        foreach (Champion champion in champions)
        {
            if (champion == null || !champion.gameObject.scene.IsValid() || boundChampions.Contains(champion))
                continue;

            boundChampions.Add(champion);
            champion.OnDamaged += (damage, type) => OnChampionDamaged(champion, damage, type);
            champion.OnDeath += () => OnChampionDeath(champion);
        }
    }

    private void OnChampionDamaged(Champion champion, float damage, DamageType type)
    {
        if (champion == null)
            return;

        SpawnDamageNumber(champion.transform.position + Vector3.up * 2.2f, damage, type);
        StartCoroutine(HitFlash(champion));

        ChampionController localController = FindObjectOfType<ChampionController>();
        if (localController != null && localController.GetComponent<Champion>() == champion)
            StartScreenImpulse(0.08f, Mathf.Clamp(damage / 450f, 0.015f, 0.08f));
    }

    private void OnChampionDeath(Champion champion)
    {
        if (champion == null)
            return;

        SpawnDamageNumber(champion.transform.position + Vector3.up * 2.8f, 0f, DamageType.True, "K.O.");
        StartScreenImpulse(0.18f, 0.10f);
    }

    private void SpawnDamageNumber(Vector3 worldPosition, float damage, DamageType type, string overrideText = null)
    {
        GameObject root = new GameObject("AOG_Damage_Number", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;
        canvas.sortingOrder = 500;

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.position = worldPosition;
        rect.sizeDelta = new Vector2(240f, 80f);
        rect.localScale = Vector3.one * 0.012f;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(root.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = overrideText ?? Mathf.CeilToInt(damage).ToString();
        text.fontSize = overrideText == null ? 44 : 34;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = type switch
        {
            DamageType.Physical => new Color(1f, 0.62f, 0.16f),
            DamageType.Magical => new Color(0.48f, 0.30f, 1f),
            _ => Color.white
        };
        text.raycastTarget = false;

        StartCoroutine(AnimateDamageNumber(root.transform, text, 0.85f));
    }

    private IEnumerator AnimateDamageNumber(Transform root, Text text, float lifetime)
    {
        float elapsed = 0f;
        Vector3 start = root.position;
        while (root != null && elapsed < lifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            root.position = start + Vector3.up * Mathf.Lerp(0f, 1.4f, t);
            Color c = text.color;
            c.a = 1f - Mathf.Pow(t, 2f);
            text.color = c;
            yield return null;
        }

        if (root != null)
            Destroy(root.gameObject);
    }

    private IEnumerator HitFlash(Champion champion)
    {
        Renderer[] renderers = champion.GetComponentsInChildren<Renderer>(true);
        List<(Renderer renderer, Color color)> originals = new();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.material == null || !renderer.material.HasProperty("_BaseColor"))
                continue;

            Color original = renderer.material.GetColor("_BaseColor");
            originals.Add((renderer, original));
            renderer.material.SetColor("_BaseColor", Color.white);
        }

        yield return new WaitForSecondsRealtime(0.07f);

        foreach ((Renderer renderer, Color color) in originals)
        {
            if (renderer != null && renderer.material != null && renderer.material.HasProperty("_BaseColor"))
                renderer.material.SetColor("_BaseColor", color);
        }
    }

    private void StartScreenImpulse(float duration, float magnitude)
    {
        if (mainCamera == null)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ScreenImpulse(duration, magnitude));
    }

    private IEnumerator ScreenImpulse(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (mainCamera != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float falloff = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 random = Random.insideUnitCircle * magnitude * falloff;
            mainCamera.transform.localPosition = cameraBaseLocalPosition + new Vector3(random.x, random.y, 0f);
            yield return null;
        }

        if (mainCamera != null)
            mainCamera.transform.localPosition = cameraBaseLocalPosition;
        shakeRoutine = null;
    }
}
