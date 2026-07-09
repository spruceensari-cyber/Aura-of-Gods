using System;
using UnityEngine;

[Serializable]
public class AOGRoleMatchMetrics
{
    public string Role;
    public float DamageShare;
    public float DamageTakenShare;
    public float KillParticipation;
    public float ObjectiveParticipation;
    public float VisionImpact;
    public float CrowdControlSeconds;
    public float AllySaveImpact;
    public float TowerDamageShare;
    public float GoldEfficiency;
    public float RoamImpact;
    public float LanePressure;
    public float JungleControl;
}

/// <summary>
/// Converts raw match contribution into a normalized 0..1 role impact score.
/// Different roles are evaluated by different responsibilities so supports are not judged like assassins.
/// </summary>
public static class AOGRoleImpactRuntime
{
    public static float Calculate(AOGRoleMatchMetrics m)
    {
        if (m == null) return 0.5f;

        string role = string.IsNullOrWhiteSpace(m.Role) ? "Fill" : m.Role;
        float score = role switch
        {
            "Support" => Weighted(
                (m.KillParticipation, 0.20f),
                (m.ObjectiveParticipation, 0.15f),
                (m.VisionImpact, 0.25f),
                (NormalizeCC(m.CrowdControlSeconds), 0.15f),
                (m.AllySaveImpact, 0.20f),
                (m.RoamImpact, 0.05f)),

            "Jungle" => Weighted(
                (m.ObjectiveParticipation, 0.30f),
                (m.KillParticipation, 0.20f),
                (m.JungleControl, 0.20f),
                (m.RoamImpact, 0.15f),
                (m.LanePressure, 0.10f),
                (m.VisionImpact, 0.05f)),

            "Mid" => Weighted(
                (m.DamageShare, 0.25f),
                (m.KillParticipation, 0.20f),
                (m.RoamImpact, 0.20f),
                (m.ObjectiveParticipation, 0.15f),
                (m.LanePressure, 0.10f),
                (m.GoldEfficiency, 0.10f)),

            "Bot" => Weighted(
                (m.DamageShare, 0.30f),
                (m.GoldEfficiency, 0.20f),
                (m.KillParticipation, 0.15f),
                (m.TowerDamageShare, 0.15f),
                (m.ObjectiveParticipation, 0.10f),
                (1f - Mathf.Clamp01(m.DamageTakenShare), 0.10f)),

            "Top" => Weighted(
                (m.LanePressure, 0.25f),
                (m.DamageTakenShare, 0.15f),
                (m.TowerDamageShare, 0.20f),
                (m.ObjectiveParticipation, 0.15f),
                (m.DamageShare, 0.15f),
                (m.KillParticipation, 0.10f)),

            _ => Weighted(
                (m.KillParticipation, 0.20f),
                (m.ObjectiveParticipation, 0.20f),
                (m.DamageShare, 0.15f),
                (m.GoldEfficiency, 0.15f),
                (m.LanePressure, 0.10f),
                (m.RoamImpact, 0.10f),
                (m.VisionImpact, 0.10f))
        };

        return Mathf.Clamp01(score);
    }

    private static float NormalizeCC(float seconds)
    {
        return Mathf.Clamp01(seconds / 40f);
    }

    private static float Weighted(params (float value, float weight)[] values)
    {
        float total = 0f;
        float weight = 0f;
        foreach ((float value, float w) in values)
        {
            total += Mathf.Clamp01(value) * w;
            weight += w;
        }
        return weight <= 0f ? 0.5f : total / weight;
    }
}
