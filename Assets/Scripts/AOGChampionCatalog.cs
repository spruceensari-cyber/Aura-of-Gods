using UnityEngine;

[System.Serializable]
public class AOGChampionDefinition
{
    public string id;
    public string displayName;
    public string title;
    public string role;
    public string lane;
    public string difficulty;
    public string quote;
    public string imageResource;
    public Color accent;
    public float maxHp;
    public float attackDamage;
    public float attackRange;
    public float moveSpeed;
    public float[] statBars;
    public string[] tags;
    public string[] abilityKeys;
    public string[] abilityNames;
    public string[] abilityDescriptions;
}

public static class AOGChampionCatalog
{
    public const string PlayerPrefsSelectedChampion = "AOG_SelectedChampion";
    public const string ResourceRoot = "AOGChampionCards/";

    private static AOGChampionDefinition[] all;

    public static AOGChampionDefinition[] All
    {
        get
        {
            if (all == null)
                all = BuildCatalog();

            return all;
        }
    }

    public static AOGChampionDefinition GetSelectedOrDefault()
    {
        string selected = PlayerPrefs.GetString(PlayerPrefsSelectedChampion, "ragnar");
        AOGChampionDefinition found = GetById(selected);
        return found ?? All[0];
    }

    public static AOGChampionDefinition GetById(string id)
    {
        foreach (AOGChampionDefinition champion in All)
        {
            if (champion.id == id)
                return champion;
        }

        return null;
    }

    public static Texture2D LoadPortrait(AOGChampionDefinition champion)
    {
        if (champion == null || string.IsNullOrEmpty(champion.imageResource))
            return null;

        return Resources.Load<Texture2D>(ResourceRoot + champion.imageResource);
    }

    private static AOGChampionDefinition[] BuildCatalog()
    {
        return new[]
        {
            Champion(
                "ragnar", "Ragnar", "Titan of Ash", "Fighter", "Top / Jungle", "Medium",
                "Born in ash. Built for the front line.",
                "Ragnar", "#FF5A1F", 980f, 68f, 4.2f, 5.8f,
                new[] { 78f, 72f, 42f, 48f, 63f },
                new[] { "Sustain", "Melee", "Durable" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Ashen Fury", "Magma Cleave", "Volcanic Skin", "Ashen Charge", "World Breaker" },
                new[] { "Damage builds fury and empowers the next skill.", "A burning axe arc cuts enemies in front.", "A short shield burns nearby enemies.", "Charge forward and stun on terrain impact.", "Smash the ground and open a lava rupture." }
            ),
            Champion(
                "lyra", "Lyra", "The Neon Huntress", "Assassin", "Jungle / Mid", "High",
                "Once a target is chosen, escape is only a rumor.",
                "Lyra", "#FF2C9A", 700f, 74f, 5.6f, 6.8f,
                new[] { 47f, 77f, 72f, 50f, 76f },
                new[] { "Burst", "Mobility", "Single Target" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Shadow Mark", "Neon Dagger", "Vanish Step", "Hunter's Net", "Blood Moon Execution" },
                new[] { "Repeated hits mark a target for bonus damage.", "Throw a fast dagger and dash a short distance.", "Briefly vanish and gain speed.", "Root an enemy inside a neon snare.", "Dive into shadows and execute low-health targets." }
            ),
            Champion(
                "dravenox", "Dravenox", "The Machine God", "Mage / Fighter", "Mid", "High",
                "Evolution begins where flesh ends.",
                "Dravenox", "#21D4D4", 820f, 58f, 6.0f, 5.4f,
                new[] { 65f, 55f, 76f, 80f, 45f },
                new[] { "Mage", "Control", "Barrier" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Mechanical Evolution", "Obliteration Ray", "Mecha Barrier", "Gravity Core", "Apocalyptic Protocol" },
                new[] { "Skills grant energy; full energy empowers the next cast.", "Fire a cyan beam in a line.", "Convert taken damage into a shield pulse.", "Pull enemies toward a gravity core.", "Overload the system and expand spell range." }
            ),
            Champion(
                "aeri", "Aeri", "The Dream Weaver", "Mage / Control", "Mid", "Medium",
                "Dreams bloom where reality forgets itself.",
                "Aeri", "#58AFFF", 690f, 48f, 6.4f, 5.9f,
                new[] { 48f, 42f, 74f, 86f, 58f },
                new[] { "Control", "Range", "Sleep" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Lucid Mind", "Dream Pulse", "Sleep Garden", "Mirror Step", "Endless Reverie" },
                new[] { "Damaging enemies restores mana and strengthens spells.", "Send a wave of dream energy.", "Create a garden that slows and sleeps enemies.", "Blink and leave a mirror behind.", "Pull nearby enemies into a disorienting dream." }
            ),
            Champion(
                "zephion", "Zephion", "The Storm Assassin", "Assassin / Burst", "Jungle / Mid", "High",
                "By the time thunder is heard, it is already late.",
                "Zephion", "#F6C94B", 720f, 76f, 5.1f, 7.1f,
                new[] { 50f, 82f, 70f, 56f, 87f },
                new[] { "Dash", "Burst", "Reset" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Storm's Edge", "Thunder Dash", "Static Blade", "Tempest Step", "Storm Reckoning" },
                new[] { "Every third attack chains lightning.", "Dash through a target and recast quickly.", "Spin the blade and chain lightning around you.", "Briefly fade into storm wind.", "Call down a storm on a selected area." }
            ),
            Champion(
                "solmira", "Solmira", "Priestess of Suns", "Support / Mage", "Bot / Mid", "Medium",
                "Light saves everyone. Sometimes it burns first.",
                "Solmira", "#FFC857", 740f, 44f, 6.2f, 5.5f,
                new[] { 56f, 46f, 80f, 72f, 50f },
                new[] { "Heal", "Shield", "Blind" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Solar Grace", "Ray of Sunlight", "Divine Aurora", "Sunfire Bind", "Day of Judgment" },
                new[] { "Every third spell heals or blinds.", "Send a piercing ray of sunlight.", "Bless an area with healing light.", "Bind enemies with a solar chain.", "Summon a judgment beam from above." }
            ),
            Champion(
                "elaris", "Elaris", "Moonblade Dancer", "Assassin / Duelist", "Moon Lane", "High",
                "Some warriors kill enemies. Others cut their fate.",
                "Elaris", "#8E83FF", 720f, 72f, 4.8f, 6.9f,
                new[] { 52f, 78f, 58f, 48f, 84f },
                new[] { "Duelist", "Crit", "Mobility" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Moon Dance", "Lunar Cut", "Moonstep", "Crescent Dance", "Eclipse Finale" },
                new[] { "Movement grants lunar energy.", "Dash and slash in a crescent.", "Vanish briefly into moonlight.", "Spin and slow enemies around you.", "Fire a wide moon blade finisher." }
            ),
            Champion(
                "kaelith", "Kaelith", "The Eclipse King", "King / Destroyer", "Any Lane", "Very High",
                "Neither light nor dark. Only command.",
                "Kaelith", "#9A55FF", 1040f, 70f, 5.2f, 5.3f,
                new[] { 74f, 76f, 78f, 55f, 54f },
                new[] { "Solar", "Void", "Duelist" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Eclipse Soul", "Duality Slash", "Gravity Collapse", "Time Shatter", "Godslayer Eclipse" },
                new[] { "Balance solar and void energy for empowered casts.", "Slash with light and void in sequence.", "Collapse a zone into a gravity well.", "Blink through time and reduce incoming damage.", "Darken the sky and silence enemy power." }
            ),
            Champion(
                "veyron", "Veyron", "The Abyss Caller", "Sorcerer / Void Mage", "Forbidden Zones", "Very High",
                "The void does not speak. It whispers.",
                "Veyron", "#8B42F6", 680f, 46f, 7.0f, 5.1f,
                new[] { 45f, 40f, 88f, 90f, 52f },
                new[] { "Void", "Zone", "Debuff" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Void Whisper", "Mind Rend", "Reality Fracture", "Abyss Grasp", "Eclipse Awakening" },
                new[] { "Whispers periodically weaken nearby enemies.", "Tear the mind of a target.", "Bend a region of reality.", "Pull an enemy with abyssal tendrils.", "Merge with the void and devastate an area." }
            ),
            Champion(
                "nightblade", "Night Blade", "Collection One", "Assassin", "Mid / Jungle", "Medium",
                "The night is not over yet.",
                "NightBlade", "#E31322", 690f, 69f, 4.9f, 6.5f,
                new[] { 46f, 72f, 52f, 58f, 74f },
                new[] { "Stealth", "Style", "Strike" },
                new[] { "P", "Q", "W", "E", "R" },
                new[] { "Silent Trace", "Redline Cut", "Blackout", "Sleeve Chain", "No Witness" },
                new[] { "Moving unseen builds strike power.", "Cut forward with a red arc.", "Dim enemy vision around you.", "Snare a target with a hidden chain.", "Execute a marked enemy in silence." }
            )
        };
    }

    private static AOGChampionDefinition Champion(
        string id,
        string name,
        string title,
        string role,
        string lane,
        string difficulty,
        string quote,
        string imageResource,
        string colorHex,
        float maxHp,
        float attackDamage,
        float attackRange,
        float moveSpeed,
        float[] statBars,
        string[] tags,
        string[] abilityKeys,
        string[] abilityNames,
        string[] abilityDescriptions)
    {
        Color accent = Color.white;
        ColorUtility.TryParseHtmlString(colorHex, out accent);

        return new AOGChampionDefinition
        {
            id = id,
            displayName = name,
            title = title,
            role = role,
            lane = lane,
            difficulty = difficulty,
            quote = quote,
            imageResource = imageResource,
            accent = accent,
            maxHp = maxHp,
            attackDamage = attackDamage,
            attackRange = attackRange,
            moveSpeed = moveSpeed,
            statBars = statBars,
            tags = tags,
            abilityKeys = abilityKeys,
            abilityNames = abilityNames,
            abilityDescriptions = abilityDescriptions
        };
    }
}
