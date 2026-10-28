using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Unified Input Helper that supports both Unity 6's new Input System
    /// and the Legacy Input Manager seamlessly for keyboard/mouse desktop fallbacks.
    /// Eliminates InvalidOperationException in Unity 6.
    /// </summary>
    public static class InputFallback
    {
        public static bool IsShootTriggerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
            return false;
#else
            try
            {
                return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            }
            catch
            {
                return false;
            }
#endif
        }

        public static bool TryGetMouseAim(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                if (delta.sqrMagnitude > 0.01f)
                {
                    screenPosition = Mouse.current.position.ReadValue();
                    return true;
                }
                // Return current position even if not moving
                screenPosition = Mouse.current.position.ReadValue();
                return false;
            }
            return false;
#else
            try
            {
                if (Input.mousePresent)
                {
                    bool moved = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f;
                    screenPosition = Input.mousePosition;
                    return moved;
                }
            }
            catch { }
            return false;
#endif
        }

        public static Vector2 GetCurrentMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
            try
            {
                return Input.mousePosition;
            }
            catch
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
#endif
        }
    }
}
