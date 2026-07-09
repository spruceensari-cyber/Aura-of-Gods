using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(1200)]
public class AOGDynamicChampionHudBinder : MonoBehaviour
{
    private AOGActiveChampion active;
    private AOGCharacterStats stats;
    private AOGPlayerEconomy economy;
    private AOGChampionProgression progression;
    private IAOGAbilityCooldownProvider provider;
    private LyraSkillSet lyra;

    private Text championName;
    private Text roleName;
    private Text hpText;
    private Image hpFill;
    private Text resourceText;
    private Image resourceFill;
    private Text goldText;
    private Text levelText;
    private Text attackText;
    private Text speedText;
    private readonly Text[] abilityNames = new Text[4];
    private readonly Text[] cooldownNumbers = new Text[4];
    private readonly Image[] cooldownMasks = new Image[4];
    private readonly Image[] abilityIcons = new Image[4];
    private readonly Image[] inventorySlots = new Image[6];

    private Transform hudRoot;
    private float nextCacheAttempt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGDynamicChampionHudBinder>() != null)
            return;

        GameObject host = new GameObject("AOG_Dynamic_Champion_HUD_Binder");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGDynamicChampionHudBinder>();
    }

    private void Update()
    {
        if (hudRoot == null && Time.unscaledTime >= nextCacheAttempt)
        {
            nextCacheAttempt = Time.unscaledTime + 0.4f;
            TryCacheHud();
        }

        if (AOGActiveChampion.Current != null && AOGActiveChampion.Current != active)
            Bind(AOGActiveChampion.Current);

        if (active == null || hudRoot == null)
            return;

        UpdateVitals();
        UpdateAbilities();
        UpdateEconomy();
        UpdateStats();
    }

    public void Bind(AOGActiveChampion champion)
    {
        active = champion;
        if (active == null)
            return;

        stats = active.GetComponent<AOGCharacterStats>();
        economy = active.GetComponent<AOGPlayerEconomy>();
        progression = active.GetComponent<AOGChampionProgression>();
        lyra = active.GetComponent<LyraSkillSet>();
        provider = null;

        MonoBehaviour[] behaviours = active.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IAOGAbilityCooldownProvider cooldownProvider)
            {
                provider = cooldownProvider;
                break;
            }
        }

        TryCacheHud();
        ApplyStaticChampionIdentity();
    }

    private void TryCacheHud()
    {
        AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
        if (hud == null)
            return;

        hudRoot = hud.transform;
        championName = FindText(hudRoot, "Name");
        roleName = FindText(hudRoot, "Role");
        hpText = FindText(hudRoot, "HP_TEXT");
        hpFill = FindImage(hudRoot, "HP_FILL");
        resourceText = FindText(hudRoot, "Resource_TEXT");
        resourceFill = FindImage(hudRoot, "Resource_FILL");
        goldText = FindText(hudRoot, "Gold");
        levelText = FindText(hudRoot, "Level");
        attackText = FindText(hudRoot, "Attack");
        speedText = FindText(hudRoot, "Speed");

        string[] keys = { "Q", "W", "E", "R" };
        for (int i = 0; i < keys.Length; i++)
        {
            Transform slot = FindTransform(hudRoot, "Ability_" + keys[i]);
            if (slot == null)
                continue;

            abilityNames[i] = FindText(slot, "AbilityName");
            cooldownNumbers[i] = FindText(slot, "Cooldown");
            cooldownMasks[i] = FindImage(slot, "CooldownMask");
            abilityIcons[i] = FindImage(slot, "Icon");
        }

        Transform itemsRoot = FindTransform(hudRoot, "Items");
        if (itemsRoot != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                Transform item = FindTransform(itemsRoot, "Item_" + i);
                if (item != null)
                    inventorySlots[i] = item.GetComponent<Image>();
            }
        }

        ApplyStaticChampionIdentity();
    }

    private void ApplyStaticChampionIdentity()
    {
        if (active == null)
            return;

        if (championName != null)
            championName.text = active.displayName;
        if (roleName != null)
        {
            roleName.text = active.roleName;
            roleName.color = active.accentColor;
        }

        for (int i = 0; i < 4; i++)
        {
            if (abilityNames[i] != null)
                abilityNames[i].text = GetAbilityName(i);

            if (abilityIcons[i] != null)
            {
                Color c = active.accentColor;
                float slotMix = 0.84f + i * 0.045f;
                abilityIcons[i].color = new Color(Mathf.Clamp01(c.r * slotMix + 0.10f), Mathf.Clamp01(c.g * slotMix + 0.06f), Mathf.Clamp01(c.b * slotMix + 0.12f), 1f);
            }
        }

        Transform portraitFrame = hudRoot != null ? FindTransform(hudRoot, "PortraitFrame") : null;
        if (portraitFrame != null)
        {
            Outline outline = portraitFrame.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = active.accentColor;
        }
    }

    private void UpdateVitals()
    {
        if (stats != null)
        {
            float ratio = Mathf.Clamp01(stats.hp / Mathf.Max(1f, stats.maxHp));
            if (hpFill != null) hpFill.fillAmount = ratio;
            if (hpText != null) hpText.text = Mathf.CeilToInt(stats.hp) + " / " + Mathf.CeilToInt(stats.maxHp);
        }

        float aether = 0.86f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.05f;
        if (resourceFill != null) resourceFill.fillAmount = aether;
        if (resourceText != null) resourceText.text = Mathf.RoundToInt(aether * 100f) + " AETHER";

        if (levelText != null && progression != null)
            levelText.text = progression.level.ToString();
    }

    private void UpdateAbilities()
    {
        for (int i = 0; i < 4; i++)
        {
            float ratio = GetCooldownRatio(i);
            float duration = GetCooldownDuration(i);
            if (cooldownMasks[i] != null)
                cooldownMasks[i].fillAmount = ratio;
            if (cooldownNumbers[i] != null)
                cooldownNumbers[i].text = ratio > 0.01f ? Mathf.CeilToInt(ratio * duration).ToString() : string.Empty;
            if (abilityIcons[i] != null)
            {
                Color c = active != null ? active.accentColor : Color.white;
                abilityIcons[i].color = ratio > 0.01f
                    ? new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 1f)
                    : new Color(Mathf.Clamp01(c.r + 0.18f), Mathf.Clamp01(c.g + 0.14f), Mathf.Clamp01(c.b + 0.18f), 1f);
            }
        }
    }

    private void UpdateEconomy()
    {
        if (economy == null)
            return;

        if (goldText != null)
            goldText.text = "◈ " + economy.gold;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            Image slot = inventorySlots[i];
            if (slot == null)
                continue;

            if (i < economy.inventory.Count)
            {
                Color accent = economy.inventory[i].accent;
                slot.color = new Color(accent.r * 0.72f, accent.g * 0.72f, accent.b * 0.72f, 1f);
            }
            else
            {
                slot.color = new Color(0.035f, 0.05f, 0.075f, 1f);
            }
        }
    }

    private void UpdateStats()
    {
        if (stats == null)
            return;

        if (attackText != null)
            attackText.text = "⚔ " + Mathf.RoundToInt(stats.attackDamage);
        if (speedText != null)
            speedText.text = "➤ " + stats.moveSpeed.ToString("0.0");
    }

    private string GetAbilityName(int slot)
    {
        if (provider != null)
            return provider.GetAbilityName(slot);

        if (lyra != null)
        {
            switch (slot)
            {
                case 0: return "DAGGER";
                case 1: return "VANISH";
                case 2: return "HUNTER'S NET";
                default: return "BLOOD MOON";
            }
        }

        return slot == 0 ? "ABILITY I" : slot == 1 ? "ABILITY II" : slot == 2 ? "ABILITY III" : "ULTIMATE";
    }

    private float GetCooldownRatio(int slot)
    {
        if (provider != null)
            return provider.GetAbilityCooldownRatio(slot);

        if (lyra == null)
            return 0f;

        switch (slot)
        {
            case 0: return lyra.GetQCooldownRatio();
            case 1: return lyra.GetWCooldownRatio();
            case 2: return lyra.GetECooldownRatio();
            default: return lyra.GetRCooldownRatio();
        }
    }

    private float GetCooldownDuration(int slot)
    {
        if (provider != null)
            return provider.GetAbilityCooldownDuration(slot);

        if (lyra == null)
            return 0f;

        switch (slot)
        {
            case 0: return lyra.qCooldown;
            case 1: return lyra.wCooldown;
            case 2: return lyra.eCooldown;
            default: return lyra.rCooldown;
        }
    }

    private static Transform FindTransform(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindTransform(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Text FindText(Transform root, string objectName)
    {
        Transform found = FindTransform(root, objectName);
        return found != null ? found.GetComponent<Text>() : null;
    }

    private static Image FindImage(Transform root, string objectName)
    {
        Transform found = FindTransform(root, objectName);
        return found != null ? found.GetComponent<Image>() : null;
    }
}

[DefaultExecutionOrder(1300)]
public class AOGDynamicMinimapMarkers : MonoBehaviour
{
    private RectTransform minimap;
    private RectTransform markerRoot;
    private readonly List<RectTransform> markerPool = new List<RectTransform>();
    private readonly List<Image> markerImages = new List<Image>();
    private float nextRefresh;
    private Vector2 worldMin = new Vector2(-60f, -60f);
    private Vector2 worldMax = new Vector2(60f, 60f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGDynamicMinimapMarkers>() != null)
            return;

        GameObject host = new GameObject("AOG_Dynamic_Minimap_Markers");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGDynamicMinimapMarkers>();
    }

    private void Update()
    {
        if (minimap == null)
        {
            AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
            if (hud == null)
                return;

            Transform map = FindTransform(hud.transform, "Minimap");
            if (map == null)
                return;

            minimap = map as RectTransform;
            BuildMarkerRoot();
            ComputeWorldBounds();
        }

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.18f;
            RefreshMarkers();
        }
    }

    private void BuildMarkerRoot()
    {
        GameObject root = new GameObject("LiveMarkers", typeof(RectTransform));
        root.transform.SetParent(minimap, false);
        markerRoot = root.GetComponent<RectTransform>();
        markerRoot.anchorMin = Vector2.zero;
        markerRoot.anchorMax = Vector2.one;
        markerRoot.offsetMin = Vector2.zero;
        markerRoot.offsetMax = Vector2.zero;
    }

    private void ComputeWorldBounds()
    {
        List<Vector3> points = new List<Vector3>();
        TowerHealth[] towers = FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (TowerHealth tower in towers)
            if (tower != null) points.Add(tower.transform.position);

        AOGNexusCore[] nexuses = FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGNexusCore nexus in nexuses)
            if (nexus != null) points.Add(nexus.transform.position);

        if (points.Count < 2)
            return;

        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (Vector3 point in points)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        Vector2 padding = new Vector2(8f, 8f);
        worldMin = new Vector2(minX, minZ) - padding;
        worldMax = new Vector2(maxX, maxZ) + padding;
    }

    private void RefreshMarkers()
    {
        if (markerRoot == null)
            return;

        int index = 0;
        AOGNexusCore[] nexuses = FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGNexusCore nexus in nexuses)
        {
            if (nexus == null) continue;
            SetMarker(index++, nexus.transform.position, nexus.team == MinionTeam.Blue ? new Color(0.18f, 0.62f, 1f) : new Color(1f, 0.20f, 0.24f), 13f, true);
        }

        TowerHealth[] towers = FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (TowerHealth tower in towers)
        {
            if (tower == null || tower.hp <= 0f) continue;
            SetMarker(index++, tower.transform.position, tower.towerTeam == MinionTeam.Blue ? new Color(0.20f, 0.64f, 1f) : new Color(1f, 0.24f, 0.28f), 7f, false);
        }

        AOGNeutralBossAI[] bosses = FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGNeutralBossAI boss in bosses)
        {
            if (boss == null || boss.IsDead) continue;
            SetMarker(index++, boss.transform.position, new Color(0.76f, 0.34f, 0.96f), 9f, true);
        }

        Minion[] minions = FindObjectsByType<Minion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int stride = Mathf.Max(1, minions.Length / 42);
        for (int i = 0; i < minions.Length; i += stride)
        {
            Minion minion = minions[i];
            if (minion == null || minion.hp <= 0f) continue;
            SetMarker(index++, minion.transform.position, minion.team == MinionTeam.Blue ? new Color(0.20f, 0.54f, 1f) : new Color(1f, 0.18f, 0.20f), 3.5f, false);
        }

        AOGActiveChampion current = AOGActiveChampion.Current;
        if (current != null)
            SetMarker(index++, current.transform.position, new Color(1f, 0.88f, 0.28f), 10f, true);

        for (int i = index; i < markerPool.Count; i++)
            markerPool[i].gameObject.SetActive(false);
    }

    private void SetMarker(int index, Vector3 worldPosition, Color color, float size, bool diamond)
    {
        EnsureMarker(index);
        RectTransform marker = markerPool[index];
        Image image = markerImages[index];
        marker.gameObject.SetActive(true);
        image.color = color;
        marker.sizeDelta = new Vector2(size, size);
        marker.localRotation = diamond ? Quaternion.Euler(0f, 0f, 45f) : Quaternion.identity;

        float u = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x);
        float v = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.z);
        Vector2 sizeMap = minimap.rect.size;
        marker.anchoredPosition = new Vector2((u - 0.5f) * sizeMap.x, (v - 0.5f) * sizeMap.y);
    }

    private void EnsureMarker(int index)
    {
        while (markerPool.Count <= index)
        {
            GameObject go = new GameObject("Marker_" + markerPool.Count, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(markerRoot, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            markerPool.Add(rect);
            markerImages.Add(image);
        }
    }

    private static Transform FindTransform(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;
        foreach (Transform child in root)
        {
            Transform found = FindTransform(child, objectName);
            if (found != null) return found;
        }
        return null;
    }
}
