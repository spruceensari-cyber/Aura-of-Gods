using UnityEngine;

public class TowerHealthBar : MonoBehaviour
{
    public TowerHealth towerHealth;

    Transform fill;

    void Start()
    {
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.transform.SetParent(transform);
        bg.transform.localPosition = new Vector3(0, 6f, 0);
        bg.transform.localScale = new Vector3(3f, 0.25f, 0.15f);
        bg.GetComponent<Renderer>().material.color = Color.black;

        GameObject hp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hp.transform.SetParent(bg.transform);
        hp.transform.localPosition = Vector3.zero;
        hp.transform.localScale = new Vector3(1f, 0.8f, 0.8f);
        hp.GetComponent<Renderer>().material.color = Color.red;

        fill = hp.transform;
    }

    void Update()
    {
        if (towerHealth == null) return;

        float ratio = towerHealth.currentHealth / towerHealth.maxHealth;
        ratio = Mathf.Clamp01(ratio);

        fill.localScale = new Vector3(ratio, 0.8f, 0.8f);
        fill.localPosition = new Vector3(-(1f - ratio) / 2f, 0, 0);
    }

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}