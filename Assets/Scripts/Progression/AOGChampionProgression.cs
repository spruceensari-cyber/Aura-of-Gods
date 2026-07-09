using System;
using UnityEngine;

public class AOGChampionProgression : MonoBehaviour
{
    public int level = 1;
    public int experience;
    public int experienceToNext = 280;
    public int maxLevel = 18;

    public event Action ProgressionChanged;

    private AOGCharacterStats stats;

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

    private void LevelUp()
    {
        level++;
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
    }
}
