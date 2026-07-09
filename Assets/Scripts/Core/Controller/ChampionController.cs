using UnityEngine;

/// <summary>
/// MOBA-style controller for the core Champion architecture.
/// Gameplay remains authoritative here while ChampionPresentationController owns animation/audio feedback.
/// </summary>
public class ChampionController : MonoBehaviour
{
    private Champion champion;
    private Rigidbody rb;
    private Camera mainCamera;
    private ChampionPresentationController presentation;
    private Vector3 moveTarget;
    private bool isMoving;

    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float turnSpeed = 10f;

    private readonly ChampionAbility[] abilities = new ChampionAbility[4];

    private void Start()
    {
        champion = GetComponent<Champion>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        presentation = GetComponent<ChampionPresentationController>();

        ChampionAbility[] allAbilities = GetComponents<ChampionAbility>();
        for (int i = 0; i < Mathf.Min(allAbilities.Length, abilities.Length); i++)
            abilities[i] = allAbilities[i];
    }

    private void Update()
    {
        HandleMovement();
        HandleAbilities();
        HandleAttacks();

        if (presentation != null && rb != null)
            presentation.SetPlanarVelocity(rb.velocity);
    }

    private void HandleMovement()
    {
        if (rb == null)
            return;

        if (Input.GetMouseButtonDown(1) && mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                moveTarget = hit.point;
                isMoving = true;
            }
        }

        if (!isMoving)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        Vector3 direction = moveTarget - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= stoppingDistance)
        {
            isMoving = false;
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        direction.Normalize();
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
        rb.velocity = new Vector3(direction.x * 5f, rb.velocity.y, direction.z * 5f);
    }

    private void HandleAbilities()
    {
        if (Input.GetKeyDown(KeyCode.Q) && abilities[0] != null) CastAbility(0);
        if (Input.GetKeyDown(KeyCode.W) && abilities[1] != null) CastAbility(1);
        if (Input.GetKeyDown(KeyCode.E) && abilities[2] != null) CastAbility(2);
        if (Input.GetKeyDown(KeyCode.R) && abilities[3] != null) CastAbility(3);
    }

    private void CastAbility(int abilityIndex)
    {
        ChampionAbility ability = abilities[abilityIndex];
        if (ability == null)
            return;

        Vector3 targetPos = transform.position + transform.forward * 10f;
        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
                targetPos = hit.point;
        }

        presentation?.PlayAbility(abilityIndex);
        ability.Cast(targetPos, null);
    }

    private void HandleAttacks()
    {
        if (!Input.GetMouseButtonDown(0) || mainCamera == null || champion == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Champion target = hit.collider.GetComponentInParent<Champion>();
        if (target == null || target.Team == champion.Team)
            return;

        moveTarget = hit.point;
        isMoving = true;
        presentation?.PlayBasicAttack();
    }
}
