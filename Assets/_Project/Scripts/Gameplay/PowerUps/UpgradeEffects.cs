using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Added by each CraftZone. Runs the upgrades that act over time (auto repair, free shields) and shows the
    /// player's upgrade stars and aura, plus the rocket reload bar on the launch pad.
    /// </summary>
    public class UpgradeEffects : MonoBehaviour
    {
        const float StarWidth = 0.3f;

        CraftZone zone;
        float repairCarry;
        float shieldTimer;

        Transform stars;
        Transform aura;
        Material auraMaterial;
        Transform bar;
        Transform barFill;
        Material barBackMaterial;
        Material barFillMaterial;
        int shownStars;

        void Awake()
        {
            zone = GetComponent<CraftZone>();
            BuildPlayerFx();
            BuildBar();
        }

        void OnEnable() { GameEvents.UpgradePicked += OnUpgradePicked; GameEvents.PhaseChanged += OnPhaseChanged; }
        void OnDisable() { GameEvents.UpgradePicked -= OnUpgradePicked; GameEvents.PhaseChanged -= OnPhaseChanged; }

        void OnDestroy()
        {
            if (stars != null) Destroy(stars.gameObject);
            if (aura != null) Destroy(aura.gameObject);
            if (bar != null) Destroy(bar.gameObject);
            if (auraMaterial != null) Destroy(auraMaterial);
            if (barBackMaterial != null) Destroy(barBackMaterial);
            if (barFillMaterial != null) Destroy(barFillMaterial);
        }

        void BuildPlayerFx()
        {
            stars = new GameObject("UpgradeStars_" + zone.owner).transform;
            var sprite = UISprites.Star;
            float scale = StarWidth / sprite.bounds.size.x;
            for (int i = 0; i < 4; i++)
            {
                var star = new GameObject("Star" + i);
                star.transform.SetParent(stars, false);
                star.transform.localScale = Vector3.one * scale;
                var sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = Palette.HelmetYellow;
                star.SetActive(false);
            }

            var ring = new GameObject("UpgradeAura_" + zone.owner);
            aura = ring.transform;
            ring.AddComponent<MeshFilter>().sharedMesh = RobotFx.RingMesh;
            var mr = ring.AddComponent<MeshRenderer>();
            auraMaterial = RobotFx.CreateTransparentMaterial(null);
            mr.sharedMaterial = auraMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.SetActive(false);
        }

        void BuildBar()
        {
            bar = new GameObject("ReloadBar_" + zone.owner).transform;
            bar.localScale = new Vector3(1.5f, 0.16f, 0.02f);

            var back = GameplayUtil.CreatePrimitive(PrimitiveType.Cube, "Back");
            back.transform.SetParent(bar, false);
            barBackMaterial = RobotFx.CreateTransparentMaterial(null);
            RobotFx.SetMaterialColor(barBackMaterial, Palette.Ink);
            back.GetComponent<Renderer>().sharedMaterial = barBackMaterial;

            var fill = GameplayUtil.CreatePrimitive(PrimitiveType.Cube, "Fill");
            barFill = fill.transform;
            barFill.SetParent(bar, false);
            barFillMaterial = RobotFx.CreateTransparentMaterial(null);
            RobotFx.SetMaterialColor(barFillMaterial, Palette.HelmetYellow);
            fill.GetComponent<Renderer>().sharedMaterial = barFillMaterial;

            foreach (var r in bar.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bar.gameObject.SetActive(false);
        }

        void OnUpgradePicked(PlayerId id, UpgradeType type)
        {
            if (id == zone.owner) RefreshStars();
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Countdown && phase != GamePhase.Title) return;
            repairCarry = 0f;
            shieldTimer = 0f;
            RefreshStars();
        }

        void RefreshStars()
        {
            var ctx = MatchContext.Instance;
            int n = ctx != null ? Mathf.Min(ctx.Upgrades(zone.owner).Purchased, stars.childCount) : 0;
            shownStars = n;
            for (int i = 0; i < stars.childCount; i++)
            {
                var star = stars.GetChild(i);
                star.gameObject.SetActive(i < n);
                star.localPosition = new Vector3((i - (n - 1) * 0.5f) * StarWidth, 0f, 0f);
            }
            aura.gameObject.SetActive(n > 0);
            Color c = Palette.Player(zone.owner);
            c.a = 0.35f + 0.15f * n;
            RobotFx.SetMaterialColor(auraMaterial, c);
        }

        void Update()
        {
            var ctx = MatchContext.Instance;
            if (ctx == null) return;

            if (ctx.IsPlaying) TickUpgrades(ctx);
            UpdateBar(ctx);

            Transform player = ctx.Player(zone.owner);
            if (player == null || shownStars == 0) return;
            stars.position = player.position + Vector3.up * 2.6f;
            GameplayUtil.Billboard(stars);
            aura.position = player.position + Vector3.up * 0.04f;
            float s = 0.85f + 0.06f * shownStars + 0.05f * Mathf.Sin(Time.time * 4f);
            aura.localScale = new Vector3(s, 1f, s);
        }

        void TickUpgrades(MatchContext ctx)
        {
            var upgrades = ctx.Upgrades(zone.owner);
            if (!(ctx.Island(zone.owner) is IslandHealth island)) return;

            repairCarry += upgrades.RepairPerSecond * Time.deltaTime;
            if (repairCarry >= 1f)
            {
                int whole = Mathf.FloorToInt(repairCarry);
                repairCarry -= whole;
                island.Repair(whole, false);
            }

            float interval = upgrades.ShieldInterval;
            if (interval > 0f && !island.ShieldActive)
            {
                shieldTimer += Time.deltaTime;
                if (shieldTimer >= interval)
                {
                    shieldTimer = 0f;
                    island.GiveShield();
                }
            }
        }

        void UpdateBar(MatchContext ctx)
        {
            var menu = ctx.CraftMenu(zone.owner);
            bool show = ctx.IsPlaying && menu.RocketCooldown > 0f && menu.RocketCooldownTotal > 0f;
            if (bar.gameObject.activeSelf != show) bar.gameObject.SetActive(show);
            if (!show) return;

            var cam = Camera.main;
            Vector3 toCamera = cam != null ? -cam.transform.forward : Vector3.back;
            bar.position = zone.LaunchPad + Vector3.up * 0.3f + toCamera * 0.4f;
            GameplayUtil.Billboard(bar);
            float left = Mathf.Clamp01(1f - menu.RocketCooldown / menu.RocketCooldownTotal);
            barFill.localScale = new Vector3(left * 0.92f, 0.7f, 1f);
            barFill.localPosition = new Vector3(-(1f - left) * 0.46f, 0f, -0.6f);
        }
    }
}
