using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enhances the existing competitive HUD in-place. It does not create a competing primary HUD.
/// Binds timer/gold to live match data and adds dynamic minimap markers for champions,
/// structures, camps and objectives.
/// </summary>
public class AOGCompetitiveHudLiveDataRuntime : MonoBehaviour
{
    private const float HalfWidth = 170f;
    private const float HalfDepth = 156f;

    private AOGCompetitiveMobaHUDRuntime hud;
    private RectTransform minimap;
    private Text timerText;
    private Text goldText;
    private Text objectiveOne;
    private Text objectiveTwo;
    private Text hintText;

    private readonly Dictionary<Object, RectTransform> markers = new Dictionary<Object, RectTransform>();
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCompetitiveHudLiveDataRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Competitive_HUD_Live_Data_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCompetitiveHudLiveDataRuntime>();
    }

    private void Update()
    {
        if (hud == null)
        {
            hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
            if (hud == null) return;
            ResolveHudReferences();
        }

        UpdateLiveHeader();
        UpdateObjectivePanel();

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.25f;
            RefreshWorldMarkers();
        }

        UpdateMarkerPositions();
    }

    private void ResolveHudReferences()
    {
        minimap = FindRect(hud.transform, "Minimap");
        timerText = FindText(hud.transform, "Timer");
        goldText = FindText(hud.transform, "Gold");
        objectiveOne = FindText(hud.transform, "Objective1");
        objectiveTwo = FindText(hud.transform, "Objective2");
        hintText = FindText(hud.transform, "Hint");

        Text oldPlayer = FindText(hud.transform, "Player");
        if (oldPlayer != null)
            oldPlayer.gameObject.SetActive(false);
    }

    private void UpdateLiveHeader()
    {
        if (timerText != null && AOGMatchDirector.Instance != null)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(AOGMatchDirector.Instance.MatchTime));
            timerText.text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        AOGPlayerEconomy economy = player != null ? player.GetComponent<AOGPlayerEconomy>() : null;
        if (goldText != null && economy != null)
            goldText.text = "◈ " + economy.gold;
    }

    private void UpdateObjectivePanel()
    {
        AOGNeutralBossAI dragon = null;
        AOGNeutralBossAI medusa = null;
        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (boss == null || boss.GetComponent<AOGVoidTitanMarker>() != null) continue;
            if (boss.bossType == AOGNeutralBossType.Dragon) dragon = boss;
            else medusa = boss;
        }

        AOGVoidTitanMarker titan = FindFirstObjectByType<AOGVoidTitanMarker>();
        if (objectiveOne != null)
            objectiveOne.text = dragon == null ? "◇ DRAGON  UNSEEN" : dragon.IsDead ? "◇ DRAGON  DEFEATED" : "◆ DRAGON  ACTIVE";

        if (objectiveTwo != null)
        {
            string medusaState = medusa == null ? "UNSEEN" : medusa.IsDead ? "DEFEATED" : "ACTIVE";
            string titanState = titan == null ? "LOCKED" : "ACTIVE";
            objectiveTwo.text = "◇ MEDUSA  " + medusaState + "   |   TITAN  " + titanState;
        }

        if (hintText != null)
        {
            bool shopAvailable = false;
            AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
            if (player != null)
                shopAvailable = AOGBaseAccessUtility.IsShopAvailable(player.GetComponent<AOGPlayerEconomy>());
            hintText.text = shopAvailable ? "P: MARKET   B: RECALL   SHOP READY" : "P: BROWSE   B: RECALL   SHOP OUT OF RANGE";
        }
    }

    private void RefreshWorldMarkers()
    {
        if (minimap == null) return;

        HashSet<Object> seen = new HashSet<Object>();

        foreach (AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (member == null) continue;
            seen.Add(member);
            EnsureMarker(member, member.team == MinionTeam.Blue ? new Color(0.18f,0.62f,1f) : new Color(1f,0.20f,0.26f), 14f, member.isHumanPlayer ? "▲" : "●");
        }

        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (tower == null || tower.hp <= 0f || tower.GetComponent<AOGSealCombatTargetAdapter>() != null) continue;
            seen.Add(tower);
            EnsureMarker(tower, tower.towerTeam == MinionTeam.Blue ? new Color(0.26f,0.70f,1f) : new Color(1f,0.30f,0.34f), 11f, "■");
        }

        foreach (AOGStrategicLaneSeal seal in FindObjectsByType<AOGStrategicLaneSeal>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (seal == null) continue;
            seen.Add(seal);
            Color c = seal.State == AOGSealState.Active ? (seal.team == MinionTeam.Blue ? new Color(0.42f,0.82f,1f) : new Color(1f,0.42f,0.46f)) : new Color(0.24f,0.24f,0.28f);
            EnsureMarker(seal,c,10f,"◇");
        }

        foreach (AOGNeutralMonsterRuntime monster in FindObjectsByType<AOGNeutralMonsterRuntime>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (monster == null || monster.IsDead) continue;
            seen.Add(monster);
            EnsureMarker(monster,new Color(0.46f,0.82f,0.48f),7f,"•");
        }

        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (boss == null || boss.IsDead) continue;
            seen.Add(boss);
            Color c = boss.GetComponent<AOGVoidTitanMarker>() != null ? new Color(0.62f,0.26f,0.96f) : boss.bossType == AOGNeutralBossType.Dragon ? new Color(1f,0.42f,0.10f) : new Color(0.66f,0.34f,0.92f);
            EnsureMarker(boss,c,15f,"◆");
        }

        List<Object> stale = new List<Object>();
        foreach (KeyValuePair<Object,RectTransform> pair in markers)
            if (pair.Key == null || !seen.Contains(pair.Key)) stale.Add(pair.Key);

        foreach (Object key in stale)
        {
            if (key != null && markers.TryGetValue(key,out RectTransform marker) && marker != null)
                Destroy(marker.gameObject);
            markers.Remove(key);
        }
    }

    private void EnsureMarker(Object key,Color color,float size,string glyph)
    {
        if (key == null || minimap == null) return;
        if (markers.TryGetValue(key,out RectTransform existing) && existing != null)
        {
            Text text = existing.GetComponent<Text>();
            if (text != null) text.color = color;
            return;
        }

        GameObject go = new GameObject("LiveMarker_" + key.name,typeof(RectTransform),typeof(Text));
        go.transform.SetParent(minimap,false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f,0.5f);
        rect.pivot = new Vector2(0.5f,0.5f);
        rect.sizeDelta = new Vector2(size,size);
        Text label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = Mathf.RoundToInt(size);
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = glyph;
        label.color = color;
        label.raycastTarget = false;
        markers[key] = rect;
    }

    private void UpdateMarkerPositions()
    {
        if (minimap == null) return;
        foreach (KeyValuePair<Object,RectTransform> pair in markers)
        {
            if (pair.Key == null || pair.Value == null) continue;
            Transform world = ResolveTransform(pair.Key);
            if (world == null) continue;
            Vector3 p = world.position;
            float x = Mathf.Clamp(p.x / HalfWidth, -1f, 1f) * 116f;
            float y = Mathf.Clamp(p.z / HalfDepth, -1f, 1f) * 116f;
            pair.Value.anchoredPosition = new Vector2(x,y);
        }
    }

    private static Transform ResolveTransform(Object obj)
    {
        Component component = obj as Component;
        return component != null ? component.transform : null;
    }

    private static RectTransform FindRect(Transform root,string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            if (rect.name == name) return rect;
        return null;
    }

    private static Text FindText(Transform root,string name)
    {
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
            if (text.name == name) return text;
        return null;
    }
}
