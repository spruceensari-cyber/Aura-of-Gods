using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Professional champion controller with MOBA-style input handling
/// Left-click to move, right-click to attack, Q/W/E/R abilities
/// </summary>
public class ChampionController : MonoBehaviour
{
    private Champion champion;
    private Rigidbody rb;
    private Camera mainCamera;
    private Vector3 moveTarget;
    private bool isMoving;
    
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float turnSpeed = 10f;
    
    private ChampionAbility[] abilities = new ChampionAbility[4];
    
    void Start()
    {
        champion = GetComponent<Champion>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        
        // Find all abilities
        ChampionAbility[] allAbilities = GetComponents<ChampionAbility>();
        for (int i = 0; i < Mathf.Min(allAbilities.Length, 4); i++)
        {
            abilities[i] = allAbilities[i];
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleAbilities();
        HandleAttacks();
    }
    
    private void HandleMovement()
    {
        // Right-click to move
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                moveTarget = hit.point;
                isMoving = true;
            }
        }
        
        if (isMoving)
        {
            Vector3 direction = (moveTarget - transform.position).normalized;
            direction.y = 0;
            
            if (direction.magnitude > 0.01f)
            {
                // Rotate towards target
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
                
                // Move
                rb.velocity = new Vector3(direction.x * 5f, rb.velocity.y, direction.z * 5f);
            }
            
            if (Vector3.Distance(transform.position, moveTarget) < stoppingDistance)
            {
                isMoving = false;
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
        else
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }
    
    private void HandleAbilities()
    {
        if (Input.GetKeyDown(KeyCode.Q) && abilities[0] != null)
            CastAbility(0);
        if (Input.GetKeyDown(KeyCode.W) && abilities[1] != null)
            CastAbility(1);
        if (Input.GetKeyDown(KeyCode.E) && abilities[2] != null)
            CastAbility(2);
        if (Input.GetKeyDown(KeyCode.R) && abilities[3] != null)
            CastAbility(3);
    }
    
    private void CastAbility(int abilityIndex)
    {
        if (abilities[abilityIndex] == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPos = transform.position + transform.forward * 10f;
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPos = hit.point;
        }
        
        abilities[abilityIndex].Cast(targetPos, null);
    }
    
    private void HandleAttacks()
    {
        // Left-click for auto-attack
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.TryGetComponent<Champion>(out var target))
                {
                    if (target.Team != champion.Team)
                    {
                        // Attack target
                        moveTarget = hit.point;
                        isMoving = true;
                    }
                }
            }
        }
    }
}
