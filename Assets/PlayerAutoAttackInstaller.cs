using UnityEngine;

public class PlayerAutoAttackInstaller : MonoBehaviour
{
    void Update()
    {
        GameObject player = GameObject.Find("Kaelith_Player");

        if (player == null)
        {
            return;
        }

        PlayerAutoAttack autoAttack = player.GetComponent<PlayerAutoAttack>();

        if (autoAttack == null)
        {
            autoAttack = player.AddComponent<PlayerAutoAttack>();

            autoAttack.attackRange = 5f;
            autoAttack.attackDamage = 20f;
            autoAttack.attackCooldown = 1f;

            Debug.Log("PlayerAutoAttack Kaelith_Player'a eklendi.");
        }

        // Bir kere ekledikten sonra bu installer artık çalışmasın
        enabled = false;
    }
}