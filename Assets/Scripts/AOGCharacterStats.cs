using System.Collections;
using UnityEngine;

public class AOGCharacterStats : MonoBehaviour
{
    public MinionTeam team = MinionTeam.Blue;

    [Header("Health")]
    public float maxHp = 600f;
    public float hp = 600f;
    [Min(0f)] public float deathPresentationDuration = 2.4f;

    [Header("Combat")]
    public float attackDamage = 45f;
    public float attackRange = 4f;
    public float attackCooldown = 1.1f;

    [Header("Movement")]
    public float moveSpeed = 6f;

    private ChampionPresentationController presentation;
    private bool deathStarted;

    public bool IsDead => hp <= 0f;

    private void Start()
    {
        if (hp <= 0f)
            hp = maxHp;

        presentation = GetComponent<ChampionPresentationController>();
    }

    public void TakeDamage(float amount)
    {
        if (deathStarted || amount <= 0f)
            return;

        hp = Mathf.Clamp(hp - amount, 0f, maxHp);

        if (hp > 0f)
        {
            presentation?.PlayHitReaction();
            return;
        }

        Die();
    }

    private void Die()
    {
        if (deathStarted)
            return;

        deathStarted = true;
        hp = 0f;
        presentation?.PlayDeath();

        AOGPlayerMOBAController controller = GetComponent<AOGPlayerMOBAController>();
        if (controller != null)
            controller.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        StartCoroutine(DisableAfterDeathPresentation());
    }

    private IEnumerator DisableAfterDeathPresentation()
    {
        if (deathPresentationDuration > 0f)
            yield return new WaitForSeconds(deathPresentationDuration);

        gameObject.SetActive(false);
    }
}
