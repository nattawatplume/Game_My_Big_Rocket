using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// The craft ring on a player island. Standing inside opens that player's CraftMenuState; while open the
    /// movement keys drive the menu (Left/Right = slot, Up/Down = target) and Confirm crafts + launches, repairs the
    /// island or buys an upgrade (then Left/Right + Confirm pick one of the cards).
    /// Confirming the ✕ slot closes the menu, which stays closed until the player leaves and re-enters the ring.
    /// </summary>
    public class CraftZone : MonoBehaviour
    {
        [Tooltip("Whose ring this is.")]
        public PlayerId owner;
        [Tooltip("Ring center (also the rocket launch pad). Defaults to this transform.")]
        public Transform ringCenter;
        [Tooltip("Ring radius in meters (XZ).")]
        public float radius = 1.3f;
        [Tooltip("Optional: the owner's PlayerController. Otherwise the transform registered in MatchContext is used.")]
        public PlayerController player;

        bool suppressed;

        Vector3 Center => ringCenter != null ? ringCenter.position : transform.position;

        /// <summary>Launch pad offset toward the ring's back edge (island +Z, away from the camera) so the player stays in front.</summary>
        public const float PadBackOffset = 0.9f;

        /// <summary>World point where rockets launch and the RocketWorkshop preview stands.</summary>
        public Vector3 LaunchPad
        {
            get
            {
                Transform island = transform.parent != null ? transform.parent : transform;
                Vector3 fwd = Vector3.ProjectOnPlane(island.forward, Vector3.up);
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                return Center + fwd.normalized * PadBackOffset;
            }
        }

        /// <summary>True when the player closed the menu with ✕ and has not left the ring yet.</summary>
        public bool Suppressed => suppressed;

        void Awake()
        {
            gameObject.AddComponent<UpgradeEffects>();
        }

        void Update()
        {
            var ctx = MatchContext.Instance;
            if (ctx == null) return;
            var menu = ctx.CraftMenu(owner);
            if (ctx.IsPlaying) menu.TickCooldowns(Time.deltaTime);

            if (!ctx.IsPlaying)
            {
                if (menu.IsOpen) menu.Close();
                suppressed = false;
                return;
            }

            Transform pt = player != null ? player.transform : ctx.Player(owner);
            if (pt == null) return;

            bool inside = GameplayUtil.DistanceXZ(pt.position, Center) <= radius;
            if (!inside)
            {
                suppressed = false;
                if (menu.IsOpen) menu.Close();
                return;
            }

            if (!menu.IsOpen)
            {
                if (!suppressed) menu.Open();
                return; // don't consume keys on the frame the menu opens
            }

            if (IsStunned(pt)) return;

            if (menu.IsPicking)
            {
                if (PlayerInput2P.LeftPressed(owner)) menu.MoveCard(-1);
                if (PlayerInput2P.RightPressed(owner)) menu.MoveCard(1);
                if (PlayerInput2P.ConfirmPressed(owner)) PickCard(ctx, menu);
                return;
            }

            if (PlayerInput2P.LeftPressed(owner)) menu.Move(-1);
            if (PlayerInput2P.RightPressed(owner)) menu.Move(1);
            if (PlayerInput2P.UpPressed(owner) || PlayerInput2P.DownPressed(owner)) menu.ToggleTarget();

            if (PlayerInput2P.ConfirmPressed(owner)) Confirm(ctx, menu);
        }

        bool IsStunned(Transform pt)
        {
            if (player != null) return player.IsStunned;
            var s = pt.GetComponent<IStunnable>();
            return s != null && s.IsStunned;
        }

        void Confirm(MatchContext ctx, CraftMenuState menu)
        {
            if (menu.Selected == CraftMenuState.CloseSlot)
            {
                menu.Close();
                suppressed = true;
                return;
            }
            if (menu.Selected == CraftMenuState.RepairSlot)
            {
                TryRepair(ctx, menu);
                return;
            }
            if (menu.Selected == CraftMenuState.UpgradeSlot)
            {
                TryBuyUpgrade(ctx, menu);
                return;
            }

            RocketRecipe recipe = null;
            if (ctx.recipes != null && menu.Selected >= 0 && menu.Selected < ctx.recipes.Length)
                recipe = ctx.recipes[menu.Selected];
            if (recipe == null)
            {
                menu.NotifyDenied();
                return;
            }

            var inventory = ctx.Inventory(owner);
            if (menu.RocketCooldown > 0f || !inventory.TrySpend(recipe))
            {
                menu.NotifyDenied();
                return;
            }

            var upgrades = ctx.Upgrades(owner);
            if (ctx.config != null)
                menu.StartRocketCooldown(ctx.config.CooldownFor(recipe.tier) * upgrades.CooldownMultiplier);
            if (recipe.costs.Length > 0 && Random.value < upgrades.RecycleChance)
                inventory.Add(recipe.costs[Random.Range(0, recipe.costs.Length)].Key);

            menu.NotifyCrafted(recipe);

            TargetKind kind = menu.Target;
            IRocketTarget target = ctx.Target(owner, kind);
            if (!IsAlive(target))
            {
                // no robot (or it is gone): fall back to the opponent island
                kind = TargetKind.Opponent;
                target = ctx.Island(owner.Opponent());
                if (!IsAlive(target)) target = null;
            }

            // only used when there is no target at all: land near the opponent (or straight across the sea)
            Transform opp = ctx.Player(owner.Opponent());
            Vector3 fallback = opp != null ? opp.position : Center + new Vector3(owner == PlayerId.P1 ? 14f : -14f, 0f, 0f);
            RocketLauncher.Launch(owner, recipe, kind, target, LaunchPad, fallback);
        }

        void TryRepair(MatchContext ctx, CraftMenuState menu)
        {
            var cfg = ctx.config;
            var island = ctx.Island(owner) as IslandHealth;
            bool canRepair = cfg != null && island != null && menu.RepairCooldown <= 0f
                && ctx.Phase != GamePhase.SuddenDeath && island.Hp < island.MaxHp;
            if (!canRepair || !ctx.Inventory(owner).TrySpendAny(cfg.repairCost))
            {
                menu.NotifyDenied();
                return;
            }
            island.Repair(Mathf.RoundToInt(cfg.repairAmount * ctx.Upgrades(owner).RepairMultiplier));
            menu.StartRepairCooldown(cfg.repairCooldown);
        }

        void TryBuyUpgrade(MatchContext ctx, CraftMenuState menu)
        {
            var cfg = ctx.config;
            var owned = ctx.Upgrades(owner);
            if (cfg == null || owned.Purchased >= cfg.upgradeCosts.Length
                || !ctx.Inventory(owner).TrySpendAny(cfg.upgradeCosts[owned.Purchased]))
            {
                menu.NotifyDenied();
                return;
            }
            menu.BeginPicking(UpgradeCatalog.Deal(owned));
        }

        void PickCard(MatchContext ctx, CraftMenuState menu)
        {
            UpgradeType type = menu.Cards[menu.CardIndex];
            ctx.Upgrades(owner).Add(type);
            if (type == UpgradeType.IslandArmor && ctx.Island(owner) is IslandHealth island && ctx.config != null)
                island.AddMaxHp(ctx.config.islandArmorHp);
            menu.EndPicking();
            GameEvents.RaiseUpgradePicked(owner, type);

            Transform pt = player != null ? player.transform : ctx.Player(owner);
            if (pt == null) return;
            RobotFloatingText.Spawn("UPGRADE!", pt.position + Vector3.up * 3f, Palette.HelmetYellow, 0.09f, 1.1f, 0.8f);
            RobotFx.Burst("UpgradeBurst", pt.position + Vector3.up, RobotFx.DotMaterial, 40, Palette.HelmetYellow, Palette.Cream, 2f, 5f, 0.12f, 0.3f, 0.5f, 1f, 0.3f);
        }

        static bool IsAlive(IRocketTarget t)
        {
            if (t == null) return false;
            if (t is Object o && o == null) return false;
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Palette.CraftRing;
            Vector3 c = Center;
            const int seg = 32;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg;
                float a1 = (i + 1) * Mathf.PI * 2f / seg;
                Gizmos.DrawLine(c + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius,
                    c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius);
            }
        }
    }
}
