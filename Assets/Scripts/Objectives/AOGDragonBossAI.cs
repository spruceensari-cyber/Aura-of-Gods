using System.Collections;
using UnityEngine;

/// <summary>
/// Multi-phase Dragon encounter for bot side.
/// Uses readable telegraphs, area denial and phase escalation instead of a static health sponge.
/// </summary>
public class AOGDragonBossAI : MonoBehaviour
{
    [SerializeField] private float aggroRadius = 18f;
    [SerializeField] private float leashRadius = 28f;
    [SerializeField] private float clawDamage = 55f;
    [SerializeField] private float breathDamage = 32f;
    [SerializeField] private float wingDamage = 28f;
    [SerializeField] private float actionCooldown = 2.4f;

    private CombatUnit unit;
    private Vector3 homePosition;
    private Champion target;
    private float nextActionTime;
    private int phase = 1;
    private bool actionRunning;
    private Transform body;
    private Transform leftWing;
    private Transform rightWing;
    private Transform head;

    public int Phase => phase;
    public Champion CurrentTarget => target;

    public void InitializePresentation()
    {
        if (transform.Find("Dragon_Presentation") != null)
            return;

        body = CreatePart("Dragon_Presentation", PrimitiveType.Capsule, transform, new Vector3(0f, 1.8f, 0f), new Vector3(2.8f, 1.6f, 4.3f), Quaternion.Euler(90f, 0f, 0f));
        head = CreatePart("Dragon_Head", PrimitiveType.Capsule, body, new Vector3(0f, 0f, 2.4f), new Vector3(0.95f, 0.8f, 1.25f), Quaternion.Euler(90f, 0f, 0f));
        leftWing = CreatePart("Dragon_Wing_Left", PrimitiveType.Cube, body, new Vector3(-2.2f, 0.4f, 0f), new Vector3(3.4f, 0.12f, 1.5f), Quaternion.Euler(0f, 0f, -18f));
        rightWing = CreatePart("Dragon_Wing_Right", PrimitiveType.Cube, body, new Vector3(2.2f, 0.4f, 0f), new Vector3(3.4f, 0.12f, 1.5f), Quaternion.Euler(0f, 0f, 18f));
        CreatePart("Dragon_Horn_L", PrimitiveType.Cube, head, new Vector3(-0.42f, 0.35f, 0.55f), new Vector3(0.16f, 0.16f, 0.9f), Quaternion.Euler(-22f, 0f, -12f));
        CreatePart("Dragon_Horn_R", PrimitiveType.Cube, head, new Vector3(0.42f, 0.35f, 0.55f), new Vector3(0.16f, 0.16f, 0.9f), Quaternion.Euler(-22f, 0f, 12f));
    }

    void Awake()
    {
        unit = GetComponent<CombatUnit>();
        homePosition = transform.position;
    }

    void Start()
    {
        InitializePresentation();
    }

    void Update()
    {
        UpdatePhase();
        AnimateIdle();
        AcquireTarget();

        if (target == null || actionRunning)
            return;

        float homeDistance = Vector3.Distance(transform.position, homePosition);
        if (homeDistance > leashRadius)
        {
            transform.position = Vector3.MoveTowards(transform.position, homePosition, Time.deltaTime * 8f);
            return;
        }

        if (Time.time >= nextActionTime)
            StartCoroutine(ChooseAction());
    }

    private void UpdatePhase()
    {
        if (unit == null)
            return;

        float hp = unit.HealthPercent;
        phase = hp <= 0.33f ? 3 : hp <= 0.66f ? 2 : 1;
    }

    private void AnimateIdle()
    {
        float flap = Mathf.Sin(Time.time * (phase + 1.8f)) * (12f + phase * 3f);
        if (leftWing != null)
            leftWing.localRotation = Quaternion.Euler(0f, 0f, -18f - flap);
        if (rightWing != null)
            rightWing.localRotation = Quaternion.Euler(0f, 0f, 18f + flap);
        if (body != null)
            body.localPosition = new Vector3(0f, 1.8f + Mathf.Sin(Time.time * 2f) * 0.12f, 0f);
    }

    private void AcquireTarget()
    {
        Champion[] champions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        Champion best = null;
        float bestDistance = aggroRadius;

        foreach (Champion champion in champions)
        {
            if (champion == null || !champion.IsAlive)
                continue;

            float distance = Vector3.Distance(transform.position, champion.transform.position);
            if (distance < bestDistance)
            {
                best = champion;
                bestDistance = distance;
            }
        }

        target = best;
    }

    private IEnumerator ChooseAction()
    {
        actionRunning = true;
        float distance = Vector3.Distance(transform.position, target.transform.position);
        int roll = Random.Range(0, 100);

        if (distance <= 4.5f && roll < 40)
            yield return WingBlast();
        else if (roll < 78)
            yield return FlameBreath();
        else
            yield return SkyDive();

        nextActionTime = Time.time + Mathf.Max(0.9f, actionCooldown - phase * 0.25f);
        actionRunning = false;
    }

    private IEnumerator WingBlast()
    {
        Vector3 center = transform.position;
        CreateTelegraph(center, 5.8f, 0.65f);
        yield return new WaitForSeconds(0.65f);
        DamageArea(center, 5.8f, wingDamage + phase * 8f);

        Collider[] hits = Physics.OverlapSphere(center, 5.8f);
        foreach (Collider hit in hits)
        {
            Champion champion = hit.GetComponentInParent<Champion>();
            if (champion != null)
            {
                Vector3 push = (champion.transform.position - center).normalized;
                champion.transform.position += push * (1.6f + phase * 0.5f);
            }
        }
    }

    private IEnumerator FlameBreath()
    {
        if (target == null)
            yield break;

        Vector3 origin = transform.position + transform.forward * 2f;
        Vector3 direction = (target.transform.position - origin).normalized;
        CreateLineTelegraph(origin, direction, 12f, 1.8f, 0.85f);
        yield return new WaitForSeconds(0.85f);

        RaycastHit[] hits = Physics.SphereCastAll(origin, 1.4f, direction, 12f);
        foreach (RaycastHit hit in hits)
        {
            Champion champion = hit.collider.GetComponentInParent<Champion>();
            if (champion != null)
                champion.TakeDamage(breathDamage + phase * 10f, DamageType.Magical);
        }
    }

    private IEnumerator SkyDive()
    {
        if (target == null)
            yield break;

        Vector3 destination = target.transform.position;
        CreateTelegraph(destination, 4.8f, 1.1f);
        Vector3 start = transform.position;
        float elapsed = 0f;
        float duration = 1.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float arc = Mathf.Sin(t * Mathf.PI) * 8f;
            Vector3 pos = Vector3.Lerp(start, destination, t);
            pos.y += arc;
            transform.position = pos;
            yield return null;
        }

        transform.position = destination;
        DamageArea(destination, 4.8f, clawDamage + phase * 14f);
    }

    private void DamageArea(Vector3 center, float radius, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (Collider hit in hits)
        {
            Champion champion = hit.GetComponentInParent<Champion>();
            if (champion != null)
                champion.TakeDamage(damage, DamageType.Magical);
        }
    }

    private void CreateTelegraph(Vector3 center, float radius, float lifetime)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Dragon_Telegraph";
        marker.transform.position = center + Vector3.up * 0.06f;
        marker.transform.localScale = new Vector3(radius, 0.03f, radius);
        Destroy(marker.GetComponent<Collider>());
        StartCoroutine(FadeMarker(marker, lifetime));
    }

    private void CreateLineTelegraph(Vector3 origin, Vector3 direction, float length, float width, float lifetime)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Dragon_Breath_Telegraph";
        marker.transform.position = origin + direction * (length * 0.5f) + Vector3.up * 0.08f;
        marker.transform.rotation = Quaternion.LookRotation(direction);
        marker.transform.localScale = new Vector3(width, 0.06f, length);
        Destroy(marker.GetComponent<Collider>());
        StartCoroutine(FadeMarker(marker, lifetime));
    }

    private IEnumerator FadeMarker(GameObject marker, float lifetime)
    {
        Renderer renderer = marker.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        mat.color = new Color(1f, 0.18f, 0.05f, 0.55f);
        renderer.material = mat;

        yield return new WaitForSeconds(lifetime);
        if (marker != null)
            Destroy(marker);
    }

    private Transform CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Quaternion rotation)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        obj.transform.localRotation = rotation;
        Collider col = obj.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return obj.transform;
    }
}
