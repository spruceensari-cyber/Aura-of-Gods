using UnityEngine;
using UnityEngine.InputSystem;

public class LeaguePrototypeBuilder : MonoBehaviour
{
    void Start()
    {
        GameObject oldCamera = GameObject.Find("Main Camera");
        if (oldCamera != null)
        {
            Destroy(oldCamera);
        }

        GameObject map = GameObject.CreatePrimitive(PrimitiveType.Plane);
        map.name = "League_Map";
        map.transform.localScale = new Vector3(12, 1, 12);
        map.GetComponent<Renderer>().material.color = new Color(0.15f, 0.15f, 0.18f);

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Kaelith_Player";
        player.transform.position = new Vector3(0, 1, 0);
        player.GetComponent<Renderer>().material.color = new Color(0.45f, 0f, 0.9f);
        player.AddComponent<PlayerMovement>();
        player.AddComponent<KaelithSkill>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerTeam>();
        GameObject camObj = new GameObject("MOBA_Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();

        CameraFollow follow = camObj.AddComponent<CameraFollow>();
        follow.target = player.transform;

        GameObject lightObj = new GameObject("Sun_Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
    }
}

public class PlayerMovement : MonoBehaviour
{
    public float speed = 6f;

    private Vector3 targetPosition;
    private bool isMoving;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            Camera cam = Camera.main;

            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                targetPosition = hit.point;
                isMoving = true;
            }
        }

        if (isMoving)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.magnitude < 0.1f)
            {
                isMoving = false;
                return;
            }

            direction.Normalize();

            transform.position += direction * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 9, -8);

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(55, 0, 0);
    }
}

public class KaelithSkill : MonoBehaviour
{
    private float nextQ;
    private float nextW;
    private float nextE;
    private float nextR;

    public float qCooldown = 1.5f;
    public float wCooldown = 5f;
    public float eCooldown = 4f;
    public float rCooldown = 12f;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame && Time.time >= nextQ)
        {
            CastQ();
            nextQ = Time.time + qCooldown;
        }

        if (Keyboard.current.wKey.wasPressedThisFrame && Time.time >= nextW)
        {
            CastW();
            nextW = Time.time + wCooldown;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame && Time.time >= nextE)
        {
            CastE();
            nextE = Time.time + eCooldown;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && Time.time >= nextR)
        {
            CastR();
            nextR = Time.time + rCooldown;
        }
    }

    void CastQ()
    {
        GameObject spear = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        spear.name = "Q_Void_Spear";
        spear.transform.position = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
        spear.transform.rotation = transform.rotation * Quaternion.Euler(90, 0, 0);
        spear.transform.localScale = new Vector3(0.2f, 0.2f, 1.7f);
        spear.GetComponent<Renderer>().material.color = new Color(0.7f, 0f, 1f);
        spear.AddComponent<SpearMove>();
    }

    void CastW()
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zone.name = "W_Dark_Dominion";
        zone.transform.position = transform.position;
        zone.transform.localScale = new Vector3(4f, 0.05f, 4f);
        zone.GetComponent<Renderer>().material.color = new Color(0.25f, 0f, 0.45f, 0.6f);
        Destroy(zone, 4f);
    }

    void CastE()
    {
        transform.position += transform.forward * 4f;

        GameObject dashEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dashEffect.name = "E_Eclipse_Rush";
        dashEffect.transform.position = transform.position;
        dashEffect.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
        dashEffect.GetComponent<Renderer>().material.color = new Color(0.8f, 0f, 1f);
        Destroy(dashEffect, 0.5f);
    }

    void CastR()
    {
        GameObject eclipse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eclipse.name = "R_Total_Eclipse";
        eclipse.transform.position = transform.position + Vector3.up * 5f;
        eclipse.transform.localScale = new Vector3(6f, 6f, 6f);
        eclipse.GetComponent<Renderer>().material.color = Color.black;
        Destroy(eclipse, 5f);

        RenderSettings.ambientLight = new Color(0.08f, 0f, 0.12f);
    }
}
public class SpearMove : MonoBehaviour
{
    public float speed = 14f;

    void Start()
    {
        Destroy(gameObject, 2f);
    }

    void Update()
    {
        transform.position += transform.up * speed * Time.deltaTime;
    }
}