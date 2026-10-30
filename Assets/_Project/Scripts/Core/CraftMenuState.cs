using System;

namespace IslandRocketRumble
{
    /// <summary>
    /// Shared state of one player's craft menu. Gameplay (CraftZone) mutates it, UI renders it.
    /// Slots 0..4 = rockets T1..T5, then repair, upgrade and close (✕).
    /// </summary>
    public class CraftMenuState
    {
        public const int RocketSlots = 5;
        public const int RepairSlot = 5;
        public const int UpgradeSlot = 6;
        public const int CloseSlot = 7;
        public const int SlotCount = 8;

        public PlayerId Owner { get; }
        public bool IsOpen { get; private set; }
        public int Selected { get; private set; }
        public TargetKind Target { get; private set; } = TargetKind.Opponent;

        /// <summary>Upgrade cards on offer; null when the player is not choosing one.</summary>
        public UpgradeType[] Cards { get; private set; }
        public int CardIndex { get; private set; }
        public bool IsPicking => Cards != null;

        public float RocketCooldown { get; private set; }
        public float RocketCooldownTotal { get; private set; }
        public float RepairCooldown { get; private set; }

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

        public void BeginPicking(UpgradeType[] cards)
        {
            Cards = cards;
            CardIndex = 1;
            Changed?.Invoke();
        }

        public void MoveCard(int delta)
        {
            if (Cards == null) return;
            CardIndex = (CardIndex + delta + Cards.Length) % Cards.Length;
            Changed?.Invoke();
        }

        public void EndPicking()
        {
            Cards = null;
            Changed?.Invoke();
        }

        public void StartRocketCooldown(float seconds)
        {
            RocketCooldown = RocketCooldownTotal = seconds;
            Changed?.Invoke();
        }

        public void StartRepairCooldown(float seconds)
        {
            RepairCooldown = seconds;
            Changed?.Invoke();
        }

        public void TickCooldowns(float dt)
        {
            RocketCooldown = RocketCooldown > dt ? RocketCooldown - dt : 0f;
            RepairCooldown = RepairCooldown > dt ? RepairCooldown - dt : 0f;
        }

        public void NotifyCrafted(RocketRecipe recipe) => Crafted?.Invoke(recipe);
        public void NotifyDenied() => Denied?.Invoke();

        public void Reset()
        {
            IsOpen = false;
            Selected = 0;
            Target = TargetKind.Opponent;
            Cards = null;
            RocketCooldown = RocketCooldownTotal = RepairCooldown = 0f;
            Changed?.Invoke();
        }
    }
}
