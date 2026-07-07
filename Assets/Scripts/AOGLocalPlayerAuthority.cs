using UnityEngine;

public class AOGLocalPlayerAuthority : MonoBehaviour
{
    [Header("Local Player")]
    public bool isLocalPlayer = false;

    [Header("Debug")]
    [SerializeField] private AOGPlayerMOBAController movementController;
    [SerializeField] private RagnarSkillSet ragnarSkillSet;
    [SerializeField] private RagnarAimController ragnarAimController;
    [SerializeField] private LyraSkillSet lyraSkillSet;

    private void Awake()
    {
        FindComponents();
        ApplyAuthority();
    }

    private void Start()
    {
        FindComponents();
        ApplyAuthority();
    }

    private void FindComponents()
    {
        if (movementController == null)
            movementController = GetComponent<AOGPlayerMOBAController>();

        if (ragnarSkillSet == null)
            ragnarSkillSet = GetComponent<RagnarSkillSet>();

        if (ragnarAimController == null)
            ragnarAimController = GetComponent<RagnarAimController>();

        if (lyraSkillSet == null)
            lyraSkillSet = GetComponent<LyraSkillSet>();
    }

    [ContextMenu("Apply Authority")]
    public void ApplyAuthority()
    {
        FindComponents();

        // Hareket kontrolü
        if (movementController != null)
            movementController.enabled = isLocalPlayer;

        // Ragnar input ve skill kontrolü
        if (ragnarSkillSet != null)
            ragnarSkillSet.enabled = isLocalPlayer;

        if (ragnarAimController != null)
            ragnarAimController.enabled = isLocalPlayer;

        // Lyra input ve skill kontrolü
        if (lyraSkillSet != null)
            lyraSkillSet.enabled = isLocalPlayer;

        Debug.Log(
            gameObject.name +
            " Authority = " +
            (isLocalPlayer ? "LOCAL PLAYER" : "REMOTE / AI")
        );
    }

    public void SetLocalPlayer(bool value)
    {
        isLocalPlayer = value;
        ApplyAuthority();
    }
}