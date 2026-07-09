using System.Collections;
using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public float attackRange = 20f;
    public float attackDamage = 42f;
    public float attackRate = 1.15f;

    [Header("Projectile Visual")]
    public float projectileSize = 0.42f;
    public float projectileSpeed = 24f;
    public float shootHeight = 6.5f;
    public float chargeTime = 0.26f;

    private TowerHealth towerHealth;
    private float nextAttackTime;
    private Transform targetTransform;
    private Minion targetMinion;
    private AOGCharacterStats targetHero;
    private Coroutine attackRoutine;
    private Transform visualCore;
    private Vector3 visualCoreBaseScale;

    private void Start()
    {
        towerHealth = GetComponent<TowerHealth>();
        if (towerHealth == null)
        {
            towerHealth = gameObject.AddComponent<TowerHealth>();
            AutoAssignTeam();
        }

        BuildTowerPresentation();
    }

    private void Update()
    {
        if (towerHealth == null || towerHealth.hp <= 0f)
            return;

        AnimateCore();

        if (attackRoutine != null || Time.time < nextAttackTime)
            return;

        AcquireTarget();
        if (targetTransform == null)
            return;

        nextAttackTime = Time.time + attackRate;
        attackRoutine = StartCoroutine(ChargeAndFire());
    }

    private void AutoAssignTeam()
    {
        string n = gameObject.name.ToLowerInvariant();
        if (n.StartsWith("blue_")) towerHealth.towerTeam = MinionTeam.Blue;
        else if (n.StartsWith("red_")) towerHealth.towerTeam = MinionTeam.Red;
    }

    private void AcquireTarget()
    {
        targetTransform = null;
        targetMinion = null;
        targetHero = null;

        Minion closestMinion = null;
        float closestMinionDistance = Mathf.Infinity;
        foreach (Minion minion in Minion.Active)
        {
            if (minion == null || minion.hp <= 0f || minion.team == towerHealth.towerTeam)
                continue;

            float distance = FlatDistance(transform.position, minion.transform.position);
            if (distance <= attackRange && distance < closestMinionDistance)
            {
                closestMinion = minion;
                closestMinionDistance = distance;
            }
        }

        if (closestMinion != null)
        {
            targetMinion = closestMinion;
            targetTransform = closestMinion.transform;
            return;
        }

        AOGActiveChampion current = AOGActiveChampion.Current;
        if (current != null && current.gameObject.activeInHierarchy)
        {
            AOGCharacterStats hero = current.GetComponent<AOGCharacterStats>();
            if (hero != null && !hero.IsDead && hero.team != towerHealth.towerTeam)
            {
                float distance = FlatDistance(transform.position, hero.transform.position);
                if (distance <= attackRange)
                {
                    targetHero = hero;
                    targetTransform = hero.transform;
                }
            }
        }
    }

    private IEnumerator ChargeAndFire()
    {
        Transform lockedTransform = targetTransform;
        Minion lockedMinion = targetMinion;
        AOGCharacterStats lockedHero = targetHero;

        if (lockedTransform == null)
        {
            attackRoutine = null;
            yield break;
        }

        Color teamColor = TeamColor();
        Vector3 start = transform.position + Vector3.up * shootHeight;
        GameObject chargeRing = AOGAbilityVisuals.CreateRing("Tower_Charge", start, 0.65f, teamColor, 0.08f);

        float elapsed = 0f;
        while (elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            if (chargeRing != null)
            {
                float pulse = Mathf.Lerp(1.4f, 0.35f, elapsed / chargeTime);
                chargeRing.transform.localScale = Vector3.one * pulse;
            }
            yield return null;
        }

        if (chargeRing != null)
            Destroy(chargeRing);

        if (lockedTransform != null && lockedTransform.gameObject.activeInHierarchy)
            ShootProjectile(lockedTransform, lockedMinion, lockedHero, teamColor);

        attackRoutine = null;
    }

    private void ShootProjectile(Transform target, Minion minion, AOGCharacterStats hero, Color teamColor)
    {
        Vector3 spawnPos = transform.position + Vector3.up * shootHeight;
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = towerHealth.towerTeam + "_Tower_Energy_Bolt";
        projectile.transform.position = spawnPos;
        projectile.transform.localScale = Vector3.one * projectileSize;

        Collider col = projectile.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color = teamColor };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", teamColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", teamColor * 5f);
        }
        projectile.GetComponent<Renderer>().sharedMaterial = material;

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = 0.32f;
        trail.startWidth = projectileSize * 1.25f;
        trail.endWidth = 0f;
        trail.sharedMaterial = material;
        trail.startColor = teamColor;
        trail.endColor = new Color(teamColor.r, teamColor.g, teamColor.b, 0f);

        TowerBolt bolt = projectile.AddComponent<TowerBolt>();
        bolt.targetTransform = target;
        bolt.minionTarget = minion;
        bolt.heroTarget = hero;
        bolt.damage = hero != null ? attackDamage * 1.15f : attackDamage;
        bolt.speed = projectileSpeed;
        bolt.color = teamColor;
    }

    private void BuildTowerPresentation()
    {
        if (transform.Find("AOG_Tower_Energy_Core") != null)
            return;

        Color teamColor = TeamColor();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material energy = new Material(shader) { color = teamColor };
        if (energy.HasProperty("_BaseColor")) energy.SetColor("_BaseColor", teamColor);
        if (energy.HasProperty("_EmissionColor"))
        {
            energy.EnableKeyword("_EMISSION");
            energy.SetColor("_EmissionColor", teamColor * 4f);
        }

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "AOG_Tower_Energy_Core";
        core.transform.SetParent(transform, false);
        core.transform.localPosition = new Vector3(0f, shootHeight - 0.55f, 0f);
        core.transform.localScale = Vector3.one * 0.72f;
        core.GetComponent<Renderer>().sharedMaterial = energy;
        Collider col = core.GetComponent<Collider>();
        if (col != null) Destroy(col);
        visualCore = core.transform;
        visualCoreBaseScale = core.transform.localScale;

        GameObject orbitObject = new GameObject("AOG_Tower_Orbit");
        orbitObject.transform.SetParent(transform, false);
        orbitObject.transform.localPosition = core.transform.localPosition;
        LineRenderer ring = orbitObject.AddComponent<LineRenderer>();
        ring.loop = true;
        ring.useWorldSpace = false;
        ring.positionCount = 48;
        ring.startWidth = 0.055f;
        ring.endWidth = 0.055f;
        ring.sharedMaterial = energy;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.0f, Mathf.Sin(a) * 0.18f, Mathf.Sin(a) * 1.0f));
        }
        AOGOrbitAnimator orbit = orbitObject.AddComponent<AOGOrbitAnimator>();
        orbit.speed = towerHealth.towerTeam == MinionTeam.Blue ? 22f : -22f;
    }

    private void AnimateCore()
    {
        if (visualCore == null)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * 3.6f) * 0.08f;
        visualCore.localScale = visualCoreBaseScale * pulse;
    }

    private Color TeamColor()
    {
        return towerHealth != null && towerHealth.towerTeam == MinionTeam.Blue
            ? new Color(0.18f, 0.62f, 1f, 1f)
            : new Color(1f, 0.18f, 0.22f, 1f);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
