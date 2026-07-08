using UnityEngine;
using UnityEngine.SceneManagement;

public class AOGPlayableSceneBootstrap : MonoBehaviour
{
    private const string ManagerName = "AOG_Playable_Scene_Bootstrap";
    private const string PlayerName = "AOG_Selected_Champion_Player";
    private const string ClickGroundName = "AOG_Runtime_Click_Ground";

    private Camera managedCamera;
    private Transform player;
    private bool manageCamera;
    private readonly Vector3 cameraOffset = new Vector3(0f, 42f, -38f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureManager();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManager();
    }

    private static void EnsureManager()
    {
        if (Object.FindAnyObjectByType<AOGPlayableSceneBootstrap>() != null)
            return;

        GameObject manager = new GameObject(ManagerName);
        manager.AddComponent<AOGPlayableSceneBootstrap>();
    }

    private void Start()
    {
        EnsurePlayableScene();
    }

    private void LateUpdate()
    {
        if (!manageCamera || managedCamera == null || player == null)
            return;

        Vector3 targetPosition = player.position + cameraOffset;
        managedCamera.transform.position = Vector3.Lerp(managedCamera.transform.position, targetPosition, Time.unscaledDeltaTime * 8f);
        managedCamera.transform.rotation = Quaternion.LookRotation((player.position - managedCamera.transform.position).normalized, Vector3.up);
    }

    private void EnsurePlayableScene()
    {
        AOGChampionDefinition champion = AOGChampionCatalog.GetSelectedOrDefault();
        GameObject playerObject = AOGChampionVisualApplier.FindPlayerObject();

        if (playerObject == null)
            playerObject = CreatePlayerProxy(champion);

        player = playerObject.transform;
        EnsureClickGround();
        ConfigurePlayer(playerObject, champion);
        ConfigureCamera(playerObject);
    }

    private GameObject CreatePlayerProxy(AOGChampionDefinition champion)
    {
        GameObject root = new GameObject(PlayerName);
        root.transform.position = FindSpawnPosition();

        try
        {
            root.tag = "Player";
        }
        catch
        {
            // Player tag exists in default Unity projects. If it was removed, the component search still works.
        }

        Material bodyMaterial = NewMaterial(Color.Lerp(new Color(0.06f, 0.065f, 0.075f), champion.accent, 0.32f), champion.accent * 0.25f);
        Material accentMaterial = NewMaterial(champion.accent, champion.accent * 0.7f);
        Material darkMaterial = NewMaterial(new Color(0.015f, 0.015f, 0.018f), champion.accent * 0.15f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Champion_Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        body.transform.localScale = new Vector3(0.9f, 1.25f, 0.9f);
        body.GetComponent<Renderer>().material = bodyMaterial;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Champion_Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 2.65f, 0f);
        head.transform.localScale = new Vector3(0.58f, 0.58f, 0.58f);
        head.GetComponent<Renderer>().material = bodyMaterial;

        GameObject cloak = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloak.name = "Champion_Cloak";
        cloak.transform.SetParent(root.transform, false);
        cloak.transform.localPosition = new Vector3(0f, 1.35f, -0.52f);
        cloak.transform.localScale = new Vector3(1.05f, 1.95f, 0.08f);
        cloak.GetComponent<Renderer>().material = darkMaterial;

        GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weapon.name = "Champion_Weapon";
        weapon.transform.SetParent(root.transform, false);
        weapon.transform.localPosition = new Vector3(0.86f, 1.45f, 0.28f);
        weapon.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        weapon.transform.localScale = new Vector3(0.12f, 2.05f, 0.12f);
        weapon.GetComponent<Renderer>().material = accentMaterial;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Champion_Selection_Ring";
        ring.transform.SetParent(root.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        ring.transform.localScale = new Vector3(1.45f, 0.035f, 1.45f);
        ring.GetComponent<Renderer>().material = accentMaterial;

        return root;
    }

    private void ConfigurePlayer(GameObject playerObject, AOGChampionDefinition champion)
    {
        AOGCharacterStats stats = playerObject.GetComponent<AOGCharacterStats>();
        if (stats == null)
            stats = playerObject.AddComponent<AOGCharacterStats>();

        stats.maxHp = champion.maxHp;
        stats.hp = champion.maxHp;
        stats.attackDamage = champion.attackDamage;
        stats.attackRange = champion.attackRange;
        stats.moveSpeed = champion.moveSpeed;

        AOGPlayerMOBAController controller = playerObject.GetComponent<AOGPlayerMOBAController>();
        if (controller == null)
            controller = playerObject.AddComponent<AOGPlayerMOBAController>();

        if (managedCamera == null)
            managedCamera = FindOrCreateCamera();

        controller.mainCamera = managedCamera;
        AOGChampionVisualApplier.ApplyToCurrentPlayer(champion);
        AOGProfessionalHUDRuntime.RefreshAll();
    }

    private void ConfigureCamera(GameObject playerObject)
    {
        managedCamera = FindOrCreateCamera();
        bool hasSceneCameraController = Object.FindAnyObjectByType<CameraController>() != null;
        manageCamera = !hasSceneCameraController;

        if (!manageCamera)
            return;

        managedCamera.orthographic = true;
        managedCamera.orthographicSize = 30f;
        managedCamera.nearClipPlane = 0.1f;
        managedCamera.farClipPlane = 500f;
        managedCamera.transform.position = playerObject.transform.position + cameraOffset;
        managedCamera.transform.rotation = Quaternion.LookRotation((playerObject.transform.position - managedCamera.transform.position).normalized, Vector3.up);
    }

    private Camera FindOrCreateCamera()
    {
        Camera camera = Camera.main;
        if (camera != null)
            return camera;

        camera = Object.FindAnyObjectByType<Camera>();
        if (camera != null)
        {
            try
            {
                camera.tag = "MainCamera";
            }
            catch
            {
            }

            return camera;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        camera = cameraObject.AddComponent<Camera>();
        try
        {
            cameraObject.tag = "MainCamera";
        }
        catch
        {
        }

        return camera;
    }

    private void EnsureClickGround()
    {
        if (GameObject.Find(ClickGroundName) != null)
            return;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = ClickGroundName;
        ground.transform.position = new Vector3(0f, -0.08f, 0f);
        ground.transform.localScale = new Vector3(320f, 0.08f, 320f);

        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
            Destroy(renderer);
    }

    private Vector3 FindSpawnPosition()
    {
        Ray ray = new Ray(new Vector3(-82f, 80f, -62f), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 160f))
            return hit.point + Vector3.up * 0.05f;

        return new Vector3(-82f, 0.05f, -62f);
    }

    private Material NewMaterial(Color color, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.color = color;

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        return material;
    }
}
