using System.Collections;
using UnityEngine;

/// <summary>
/// Top-side Medusa boss with gaze direction, petrification zones and anti-burst retaliation.
/// </summary>
public class AOGMedusaBossAI : MonoBehaviour
{
    [SerializeField] private float aggroRadius = 17f;
    [SerializeField] private float gazeRange = 14f;
    [SerializeField] private float gazeHalfAngle = 38f;
    [SerializeField] private float gazeDamage = 38f;
    [SerializeField] private float petrifyDuration = 1.25f;
    [SerializeField] private float burstWindow = 1.6f;
    [SerializeField] private float burstThreshold = 420f;

    private CombatUnit unit;
    private Champion target;
    private float nextActionTime;
    private bool actionRunning;
    private float recentDamage;
    private float burstWindowEnd;
    private float lastHealth;
    private Transform torso;
    private Transform head;
    private Transform snakeRing;

    public Champion CurrentTarget => target;

    void Awake()
    {
        unit = GetComponent<CombatUnit>();
        if (unit != null)
            lastHealth = unit.CurrentHealth;
    }

    void Start()
    {
        BuildPresentation();
        if (unit != null)
            lastHealth = unit.CurrentHealth;
    }

    void Update()
    {
        AnimatePresentation();
        TrackBurstDamage();
        AcquireTarget();

        if (target == null || actionRunning || Time.time < nextActionTime)
            return;

        StartCoroutine(ChooseAction());
    }

    private void TrackBurstDamage()
    {
        if (unit == null)
            return;

        float current = unit.CurrentHealth;
        if (current < lastHealth)
        {
            if (Time.time > burstWindowEnd)
                recentDamage = 0f;

            recentDamage += lastHealth - current;
            burstWindowEnd = Time.time + burstWindow;

            if (recentDamage >= burstThreshold && !actionRunning)
            {
                recentDamage = 0f;
                StartCoroutine(StoneRebuke());
            }
        }

        lastHealth = current;
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
        int roll = Random.Range(0, 100);

        if (roll < 50)
            yield return GazeAttack();
        else if (roll < 78)
            yield return SerpentZones();
        else
            yield return StoneRebuke();

        nextActionTime = Time.time + 2.2f;
        actionRunning = false;
    }

    private IEnumerator GazeAttack()
    {
        if (target == null)
            yield break;

        Vector3 targetDirection = target.transform.position - transform.position;
        targetDirection.y = 0f;
        if (targetDirection.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(targetDirection.normalized);

        CreateConeTelegraph(0.9f);
        yield return new WaitForSeconds(0.9f);

        Champion[] champions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        foreach (Champion champion in champions)
        {
            if (champion == null || !champion.IsAlive)
                continue;

            Vector3 toChampion = champion.transform.position - transform.position;
            float distance = toChampion.magnitude;
            if (distance > gazeRange)
                continue;

            float angle = Vector3.Angle(transform.forward, toChampion.normalized);
            if (angle <= gazeHalfAngle)
            {
                champion.TakeDamage(gazeDamage, DamageType.Magical);
                champion.Stun(petrifyDuration);
            }
        }
    }

    private IEnumerator SerpentZones()
    {
        Champion[] champions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        int spawned = 0;

        foreach (Champion champion in champions)
        {
            if (champion == null || !champion.IsAlive)
                continue;

            StartCoroutine(DelayedStoneZone(champion.transform.position, 0.85f + spawned * 0.12f));
            spawned++;
            if (spawned >= 3)
                break;
        }

        yield return new WaitForSeconds(1.4f);
    }

    private IEnumerator DelayedStoneZone(Vector3 center, float delay)
    {
        GameObject marker = CreateCircleMarker("Medusa_Stone_Zone", center, 3.2f, new Color(0.24f, 0.78f, 0.34f, 0.48f));
        yield return new WaitForSeconds(delay);

        Collider[] hits = Physics.OverlapSphere(center, 3.2f);
        foreach (Collider hit in hits)
        {
            Champion champion = hit.GetComponentInParent<Champion>();
            if (champion != null)
            {
                champion.TakeDamage(46f, DamageType.Magical);
                champion.Stun(0.7f);
            }
        }

        if (marker != null)
            Destroy(marker);
    }

    private IEnumerator StoneRebuke()
    {
        actionRunning = true;
        Vector3 center = transform.position;
        GameObject marker = CreateCircleMarker("Medusa_Rebuke", center, 6.2f, new Color(0.54f, 0.86f, 0.34f, 0.54f));
        yield return new WaitForSeconds(0.75f);

        Collider[] hits = Physics.OverlapSphere(center, 6.2f);
        foreach (Collider hit in hits)
        {
            Champion champion = hit.GetComponentInParent<Champion>();
            if (champion != null)
            {
                champion.TakeDamage(58f, DamageType.Magical);
                champion.Stun(1.0f);
            }
        }

        if (marker != null)
            Destroy(marker);

        actionRunning = false;
        nextActionTime = Time.time + 1.6f;
    }

    private void BuildPresentation()
    {
        if (transform.Find("Medusa_Presentation") != null)
            return;

        torso = CreatePart("Medusa_Presentation", PrimitiveType.Capsule, transform, new Vector3(0f, 1.7f, 0f), new Vector3(1.3f, 1.7f, 1.3f), Quaternion.identity);
        head = CreatePart("Medusa_Head", PrimitiveType.Sphere, torso, new Vector3(0f, 1.35f, 0f), new Vector3(0.8f, 0.8f, 0.8f), Quaternion.identity);
        snakeRing = new GameObject("Medusa_Snake_Crown").transform;
        snakeRing.SetParent(head, false);

        for (int i = 0; i < 10; i++)
        {
            float angle = i * 36f;
            float rad = angle * Mathf.Deg2Rad;
            Transform snake = CreatePart("Snake_" + i, PrimitiveType.Capsule, snakeRing,
                new Vector3(Mathf.Cos(rad) * 0.72f, 0.55f, Mathf.Sin(rad) * 0.72f),
                new Vector3(0.16f, 0.65f, 0.16f),
                Quaternion.Euler(18f, -angle, 28f));
            snake.localRotation *= Quaternion.Euler(0f, 0f, i % 2 == 0 ? 24f : -24f);
        }

        CreatePart("Medusa_Tail", PrimitiveType.Capsule, transform, new Vector3(0f, 0.45f, -0.9f), new Vector3(0.72f, 2.2f, 0.72f), Quaternion.Euler(62f, 0f, 0f));
    }

    private void AnimatePresentation()
    {
        if (torso != null)
            torso.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.3f) * 8f, Mathf.Sin(Time.time * 2.2f) * 2f);
        if (head != null)
            head.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.7f) * 4f, Mathf.Sin(Time.time * 1.1f) * 8f, 0f);
        if (snakeRing != null)
            snakeRing.Rotate(Vector3.up, Time.deltaTime * 24f, Space.Self);
    }

    private void CreateConeTelegraph(float lifetime)
    {
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.name = "Medusa_Gaze_Telegraph";
        cone.transform.position = transform.position + transform.forward * (gazeRange * 0.45f) + Vector3.up * 0.08f;
        cone.transform.localScale = new Vector3(4.8f, 0.03f, gazeRange * 0.45f);
        cone.transform.rotation = transform.rotation;
        Destroy(cone.GetComponent<Collider>());
        StartCoroutine(DestroyAfter(cone, lifetime));
    }

    private GameObject CreateCircleMarker(string name, Vector3 center, float radius, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = name;
        marker.transform.position = center + Vector3.up * 0.06f;
        marker.transform.localScale = new Vector3(radius, 0.03f, radius);
        Destroy(marker.GetComponent<Collider>());
        Renderer renderer = marker.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        mat.color = color;
        renderer.material = mat;
        return marker;
    }

    private IEnumerator DestroyAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            Destroy(obj);
    }

    private Transform CreatePart(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.transform.localRotation = rotation;
        Collider col = obj.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return obj.transform;
    }
}
