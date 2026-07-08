using UnityEngine;
using System.Collections;

/// <summary>
/// Boss objective spawning - Dragon & Baron with timers
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    [SerializeField] private float dragonRespawnTime = 300f; // 5 minutes
    [SerializeField] private float baronRespawnTime = 420f; // 7 minutes
    [SerializeField] private Transform dragonPitPosition;
    [SerializeField] private Transform baronPitPosition;
    
    private GameObject dragonNPC;
    private GameObject baronNPC;
    private bool dragonAlive = true;
    private bool baronAlive = true;
    
    void Start()
    {
        SpawnDragon();
        SpawnBaron();
    }
    
    private void SpawnDragon()
    {
        GameObject dragon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dragon.name = "Dragon_Objective";
        dragon.transform.position = dragonPitPosition.position;
        dragon.transform.localScale = Vector3.one * 5f;
        dragon.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);
        
        CombatUnit unit = dragon.AddComponent<CombatUnit>();
        unit.baseHealth = 3000f;
        
        dragonNPC = dragon;
        dragonAlive = true;
    }
    
    private void SpawnBaron()
    {
        GameObject baron = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        baron.name = "Baron_Objective";
        baron.transform.position = baronPitPosition.position;
        baron.transform.localScale = new Vector3(3f, 8f, 3f);
        baron.GetComponent<Renderer>().material.color = new Color(0.5f, 0f, 1f);
        
        CombatUnit unit = baron.AddComponent<CombatUnit>();
        unit.baseHealth = 5000f;
        
        baronNPC = baron;
        baronAlive = true;
    }
    
    public void OnDragonKilled()
    {
        dragonAlive = false;
        NetworkManager.Instance.OnStateUpdate?.Invoke(new GameStateSnapshot());
        StartCoroutine(RespawnDragonAfterDelay());
    }
    
    public void OnBaronKilled()
    {
        baronAlive = false;
        NetworkManager.Instance.OnStateUpdate?.Invoke(new GameStateSnapshot());
        StartCoroutine(RespawnBaronAfterDelay());
    }
    
    private IEnumerator RespawnDragonAfterDelay()
    {
        if (dragonNPC != null)
            Destroy(dragonNPC);
        
        yield return new WaitForSeconds(dragonRespawnTime);
        SpawnDragon();
    }
    
    private IEnumerator RespawnBaronAfterDelay()
    {
        if (baronNPC != null)
            Destroy(baronNPC);
        
        yield return new WaitForSeconds(baronRespawnTime);
        SpawnBaron();
    }
}
