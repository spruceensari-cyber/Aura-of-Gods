using UnityEngine;

public class AOGSpectatorCameraRuntime : MonoBehaviour
{
    public bool SpectatorMode { get; private set; }
    public Transform FollowTarget { get; private set; }
    [SerializeField] float freeSpeed = 24f;
    [SerializeField] float followSmooth = 8f;
    [SerializeField] Vector3 followOffset = new Vector3(0f, 30f, -24f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGSpectatorCameraRuntime>() != null) return;
        GameObject obj = new GameObject("AOG_Spectator_Camera_Runtime");
        obj.AddComponent<AOGSpectatorCameraRuntime>();
    }

    void Awake() => DontDestroyOnLoad(gameObject);

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10)) SpectatorMode = !SpectatorMode;
        if (!SpectatorMode || Camera.main == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) CycleChampion(-1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) CycleChampion(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) FocusObjective(true);
        if (Input.GetKeyDown(KeyCode.Alpha4)) FocusObjective(false);

        if (FollowTarget != null)
        {
            Vector3 desired = FollowTarget.position + followOffset;
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desired, Time.unscaledDeltaTime * followSmooth);
            Camera.main.transform.LookAt(FollowTarget.position);
        }
        else
        {
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            Camera.main.transform.position += input.normalized * freeSpeed * Time.unscaledDeltaTime;
        }
    }

    void CycleChampion(int direction)
    {
        Champion[] champions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        if (champions.Length == 0) return;
        int index = 0;
        if (FollowTarget != null)
        {
            for (int i = 0; i < champions.Length; i++)
                if (champions[i].transform == FollowTarget) index = i;
        }
        index = (index + direction + champions.Length) % champions.Length;
        FollowTarget = champions[index].transform;
    }

    void FocusObjective(bool dragon)
    {
        ObjectiveManager manager = FindObjectOfType<ObjectiveManager>();
        if (manager == null) return;
        GameObject obj = dragon ? manager.DragonObject : manager.MedusaObject;
        if (obj != null) FollowTarget = obj.transform;
    }
}
