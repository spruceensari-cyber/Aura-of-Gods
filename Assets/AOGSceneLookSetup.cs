using UnityEngine;

public class AOGSceneLookSetup : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("MOBA Camera Settings")]
    public Vector3 cameraPosition = new Vector3(0, 155, -135);
    public Vector3 cameraRotation = new Vector3(58, 0, 0);
    public float fieldOfView = 45f;

    [Header("Lighting")]
    public Color lightColor = new Color(0.78f, 0.86f, 1.0f);
    public float lightIntensity = 1.25f;

    [Header("Fog")]
    public bool useFog = true;
    public Color fogColor = new Color(0.055f, 0.06f, 0.07f);
    public float fogDensity = 0.012f;

    [ContextMenu("Apply Aura Of Gods Scene Look")]
    public void ApplyLook()
    {
        SetupCamera();
        SetupDirectionalLight();
        SetupAmbientAndFog();
        CleanupDuplicateAudioListeners();
    }

    void SetupCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        mainCamera.transform.position = cameraPosition;
        mainCamera.transform.rotation = Quaternion.Euler(cameraRotation);

        mainCamera.fieldOfView = fieldOfView;
        mainCamera.nearClipPlane = 0.3f;
        mainCamera.farClipPlane = 1000f;
        mainCamera.clearFlags = CameraClearFlags.Skybox;
    }

    void SetupDirectionalLight()
    {
        Light sun = null;

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                sun = l;
                break;
            }
        }

        if (sun == null)
        {
            GameObject lightObj = new GameObject("AOG_Moonlit_Directional_Light");
            sun = lightObj.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.name = "AOG_Moonlit_Directional_Light";
        sun.transform.rotation = Quaternion.Euler(48, -35, 0);
        sun.color = lightColor;
        sun.intensity = lightIntensity;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.75f;
    }

    void SetupAmbientAndFog()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.045f, 0.055f, 0.07f);

        RenderSettings.fog = useFog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
    }

    void CleanupDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        if (listeners.Length <= 1)
            return;

        bool keptOne = false;

        foreach (AudioListener listener in listeners)
        {
            if (!keptOne)
            {
                keptOne = true;
                continue;
            }

            if (Application.isPlaying)
                Destroy(listener);
            else
                DestroyImmediate(listener);
        }
    }
}