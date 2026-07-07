using UnityEngine;

public class AOGCharacterAutoInstaller : MonoBehaviour
{
    [Header("Default Player Settings")]
    public MinionTeam playerTeam = MinionTeam.Blue;
    public float playerHp = 900f;
    public float playerMoveSpeed = 6f;
    public float playerAttackDamage = 55f;
    public float playerAttackRange = 4.5f;

    [ContextMenu("Setup Selected Player")]
    public void SetupSelectedPlayer()
    {
#if UNITY_EDITOR
        GameObject obj = UnityEditor.Selection.activeGameObject;

        if (obj == null)
        {
            Debug.LogWarning("Önce Hierarchy'de karakter objesini seç.");
            return;
        }

        SetupCharacter(obj);
#endif
    }

    [ContextMenu("Setup All Player Named Characters")]
    public void SetupAllPlayerNamedCharacters()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        int count = 0;

        foreach (GameObject obj in allObjects)
        {
            string n = obj.name.ToLower();

            bool looksLikePlayer =
                n.Contains("player") ||
                n.Contains("kaelith") ||
                n.Contains("hero") ||
                n.Contains("character");

            if (!looksLikePlayer)
                continue;

            SetupCharacter(obj);
            count++;
        }

        Debug.Log("Karakter setup tamamlandı. Sayı: " + count);
    }

    void SetupCharacter(GameObject obj)
    {
        AOGCharacterStats stats = obj.GetComponent<AOGCharacterStats>();

        if (stats == null)
            stats = obj.AddComponent<AOGCharacterStats>();

        stats.team = playerTeam;
        stats.maxHp = playerHp;
        stats.hp = playerHp;
        stats.moveSpeed = playerMoveSpeed;
        stats.attackDamage = playerAttackDamage;
        stats.attackRange = playerAttackRange;

        AOGPlayerMOBAController controller = obj.GetComponent<AOGPlayerMOBAController>();

        if (controller == null)
            controller = obj.AddComponent<AOGPlayerMOBAController>();

        Collider collider = obj.GetComponent<Collider>();

        if (collider == null)
        {
            CapsuleCollider capsule = obj.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.5f;
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb == null)
            rb = obj.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;

        obj.tag = "Player";

        Debug.Log("Karakter hazırlandı: " + obj.name);
    }
}