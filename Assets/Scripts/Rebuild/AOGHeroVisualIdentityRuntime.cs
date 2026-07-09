using UnityEngine;

/// <summary>
/// Applies a non-destructive team/hero visual identity using MaterialPropertyBlock.
/// Keeps authored meshes and textures while adding readable accent/emission language.
/// </summary>
public class AOGHeroVisualIdentityRuntime : MonoBehaviour
{
    private AOGOriginalHeroId heroId;
    private Renderer[] renderers;
    private MaterialPropertyBlock block;

    public void Initialize(AOGOriginalHeroId id)
    {
        heroId = id;
        renderers = GetComponentsInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
        Apply();
    }

    private void Apply()
    {
        if (renderers == null) return;
        Color accent = heroId switch
        {
            AOGOriginalHeroId.SorynPrismHuntress => new Color(0.10f, 0.72f, 1f, 1f),
            AOGOriginalHeroId.CaelixRiftVanguard => new Color(1f, 0.16f, 0.06f, 1f),
            _ => new Color(0.42f, 0.24f, 1f, 1f)
        };

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", accent * 0.55f);
            renderer.SetPropertyBlock(block);
        }
    }
}
