using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace IslandRocketRumble
{
    /// <summary>
    /// Reads one player's keys from the shared keyboard.
    /// P1: WASD + Space · P2: Arrow keys + Enter (main or numpad).
    /// </summary>
    public static class PlayerInput2P
    {
        static bool blocked;
        static int releasedFrame = -1;

        /// <summary>True while an overlay (settings) owns the keyboard, and for the rest of the frame it closes in.</summary>
        public static bool Blocked => blocked || Time.frameCount <= releasedFrame;

        public static void Block() => blocked = true;

        public static void Release()
        {
            blocked = false;
            releasedFrame = Time.frameCount;
        }

        public static Vector2 Move(PlayerId id)
        {
            var kb = Keyboard.current;
            if (kb == null || Blocked) return Vector2.zero;
            Vector2 v = id == PlayerId.P1
                ? new Vector2(Axis(kb.dKey, kb.aKey), Axis(kb.wKey, kb.sKey))
                : new Vector2(Axis(kb.rightArrowKey, kb.leftArrowKey), Axis(kb.upArrowKey, kb.downArrowKey));
            return Vector2.ClampMagnitude(v, 1f);
        }

        public static bool ConfirmPressed(PlayerId id)
        {
            var kb = Keyboard.current;
            if (kb == null || Blocked) return false;
            return id == PlayerId.P1
                ? kb.spaceKey.wasPressedThisFrame
                : kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
        }

        public static bool LeftPressed(PlayerId id) => Pressed(id == PlayerId.P1 ? Key.A : Key.LeftArrow);
        public static bool RightPressed(PlayerId id) => Pressed(id == PlayerId.P1 ? Key.D : Key.RightArrow);
        public static bool UpPressed(PlayerId id) => Pressed(id == PlayerId.P1 ? Key.W : Key.UpArrow);
        public static bool DownPressed(PlayerId id) => Pressed(id == PlayerId.P1 ? Key.S : Key.DownArrow);

        static bool Pressed(Key key)
        {
            var kb = Keyboard.current;
            return kb != null && !Blocked && kb[key].wasPressedThisFrame;
        }

        static float Axis(KeyControl positive, KeyControl negative)
            => (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
    }
}
