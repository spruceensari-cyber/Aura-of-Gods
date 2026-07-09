using UnityEngine;
using UnityEngine.Rendering;

public class AOGWorldHealthBar : MonoBehaviour
{
    [Header("Bar Settings")]
    public Vector3 barOffset = new Vector3(0f, 3f, 0f);
    public float barWidth = 2f;
    public float barHeight = 0.16f;

    [Header("Compatibility With Old Scripts")]
    public Transform target;
    public float heightOffset = 3f;
    public float width = 2f;

    private Transform barRootTransform;
    private Transform fillTransform;
    private Transform damageGhostTransform;

    private Minion minionTarget;
    private TowerHealth towerTarget;
    private AOGCharacterStats heroTarget;

    private bool isHidden;
    private float displayedRatio = 1f;
    private float ghostRatio = 1f;

    private void Start()
    {
        if (target == null)
            target = transform;

        if (barOffset == Vector3.zero)
            barOffset = new Vector3(0f, heightOffset, 0f);

        if (barWidth <= 0f)
            barWidth = width;

        minionTarget = GetComponent<Minion>();
        towerTarget = GetComponent<TowerHealth>();
        heroTarget = GetComponent<AOGCharacterStats>();

        if (heroTarget != null)
        {
            barWidth = Mathf.Max(barWidth, 2.45f);
            barHeight = Mathf.Max(barHeight, 0.20f);
            barOffset.y = Mathf.Max(barOffset.y, 3.1f);
        }

        BuildHealthBar();
        displayedRatio = ghostRatio = GetHealthRatio();
    }

    private void LateUpdate()
    {
        if (barRootTransform == null || fillTransform == null || isHidden)
            return;

        float targetRatio = GetHealthRatio();
        displayedRatio = Mathf.MoveTowards(displayedRatio, targetRatio, Time.deltaTime * 3.8f);
        ghostRatio = Mathf.MoveTowards(ghostRatio, targetRatio, Time.deltaTime * 0.65f);

        ApplyRatio(fillTransform, displayedRatio, -0.018f);
        if (damageGhostTransform != null)
            ApplyRatio(damageGhostTransform, ghostRatio, -0.010f);

        Camera camera = Camera.main;
        if (camera != null)
            barRootTransform.rotation = camera.transform.rotation;
    }

    private static void ApplyRatio(Transform bar, float ratio, float z)
    {
        ratio = Mathf.Clamp01(ratio);
        bar.localScale = new Vector3(ratio, bar.localScale.y, bar.localScale.z);
        bar.localPosition = new Vector3(-(1f - ratio) * 0.5f, 0f, z);
    }

    private void BuildHealthBar()
    {
        if (barRootTransform != null)
            return;

        GameObject rootObj = new GameObject("AOG_HP_Bar");
        rootObj.transform.SetParent(transform);
        rootObj.transform.localPosition = barOffset;
        rootObj.transform.localRotation = Quaternion.identity;
        rootObj.transform.localScale = Vector3.one;
        barRootTransform = rootObj.transform;

        GameObject border = CreateBarCube("HP_BORDER", barRootTransform, new Vector3(barWidth + 0.16f, barHeight + 0.13f, 0.055f), new Color(0.02f, 0.025f, 0.035f, 1f), 0f);
        GameObject bg = CreateBarCube("HP_BG", border.transform, new Vector3(0.94f, 0.56f, 0.8f), new Color(0.055f, 0.07f, 0.075f, 1f), -0.006f);

        GameObject ghost = CreateBarCube("HP_DAMAGE_GHOST", bg.transform, new Vector3(1f, 0.78f, 0.75f), new Color(0.92f, 0.72f, 0.19f, 1f), -0.010f);
        damageGhostTransform = ghost.transform;

        GameObject fill = CreateBarCube("HP_FILL", bg.transform, new Vector3(1f, 0.78f, 0.68f), GetTeamColor(), -0.018f);
        fillTransform = fill.transform;

        CreateSegments(border.transform);
    }

    private GameObject CreateBarCube(string name, Transform parent, Vector3 scale, Color color, float z)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = new Vector3(0f, 0f, z);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = scale;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateUnlitMaterial(name + "_MAT", color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        return obj;
    }

    private void CreateSegments(Transform parent)
    {
        int segmentCount = heroTarget != null ? 10 : 5;
        for (int i = 1; i < segmentCount; i++)
        {
            float x = Mathf.Lerp(-barWidth * 0.45f, barWidth * 0.45f, i / (float)segmentCount);
            GameObject segment = CreateBarCube("HP_SEGMENT_" + i, parent, new Vector3(0.018f, barHeight * 0.58f, 0.035f), new Color(0f, 0f, 0f, 0.55f), -0.028f);
            segment.transform.localPosition = new Vector3(x, 0f, -0.028f);
        }
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        return material;
    }

    public void Refresh()
    {
        if (barRootTransform == null)
            BuildHealthBar();

        isHidden = false;
        if (barRootTransform != null)
            barRootTransform.gameObject.SetActive(true);
    }

    public void Hide()
    {
        isHidden = true;
        if (barRootTransform != null)
            barRootTransform.gameObject.SetActive(false);
    }

    private float GetHealthRatio()
    {
        if (minionTarget != null)
            return Mathf.Clamp01(minionTarget.hp / Mathf.Max(1f, minionTarget.maxHp));

        if (towerTarget != null)
            return Mathf.Clamp01(towerTarget.hp / Mathf.Max(1f, towerTarget.maxHp));

        if (heroTarget != null)
            return Mathf.Clamp01(heroTarget.hp / Mathf.Max(1f, heroTarget.maxHp));

        return 1f;
    }

    private Color GetTeamColor()
    {
        if (heroTarget != null)
            return heroTarget.team == MinionTeam.Blue ? new Color(0.17f, 0.88f, 0.38f, 1f) : new Color(0.96f, 0.20f, 0.22f, 1f);

        if (minionTarget != null)
            return minionTarget.team == MinionTeam.Blue ? new Color(0.16f, 0.62f, 1f, 1f) : new Color(0.96f, 0.22f, 0.25f, 1f);

        if (towerTarget != null)
            return towerTarget.towerTeam == MinionTeam.Blue ? new Color(0.16f, 0.62f, 1f, 1f) : new Color(0.96f, 0.22f, 0.25f, 1f);

        return new Color(0.72f, 0.32f, 1f, 1f);
    }
}
