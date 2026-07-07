using UnityEngine;

public class AOGFloatingCombatText : MonoBehaviour
{
    public float lifetime = 0.85f;
    public float riseSpeed = 1.9f;
    public float sideDrift = 0.35f;
    public float startScale = 1.15f;
    public float endScale = 0.85f;

    private TextMesh textMesh;
    private Color startColor;
    private Vector3 velocity;
    private float birthTime;

    public static void SpawnDamage(Vector3 worldPosition, float amount, Color color)
    {
        if (amount <= 0f)
            return;

        GameObject obj = new GameObject("AOG_Damage_Text");
        obj.transform.position = worldPosition + Vector3.up * 1.7f + Random.insideUnitSphere * 0.22f;
        obj.transform.localScale = Vector3.one * 0.9f;

        TextMesh mesh = obj.AddComponent<TextMesh>();
        mesh.text = Mathf.CeilToInt(amount).ToString();
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 48;
        mesh.characterSize = 0.08f;
        mesh.color = color;

        AOGFloatingCombatText text = obj.AddComponent<AOGFloatingCombatText>();
        text.Initialize(mesh, color);
    }

    public static void SpawnHeal(Vector3 worldPosition, float amount)
    {
        SpawnDamage(worldPosition, amount, new Color(0.25f, 1f, 0.45f, 1f));
    }

    private void Initialize(TextMesh mesh, Color color)
    {
        textMesh = mesh;
        startColor = color;
        birthTime = Time.time;

        velocity = new Vector3(
            Random.Range(-sideDrift, sideDrift),
            riseSpeed,
            Random.Range(-sideDrift, sideDrift)
        );
    }

    private void Update()
    {
        float age = Time.time - birthTime;
        float t = Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));

        transform.position += velocity * Time.deltaTime;
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);

        Camera cam = Camera.main;

        if (cam != null)
        {
            Vector3 direction = transform.position - cam.transform.position;

            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        if (textMesh != null)
        {
            Color color = startColor;
            color.a = 1f - t;
            textMesh.color = color;
        }

        if (age >= lifetime)
            Destroy(gameObject);
    }
}
