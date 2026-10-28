using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Capa de entrada única. Funciona con el Input System nuevo y con el Input
/// Manager clásico, así el resto de los scripts no depende de ninguno.
/// </summary>
public static class Controls
{
    /// <summary>Movimiento: WASD.</summary>
    public static Vector2 Move
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return Vector2.zero;
            return new Vector2(Axis(k.dKey.isPressed, k.aKey.isPressed),
                               Axis(k.wKey.isPressed, k.sKey.isPressed));
#else
            return new Vector2(Axis(Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A)),
                               Axis(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S)));
#endif
        }
    }

    /// <summary>Disparo: flechas. Solo 4 direcciones, como en el juego original.</summary>
    public static Vector2 Shoot
    {
        get
        {
            Vector2 v;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return Vector2.zero;
            v = new Vector2(Axis(k.rightArrowKey.isPressed, k.leftArrowKey.isPressed),
                            Axis(k.upArrowKey.isPressed, k.downArrowKey.isPressed));
#else
            v = new Vector2(Axis(Input.GetKey(KeyCode.RightArrow), Input.GetKey(KeyCode.LeftArrow)),
                            Axis(Input.GetKey(KeyCode.UpArrow), Input.GetKey(KeyCode.DownArrow)));
#endif
            if (v.x != 0f) v.y = 0f; // si se pulsan dos ejes, gana el horizontal
            return v;
        }
    }

    /// <summary>Enter: confirmar / empezar.</summary>
    public static bool SubmitPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }
    }

    /// <summary>R: reiniciar.</summary>
    public static bool RestartPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }

    /// <summary>M: silenciar / activar sonido.</summary>
    public static bool MutePressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.M);
#endif
        }
    }

    static float Axis(bool positive, bool negative)
    {
        return (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }
}
