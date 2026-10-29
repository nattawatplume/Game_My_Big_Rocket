using System.Collections;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Match phase machine: Title → HowToPlay → Countdown → Playing → (SuddenDeath) → Result.
    /// Answers GameEvents.Request* from the UI, advances the match clock, detects K.O. / time up / sudden death,
    /// and resets islands, players, parts and rockets when a match (re)starts.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Scene registry. Defaults to MatchContext.Instance.")]
        public MatchContext context;
        [Tooltip("Optional particle material for code-made FX (smoke, sparks, explosions). Auto-created if empty.")]
        public Material particleMaterial;
        [Tooltip("Phase entered when the scene starts. Use Countdown to skip the title while testing.")]
        public GamePhase startPhase = GamePhase.Title;
        [Tooltip("Brief slow motion when an island is knocked out.")]
        public bool slowMoOnKnockOut = true;
        public float slowMoDuration = 0.6f;
        public float slowMoTimeScale = 0.3f;

        [Header("Optional explicit references (found automatically when empty)")]
        public IslandHealth[] islands;
        public PlayerController[] players;
        public PartSpawner[] partSpawners;

        public float CountdownRemaining { get; private set; }
        public PlayerId LastWinner { get; private set; }
        public MatchEndReason LastReason { get; private set; }
        public bool HasResult { get; private set; }

        PartTier lastTier;
        Coroutine slowMo;

        MatchContext Ctx => context != null ? context : MatchContext.Instance;
        public GamePhase Phase => Ctx != null ? Ctx.Phase : GamePhase.Title;

        void Awake()
        {
            Instance = this;
            if (particleMaterial != null) FxFactory.ParticleMaterial = particleMaterial;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void OnEnable()
        {
            GameEvents.RequestHowToPlay += OnRequestHowToPlay;
            GameEvents.RequestStartMatch += OnRequestStartMatch;
            GameEvents.RequestRematch += OnRequestRematch;
            GameEvents.RequestTitle += OnRequestTitle;
            GameEvents.IslandDamaged += OnIslandDamaged;
        }

        void OnDisable()
        {
            GameEvents.RequestHowToPlay -= OnRequestHowToPlay;
            GameEvents.RequestStartMatch -= OnRequestStartMatch;
            GameEvents.RequestRematch -= OnRequestRematch;
            GameEvents.RequestTitle -= OnRequestTitle;
            GameEvents.IslandDamaged -= OnIslandDamaged;
        }

        void Start()
        {
            if (Ctx == null) Debug.LogError("[IRR] GameManager: no MatchContext in the scene.");
            else if (Ctx.config == null) Debug.LogError("[IRR] GameManager: MatchContext.config is not assigned.");

            if (startPhase == GamePhase.Countdown || startPhase == GamePhase.Playing) StartCountdown();
            else if (startPhase == GamePhase.HowToPlay) { ResetMatchState(); SetPhase(GamePhase.HowToPlay); }
            else GoToTitle();
        }

        // ---------------- Requests ----------------

        void OnRequestHowToPlay()
        {
            if (Phase == GamePhase.Title || Phase == GamePhase.Result) SetPhase(GamePhase.HowToPlay);
        }

        void OnRequestStartMatch()
        {
            if (Phase == GamePhase.Title || Phase == GamePhase.HowToPlay || Phase == GamePhase.Result) StartCountdown();
        }

        void OnRequestRematch()
        {
            StartCountdown();
        }

        void OnRequestTitle()
        {
            GoToTitle();
        }

        // ---------------- Public control ----------------

        public void StartCountdown()
        {
            ResetMatchState();
            var cfg = Ctx != null ? Ctx.config : null;
            CountdownRemaining = cfg != null ? cfg.countdownSeconds : 3f;
            SetPhase(GamePhase.Countdown);
        }

        public void GoToTitle()
        {
            ResetMatchState();
            SetPhase(GamePhase.Title);
        }

        void SetPhase(GamePhase phase)
        {
            if (Ctx != null) Ctx.Phase = phase;
            GameEvents.RaisePhaseChanged(phase);
        }

        // ---------------- Loop ----------------

        void Update()
        {
            var ctx = Ctx;
            if (ctx == null) return;

            switch (ctx.Phase)
            {
                case GamePhase.Countdown:
                    CountdownRemaining -= Time.deltaTime;
                    if (CountdownRemaining <= 0f)
                    {
                        CountdownRemaining = 0f;
                        BeginPlaying();
                    }
                    break;

                case GamePhase.Playing:
                    TickPlaying(ctx);
                    break;
            }
        }

        void BeginPlaying()
        {
            var ctx = Ctx;
            ctx.Elapsed = 0f;
            lastTier = ctx.CurrentTier;
            SetPhase(GamePhase.Playing);
            GameEvents.RaiseTierChanged(lastTier);
            GameEvents.RaiseClockTick(0f);
        }

        void TickPlaying(MatchContext ctx)
        {
            float duration = ctx.config != null ? ctx.config.matchDuration : 300f;
            ctx.Elapsed = Mathf.Min(ctx.Elapsed + Time.deltaTime, duration);
            GameEvents.RaiseClockTick(ctx.Elapsed);
            if (ctx.Phase != GamePhase.Playing) return; // a listener may have ended the match

            PartTier tier = ctx.CurrentTier;
            if (tier != lastTier)
            {
                lastTier = tier;
                GameEvents.RaiseTierChanged(tier);
            }

            if (ctx.Elapsed >= duration) TimeUp();
        }

        void TimeUp()
        {
            int hp1 = IslandHp(PlayerId.P1);
            int hp2 = IslandHp(PlayerId.P2);
            if (hp1 == hp2)
            {
                SetPhase(GamePhase.SuddenDeath);
                return;
            }
            EndMatch(hp1 > hp2 ? PlayerId.P1 : PlayerId.P2, MatchEndReason.TimeUp);
        }

        void OnIslandDamaged(PlayerId owner, int damage, int hpLeft, Vector3 point)
        {
            var ctx = Ctx;
            if (ctx == null || !ctx.IsPlaying) return;

            // Deferred to LateUpdate so every other IslandDamaged listener (HUD, result HP) sees the final hit first.
            if (hasPendingEnd) return;
            if (ctx.Phase == GamePhase.SuddenDeath && damage > 0)
                QueueEnd(owner.Opponent(), MatchEndReason.SuddenDeath);
            else if (hpLeft <= 0)
                QueueEnd(owner.Opponent(), MatchEndReason.KnockOut);
        }

        bool hasPendingEnd;
        PlayerId pendingWinner;
        MatchEndReason pendingReason;

        void QueueEnd(PlayerId winner, MatchEndReason reason)
        {
            hasPendingEnd = true;
            pendingWinner = winner;
            pendingReason = reason;
        }

        void LateUpdate()
        {
            if (!hasPendingEnd) return;
            hasPendingEnd = false;
            if (Ctx != null && Ctx.IsPlaying) EndMatch(pendingWinner, pendingReason);
        }

        void EndMatch(PlayerId winner, MatchEndReason reason)
        {
            LastWinner = winner;
            LastReason = reason;
            HasResult = true;
            if (Ctx != null) Ctx.Phase = GamePhase.Result;
            GameEvents.RaiseMatchEnded(winner, reason);
            SetPhase(GamePhase.Result);

            if (reason == MatchEndReason.KnockOut && slowMoOnKnockOut)
            {
                if (slowMo != null) StopCoroutine(slowMo);
                slowMo = StartCoroutine(SlowMoRoutine());
            }
        }

        IEnumerator SlowMoRoutine()
        {
            Time.timeScale = Mathf.Clamp(slowMoTimeScale, 0.05f, 1f);
            yield return new WaitForSecondsRealtime(slowMoDuration);
            Time.timeScale = 1f;
            slowMo = null;
        }

        // ---------------- Helpers ----------------

        int IslandHp(PlayerId id)
        {
            var island = FindIsland(id);
            return island != null ? island.Hp : 0;
        }

        IslandHealth FindIsland(PlayerId id)
        {
            if (Ctx != null && Ctx.Island(id) is IslandHealth registered && registered != null) return registered;
            foreach (var i in GetIslands())
                if (i != null && i.owner == id) return i;
            return null;
        }

        IslandHealth[] GetIslands()
            => islands != null && islands.Length > 0 ? islands : FindObjectsByType<IslandHealth>(FindObjectsSortMode.None);

        PlayerController[] GetPlayers()
            => players != null && players.Length > 0 ? players : FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        PartSpawner[] GetSpawners()
            => partSpawners != null && partSpawners.Length > 0 ? partSpawners : FindObjectsByType<PartSpawner>(FindObjectsSortMode.None);

        /// <summary>Clears everything back to a fresh match (no phase change).</summary>
        public void ResetMatchState()
        {
            if (slowMo != null) { StopCoroutine(slowMo); slowMo = null; }
            Time.timeScale = 1f;
            HasResult = false;
            hasPendingEnd = false;

            var ctx = Ctx;
            if (ctx != null) ctx.ResetMatch();

            foreach (var i in GetIslands()) if (i != null) i.ResetIsland();
            foreach (var p in GetPlayers()) if (p != null) p.ResetToSpawn();
            foreach (var s in GetSpawners()) if (s != null) s.ClearAll();
            RocketFlight.DestroyAll();
            CameraShake.StopAll();

            lastTier = ctx != null ? ctx.CurrentTier : PartTier.Scrap;
        }
    }
}
