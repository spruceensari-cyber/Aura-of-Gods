using UnityEngine;

// Compatibility adapter for older scenes. Character motion is now handled by Animator clips
// through ChampionPresentationController; this component never moves or tilts the character transform.
public class AOGSimpleCombatAnimator : MonoBehaviour
{
    private ChampionPresentationController presentation;

    private void Awake()
    {
        presentation = GetComponent<ChampionPresentationController>();
        if (presentation == null)
            presentation = GetComponentInParent<ChampionPresentationController>();
    }

    public void PlayAttack()
    {
        presentation?.PlayBasicAttack();
    }
}
