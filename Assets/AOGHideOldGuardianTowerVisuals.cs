using UnityEngine;

public class AOGHideOldGuardianTowerVisuals : MonoBehaviour
{
    [ContextMenu("Hide Old Guardian Tower Visuals")]
    public void HideOldGuardianTowerVisuals()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int hiddenCount = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string n = renderer.gameObject.name.ToLower();

            bool isOldGuardianVisual =
                n.Contains("guardian_body") ||
                n.Contains("guardian_form") ||
                n.Contains("guardian_wing") ||
                n.Contains("guardian_halo") ||
                n.Contains("guardian_weapon") ||
                n.Contains("final_guardian") ||
                n.Contains("outer_guardian") ||
                n.Contains("inner_guardian") ||

                // Senin yazdığın eski kule parçaları
                n.Contains("small_flame") ||
                n.Contains("flame") ||
                n.Contains("glow") ||
                n.Contains("attack_energy_core") ||
                n.Contains("energy_core") ||
                n.Contains("energy_head") ||
                n.Contains("wing_l") ||
                n.Contains("wing_r") ||
                n.Contains("small_gold_rune") ||
                n.Contains("gold_rune") ||

                // Red eski kule özel parçaları
                n.Contains("red_top_outer_final") ||
                n.Contains("red_bot_outer") ||
                n.Contains("red_top_inner") ||
                n.Contains("red_bot_inner") ||

                // Blue eski kule özel parçaları
                n.Contains("blue_top_outer") ||
                n.Contains("blue_bot_outer") ||
                n.Contains("blue_top_inner") ||
                n.Contains("blue_bot_inner");

            bool doNotHideNewModels =
                n.Contains("pf_red_fallen") ||
                n.Contains("pf_blue_celestial") ||
                n.Contains("blue_celestial_tower_visual") ||
                n.Contains("red_fallen_tower_visual") ||
                n.Contains("aog_final") ||
                n.Contains("generated_blue") ||
                n.Contains("generated_tower");

            if (isOldGuardianVisual && !doNotHideNewModels)
            {
                renderer.enabled = false;
                hiddenCount++;
            }
        }

        Debug.Log("Old guardian tower visual renderers hidden: " + hiddenCount);
    }

    [ContextMenu("Show Old Guardian Tower Visuals")]
    public void ShowOldGuardianTowerVisuals()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int shownCount = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string n = renderer.gameObject.name.ToLower();

            bool isOldGuardianVisual =
                n.Contains("guardian_body") ||
                n.Contains("guardian_form") ||
                n.Contains("guardian_wing") ||
                n.Contains("guardian_halo") ||
                n.Contains("guardian_weapon") ||
                n.Contains("final_guardian") ||
                n.Contains("outer_guardian") ||
                n.Contains("inner_guardian") ||

                n.Contains("small_flame") ||
                n.Contains("flame") ||
                n.Contains("glow") ||
                n.Contains("attack_energy_core") ||
                n.Contains("energy_core") ||
                n.Contains("energy_head") ||
                n.Contains("wing_l") ||
                n.Contains("wing_r") ||
                n.Contains("small_gold_rune") ||
                n.Contains("gold_rune") ||

                n.Contains("red_top_outer_final") ||
                n.Contains("red_bot_outer") ||
                n.Contains("red_top_inner") ||
                n.Contains("red_bot_inner") ||

                n.Contains("blue_top_outer") ||
                n.Contains("blue_bot_outer") ||
                n.Contains("blue_top_inner") ||
                n.Contains("blue_bot_inner");

            bool doNotShowNewModels =
                n.Contains("pf_red_fallen") ||
                n.Contains("pf_blue_celestial") ||
                n.Contains("blue_celestial_tower_visual") ||
                n.Contains("red_fallen_tower_visual") ||
                n.Contains("aog_final") ||
                n.Contains("generated_blue") ||
                n.Contains("generated_tower");

            if (isOldGuardianVisual && !doNotShowNewModels)
            {
                renderer.enabled = true;
                shownCount++;
            }
        }

        Debug.Log("Old guardian tower visual renderers shown: " + shownCount);
    }
}