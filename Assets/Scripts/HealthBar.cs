using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public PlayerHealth playerHealth;

    Transform fill;
    Transform bg;

    void Start()
    {
        // BACKGROUND
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
        background.transform.SetParent(transform);

        background.transform.localPosition = new Vector3(0, 3f, 0);
        background.transform.localScale = new Vector3(2.2f, 0.18f, 0.12f);

        background.GetComponent<Renderer>().material.color = Color.black;

        bg = background.transform;

        // HP
        GameObject hp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hp.transform.SetParent(background.transform);

        hp.transform.localPosition = Vector3.zero;
        hp.transform.localScale = new Vector3(1f, 0.8f, 0.8f);

        hp.GetComponent<Renderer>().material.color = Color.red;

        fill = hp.transform;
    }

    void Update()
    {
        if (playerHealth == null) return;

        float ratio =
            (float)playerHealth.currentHealth /
            playerHealth.maxHealth;

        ratio = Mathf.Clamp01(ratio);

        fill.localScale = new Vector3(ratio, 0.8f, 0.8f);

        fill.localPosition =
            new Vector3(-(1f - ratio) / 2f, 0, 0);
    }

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.rotation =
                Camera.main.transform.rotation;
        }
    }
}