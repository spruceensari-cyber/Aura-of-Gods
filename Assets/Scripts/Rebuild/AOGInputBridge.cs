using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Single input bridge for the rebuild slice. Uses the new Input System when enabled and legacy input otherwise.
/// Prevents silent Q/W/E/R failure when Active Input Handling is set to Input System Package only.
/// </summary>
public static class AOGInputBridge
{
    public static bool QPressed => KeyPressed(KeyCode.Q, 0);
    public static bool WPressed => KeyPressed(KeyCode.W, 1);
    public static bool EPressed => KeyPressed(KeyCode.E, 2);
    public static bool RPressed => KeyPressed(KeyCode.R, 3);
    public static bool FocusHeld => KeyHeld(KeyCode.Space, 4);

    public static bool LeftClickPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }

    public static bool RightClickPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }
    }

    public static bool MiddleClickPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(2);
#endif
        }
    }

    public static bool MiddleClickReleased
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(2);
#endif
        }
    }

    public static Vector2 PointerPosition
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
    }

    public static float ScrollY
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
#else
            return Input.GetAxis("Mouse ScrollWheel");
#endif
        }
    }

    private static bool KeyPressed(KeyCode legacy, int index)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        return index switch
        {
            0 => Keyboard.current.qKey.wasPressedThisFrame,
            1 => Keyboard.current.wKey.wasPressedThisFrame,
            2 => Keyboard.current.eKey.wasPressedThisFrame,
            3 => Keyboard.current.rKey.wasPressedThisFrame,
            _ => Keyboard.current.spaceKey.wasPressedThisFrame
        };
#else
        return Input.GetKeyDown(legacy);
#endif
    }

    private static bool KeyHeld(KeyCode legacy, int index)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        return index == 4 && Keyboard.current.spaceKey.isPressed;
#else
        return Input.GetKey(legacy);
#endif
    }
}
