using UnityEngine;

public static class PlayerInputReader
{
    const float KeyboardAxisThreshold = 0.01f;

    public static Vector2 ReadMoveInput(Joystick joystick)
    {
        Vector2 input = joystick != null
            ? new Vector2(joystick.Horizontal, joystick.Vertical)
            : Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        Vector2 keyboardInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        if (keyboardInput.sqrMagnitude > KeyboardAxisThreshold * KeyboardAxisThreshold)
            input = keyboardInput;
#endif

        return input;
    }

    public static bool TryReadKeyboardRun(out bool isRunning)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        isRunning = Input.GetKey(KeyCode.LeftShift);
        return true;
#else
        isRunning = false;
        return false;
#endif
    }
}
