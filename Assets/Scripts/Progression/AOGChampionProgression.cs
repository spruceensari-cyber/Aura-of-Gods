using System;
using UnityEngine;

public class AOGChampionProgression : MonoBehaviour
{
    public int level = 1;
    public int experience;
    public int experienceToNext = 280;
    public int maxLevel = 18;

    [Header("Ability Progression")]
    public int unspentSkillPoints = 1;
    public int qRank;
    public int wRank;
    public int eRank;
    public int rRank;
    public int basicAbilityMaxRank = 5;
    public int ultimateMaxRank = 3;

    public event Action ProgressionChanged;

    private AOGCharacterStats stats;

    public float ExperienceRatio => level >= maxLevel ? 1f : Mathf.Clamp01(experience / (float)Mathf.Max(1, experienceToNext));

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0 || level >= maxLevel)
            return;

        experience += amount;
        while (experience >= experienceToNext && level < maxLevel)
        {
            experience -= experienceToNext;
            LevelUp();
        }

        ProgressionChanged?.Invoke();
    }

    public int GetAbilityRank(int slot)
    {
        switch (slot)
        {
            case 0: return qRank;
            case 1: return wRank;
            case 2: return eRank;
            default: return rRank;
        }
    }

    public bool CanUpgradeAbility(int slot)
    {
        if (unspentSkillPoints <= 0)
            return false;

        int rank = GetAbilityRank(slot);
        if (slot == 3)
        {
            if (rank >= ultimateMaxRank)
                return false;
            int requiredLevel = rank == 0 ? 6 : rank == 1 ? 11 : 16;
            return level >= requiredLevel;
        }

        if (rank >= basicAbilityMaxRank)
            return false;

        // Prevent maxing a basic ability too early: ranks 1/2/3/4/5 require champion levels 1/3/5/7/9.
        int minimumLevel = 1 + rank * 2;
        return level >= minimumLevel;
    }

    public bool UpgradeAbility(int slot)
    {
        if (!CanUpgradeAbility(slot))
            return false;

        switch (slot)
        {
            case 0: qRank++; break;
            case 1: wRank++; break;
            case 2: eRank++; break;
            default: rRank++; break;
        }

        unspentSkillPoints--;
        ProgressionChanged?.Invoke();
        AOGAbilityVisuals.CreateRing("Ability_Rank_Up", transform.position + Vector3.up * 0.12f, 1.8f, new Color(0.38f, 0.82f, 1f, 1f), 0.10f);
        return true;
    }

    private void LevelUp()
    {
        level++;
        unspentSkillPoints++;
        experienceToNext = Mathf.RoundToInt(experienceToNext * 1.16f + 35f);

        if (stats == null)
            stats = GetComponent<AOGCharacterStats>();

        if (stats != null)
        {
            float oldMax = stats.maxHp;
            stats.maxHp += 78f;
            stats.hp += stats.maxHp - oldMax;
            stats.attackDamage += 4.2f;
            stats.moveSpeed += level % 4 == 0 ? 0.08f : 0f;
            stats.attackCooldown = Mathf.Max(0.42f, stats.attackCooldown * 0.988f);
        }

        AOGAbilityVisuals.CreateRing("Level_Up_Ring", transform.position + Vector3.up * 0.12f, 2.4f, new Color(0.96f, 0.78f, 0.28f, 1f), 0.14f);
        AOGMobaCameraController camera = Camera.main != null ? Camera.main.GetComponent<AOGMobaCameraController>() : null;
        camera?.AddRandomImpulse(0.12f);
        ProgressionChanged?.Invoke();
    }
}
