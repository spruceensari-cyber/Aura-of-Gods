using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// One input facade for projects that run Legacy Input, the New Input System, or Both.
/// This avoids silent mouse/key failures when Player Settings changes Active Input Handling.
/// </summary>
public static class AOGInputBridge
{
    public static Vector2 PointerPosition
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }
    }

    public static Vector2 ScrollDelta
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.scroll.ReadValue() / 120f;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta;
#else
            return Vector2.zero;
#endif
        }
    }

    public static bool LeftPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
            return true;
#endif
        return false;
    }

    public static bool RightPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(1))
            return true;
#endif
        return false;
    }

    public static bool MiddlePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(2))
            return true;
#endif
        return false;
    }

    public static bool MiddleReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonUp(2))
            return true;
#endif
        return false;
    }

    public static bool MiddleIsPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.middleButton.isPressed)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButton(2))
            return true;
#endif
        return false;
    }

    public static bool KeyPressedThisFrame(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Key mapped = MapKey(key);
            if (mapped != Key.None && Keyboard.current[mapped].wasPressedThisFrame)
                return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(key))
            return true;
#endif
        return false;
    }

    public static bool KeyIsPressed(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Key mapped = MapKey(key);
            if (mapped != Key.None && Keyboard.current[mapped].isPressed)
                return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(key))
            return true;
#endif
        return false;
    }

#if ENABLE_INPUT_SYSTEM
    private static Key MapKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Q: return Key.Q;
            case KeyCode.W: return Key.W;
            case KeyCode.E: return Key.E;
            case KeyCode.R: return Key.R;
            case KeyCode.S: return Key.S;
            case KeyCode.P: return Key.P;
            case KeyCode.Space: return Key.Space;
            case KeyCode.LeftControl: return Key.LeftCtrl;
            case KeyCode.LeftShift: return Key.LeftShift;
            case KeyCode.Escape: return Key.Escape;
            case KeyCode.B: return Key.B;
            default: return Key.None;
        }
    }
#endif
}
