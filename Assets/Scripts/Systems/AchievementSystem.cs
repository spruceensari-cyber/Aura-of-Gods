using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
Pentakill and milestone tracking system with special effects
/// </summary>
public class AchievementSystem : MonoBehaviour
{
    [SerializeField] private AudioClip pentakillSound;
    [SerializeField] private AudioClip milestoneSoundClip;
    
    private Dictionary<Champion, int> killStreaks = new();
    private Dictionary<Champion, int> totalKills = new();
    
    public enum KillMilestone
    {
        FirstBlood,
        DoubleKill,
        TripleKill,
        QuadraKill,
        Pentakill,
        Unstoppable,
        Legendary
    }
    
    private delegate void MilestoneEvent(Champion champion, KillMilestone milestone);
    private event MilestoneEvent OnMilestoneAchieved;
    
    void Start()
    {
        OnMilestoneAchieved += DisplayMilestoneNotification;
        OnMilestoneAchieved += PlayMilestoneSound;
        OnMilestoneAchieved += SpawnMilestoneEffect;
    }
    
    public void RecordKill(Champion killer)
    {
        if (!killStreaks.ContainsKey(killer))
            killStreaks[killer] = 0;
        if (!totalKills.ContainsKey(killer))
            totalKills[killer] = 0;
        
        killStreaks[killer]++;
        totalKills[killer]++;
        
        KillMilestone milestone = GetMilestoneFromKillStreak(killStreaks[killer]);
        
        if (milestone != KillMilestone.FirstBlood)
        {
            OnMilestoneAchieved?.Invoke(killer, milestone);
        }
    }
    
    public void ResetKillStreak(Champion champion)
    {
        if (killStreaks.ContainsKey(champion))
            killStreaks[champion] = 0;
    }
    
    private KillMilestone GetMilestoneFromKillStreak(int streak)
    {
        return streak switch
        {
            1 => KillMilestone.FirstBlood,
            2 => KillMilestone.DoubleKill,
            3 => KillMilestone.TripleKill,
            4 => KillMilestone.QuadraKill,
            5 => KillMilestone.Pentakill,
            6 => KillMilestone.Unstoppable,
            7 => KillMilestone.Legendary,
            _ => KillMilestone.Legendary
        };
    }
    
    private void DisplayMilestoneNotification(Champion champion, KillMilestone milestone)
    {
        string message = $"{champion.name} - {milestone}!";
        Debug.Log($"⚡ {message}");
        
        // Display on screen (integrate with UI system)
    }
    
    private void PlayMilestoneSound(Champion champion, KillMilestone milestone)
    {
        if (milestone == KillMilestone.Pentakill && pentakillSound != null)
        {
            AudioSource audio = champion.GetComponent<AudioSource>();
            if (audio != null)
                audio.PlayOneShot(pentakillSound);
        }
    }
    
    private void SpawnMilestoneEffect(Champion champion, KillMilestone milestone)
    {
        Vector3 pos = champion.transform.position;
        
        if (milestone == KillMilestone.Pentakill)
        {
            // Pentakill visual explosion
            GothicParticlePresets.CreateShadowExplosion(pos, 5f);
            GothicParticlePresets.CreateBloodExplosion(pos);
            for (int i = 0; i < 3; i++)
                GothicParticlePresets.CreateDarkAura(pos + Random.insideUnitSphere, 3f);
        }
        else if (milestone == KillMilestone.TripleKill)
        {
            GothicParticlePresets.CreateDarkAura(pos, 2.5f);
        }
    }
    
    public int GetKillStreak(Champion champion)
    {
        return killStreaks.ContainsKey(champion) ? killStreaks[champion] : 0;
    }
    
    public int GetTotalKills(Champion champion)
    {
        return totalKills.ContainsKey(champion) ? totalKills[champion] : 0;
    }
}
