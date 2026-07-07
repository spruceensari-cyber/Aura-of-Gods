using UnityEngine;
using System.Collections;

public class AOGAttackFlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    public Color flashColor = new Color(1f, 0.05f, 0.65f, 1f);
    public float flashDuration = 0.12f;
    public float flashScale = 1.2f;
    public Vector3 offset = new Vector3(0f, 1.2f, 0f);

    private GameObject flashObj;
    private Renderer flashRenderer;

    void Start()
    {
        CreateFlashObject();
    }

    void CreateFlashObject()
    {
        flashObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flashObj.name = "AOG_Attack_Flash";
        flashObj.transform.SetParent(transform);
        flashObj.transform.localPosition = offset;
        flashObj.transform.localScale = Vector3.one * flashScale;

        Collider col = flashObj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        flashRenderer = flashObj.GetComponent<Renderer>();

        if (flashRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = flashColor;
            flashRenderer.material = mat;
        }

        flashObj.SetActive(false);
    }

    public void PlayFlash()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (flashObj == null)
            CreateFlashObject();

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        flashObj.SetActive(true);

        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float t = timer / flashDuration;

            float scale = Mathf.Lerp(flashScale, 0.1f, t);
            flashObj.transform.localScale = Vector3.one * scale;

            if (flashRenderer != null)
            {
                Color c = flashColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                flashRenderer.material.color = c;
            }

            yield return null;
        }

        flashObj.SetActive(false);
        flashObj.transform.localScale = Vector3.one * flashScale;
    }
}