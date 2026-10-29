using System;

namespace IslandRocketRumble
{
    /// <summary>
    /// Shared state of one player's craft menu. Gameplay (CraftZone) mutates it, UI renders it.
    /// Slots 0..4 = rockets T1..T5, slot 5 = close (✕).
    /// </summary>
    public class CraftMenuState
    {
        public const int CloseSlot = 5;
        public const int SlotCount = 6;

        public PlayerId Owner { get; }
        public bool IsOpen { get; private set; }
        public int Selected { get; private set; }
        public TargetKind Target { get; private set; } = TargetKind.Opponent;

        /// <summary>Raised whenever anything above changes.</summary>
        public event Action Changed;
        /// <summary>Raised when a craft succeeds (for UI pop / sfx).</summary>
        public event Action<RocketRecipe> Crafted;
        /// <summary>Raised when the player tries to craft a slot they cannot afford (UI shake).</summary>
        public event Action Denied;

        public CraftMenuState(PlayerId owner) { Owner = owner; }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            Changed?.Invoke();
            GameEvents.RaiseCraftMenuToggled(Owner, true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Changed?.Invoke();
            GameEvents.RaiseCraftMenuToggled(Owner, false);
        }

        public void Move(int delta)
        {
            Selected = (Selected + delta + SlotCount) % SlotCount;
            Changed?.Invoke();
        }

        public void ToggleTarget()
        {
            Target = Target == TargetKind.Opponent ? TargetKind.Robot : TargetKind.Opponent;
            Changed?.Invoke();
        }

        public void NotifyCrafted(RocketRecipe recipe) => Crafted?.Invoke(recipe);
        public void NotifyDenied() => Denied?.Invoke();

        public void Reset()
        {
            IsOpen = false;
            Selected = 0;
            Target = TargetKind.Opponent;
            Changed?.Invoke();
        }
    }
}
