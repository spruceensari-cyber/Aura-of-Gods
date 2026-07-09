using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DefaultExecutionOrder(-10000)]
public class AOGEventSystemCompatibilityRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_EventSystem_Compatibility");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGEventSystemCompatibilityRuntime>();
    }

    private void Start()
    {
        EnsureCompatibleEventSystem();
    }

    private void Update()
    {
        if (EventSystem.current == null)
            EnsureCompatibleEventSystem();
    }

    private static void EnsureCompatibleEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventObject = new GameObject("AOG_EventSystem");
            eventSystem = eventObject.AddComponent<EventSystem>();
            DontDestroyOnLoad(eventObject);
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
            legacyModule.enabled = false;

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
