using UnityEngine;

public class AOGWorldHealthBar : MonoBehaviour
{
    [Header("Bar Settings")]
    public Vector3 barOffset = new Vector3(0f, 3f, 0f);
    public float barWidth = 2f;
    public float barHeight = 0.18f;

    [Header("Compatibility With Old Scripts")]
    public Transform target;
    public float heightOffset = 3f;
    public float width = 2f;

    private Transform barRootTransform;
    private Transform fillTransform;

    private Minion minionTarget;
    private TowerHealth towerTarget;
    private AOGCharacterStats heroTarget;

    private bool isHidden = false;

    void Start()
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

        BuildHealthBar();
    }

    void LateUpdate()
    {
        if (barRootTransform == null || fillTransform == null)
            return;

        if (isHidden)
            return;

        float ratio = GetHealthRatio();

        fillTransform.localScale = new Vector3(ratio, 1f, 1f);
        fillTransform.localPosition = new Vector3(-(1f - ratio) * 0.5f, 0f, -0.01f);

        if (Camera.main != null)
        {
            barRootTransform.rotation = Camera.main.transform.rotation;
        }
    }

    void BuildHealthBar()
    {
        if (barRootTransform != null)
            return;

        GameObject rootObj = new GameObject("AOG_HP_Bar");
        rootObj.transform.SetParent(transform);
        rootObj.transform.localPosition = barOffset;
        rootObj.transform.localRotation = Quaternion.identity;
        rootObj.transform.localScale = Vector3.one;

        barRootTransform = rootObj.transform;

        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bgObj.name = "HP_BG";
        bgObj.transform.SetParent(barRootTransform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localRotation = Quaternion.identity;
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 0.05f);

        Renderer bgRenderer = bgObj.GetComponent<Renderer>();
        if (bgRenderer != null)
            bgRenderer.material.color = Color.black;

        Collider bgCollider = bgObj.GetComponent<Collider>();
        if (bgCollider != null)
            Destroy(bgCollider);

        GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fillObj.name = "HP_FILL";
        fillObj.transform.SetParent(bgObj.transform);
        fillObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        fillObj.transform.localRotation = Quaternion.identity;
        fillObj.transform.localScale = new Vector3(1f, 0.8f, 1f);

        Renderer fillRenderer = fillObj.GetComponent<Renderer>();
        if (fillRenderer != null)
            fillRenderer.material.color = GetTeamColor();

        Collider fillCollider = fillObj.GetComponent<Collider>();
        if (fillCollider != null)
            Destroy(fillCollider);

        fillTransform = fillObj.transform;
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

    float GetHealthRatio()
    {
        if (minionTarget != null)
            return Mathf.Clamp01(minionTarget.hp / minionTarget.maxHp);

        if (towerTarget != null)
            return Mathf.Clamp01(towerTarget.hp / towerTarget.maxHp);

        if (heroTarget != null)
            return Mathf.Clamp01(heroTarget.hp / heroTarget.maxHp);

        return 1f;
    }

    Color GetTeamColor()
    {
        if (minionTarget != null)
            return minionTarget.team == MinionTeam.Blue ? Color.cyan : Color.red;

        if (towerTarget != null)
            return towerTarget.towerTeam == MinionTeam.Blue ? Color.cyan : Color.red;

        if (heroTarget != null)
            return heroTarget.team == MinionTeam.Blue ? Color.cyan : Color.red;

        return Color.magenta;
    }
}