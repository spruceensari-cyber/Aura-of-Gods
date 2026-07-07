using UnityEngine;

public class AOGHideRootRenderer : MonoBehaviour
{
    void Awake()
    {
        Hide();
    }

    void Start()
    {
        Hide();
    }

    void LateUpdate()
    {
        Hide();
    }

    void Hide()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null && meshRenderer.enabled)
            meshRenderer.enabled = false;

        SkinnedMeshRenderer skinned = GetComponent<SkinnedMeshRenderer>();

        if (skinned != null && skinned.enabled)
            skinned.enabled = false;
    }
}