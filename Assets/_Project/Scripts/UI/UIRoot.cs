using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace IslandRocketRumble
{
    /// <summary>
    /// The only UI component the scene needs. Builds the overlay canvas, EventSystem and every screen in code,
    /// then switches screens on GameEvents.PhaseChanged.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIRoot : MonoBehaviour
    {
        [Tooltip("Canvas sorting order of the UI overlay.")]
        public int sortingOrder = 50;

        Canvas canvas;
        RectTransform canvasRect;

        TitleScreen title;
        HowToPlayScreen howTo;
        CountdownScreen countdown;
        HudScreen hud;
        ResultScreen result;
        FeedbackLayer feedback;
        readonly CraftMenuView[] menus = new CraftMenuView[2];

        GamePhase phase = GamePhase.Title;
        bool hasPhase;

        // ---- match data kept by the UI (latest HP per island, result)
        public readonly int[] Hp = { 600, 600 };
        public bool HasResult { get; private set; }
        public PlayerId LastWinner { get; private set; }
        public MatchEndReason LastReason { get; private set; }

        public RectTransform CanvasRect => canvasRect;
        public GamePhase Phase => phase;
        public bool IsPlayingPhase => phase == GamePhase.Playing || phase == GamePhase.SuddenDeath;

        public static MatchContext Ctx => MatchContext.Instance;

        public GameConfig Config
        {
            get
            {
                var c = MatchContext.Instance;
                return c != null ? c.config : null;
            }
        }

        public int MaxHp
        {
            get
            {
                var cfg = Config;
                return cfg != null ? Mathf.Max(1, cfg.islandMaxHp) : 600;
            }
        }

        public float MatchDuration
        {
            get
            {
                var cfg = Config;
                return cfg != null ? cfg.matchDuration : 300f;
            }
        }

        public float StunDuration
        {
            get
            {
                var cfg = Config;
                return cfg != null ? cfg.stunDuration : 2.5f;
            }
        }

        public RocketRecipe Recipe(int index)
        {
            var c = MatchContext.Instance;
            if (c == null || c.recipes == null || index < 0 || index >= c.recipes.Length) return null;
            return c.recipes[index];
        }

        // ------------------------------------------------------------------ build

        void Awake()
        {
            BuildCanvas();
            EnsureEventSystem();

            hud = CreateScreen<HudScreen>("HUD");

            var worldLayer = UIFactory.Stretch(canvasRect, "WorldLayer");
            for (int i = 0; i < 2; i++)
            {
                var rt = UIFactory.Rect(worldLayer, "CraftMenu_" + UIText.PlayerTag((PlayerId)i));
                menus[i] = rt.gameObject.AddComponent<CraftMenuView>();
                menus[i].Init(this, (PlayerId)i, worldLayer);
            }

            var fbRect = UIFactory.Stretch(canvasRect, "Feedback");
            feedback = fbRect.gameObject.AddComponent<FeedbackLayer>();
            feedback.Init(this);

            countdown = CreateScreen<CountdownScreen>("Countdown");
            result = CreateScreen<ResultScreen>("Result");
            title = CreateScreen<TitleScreen>("Title");
            howTo = CreateScreen<HowToPlayScreen>("HowToPlay");

            Subscribe();
        }

        /// <summary>Idempotent (re)subscription to the global event bus.</summary>
        void Subscribe()
        {
            GameEvents.PhaseChanged -= OnPhaseChanged;
            GameEvents.IslandDamaged -= OnIslandDamaged;
            GameEvents.MatchEnded -= OnMatchEnded;
            GameEvents.PhaseChanged += OnPhaseChanged;
            GameEvents.IslandDamaged += OnIslandDamaged;
            GameEvents.MatchEnded += OnMatchEnded;
        }

        float resubTimer;
        GamePhase lastCtxPhase;
        bool lastCtxPhaseValid;

        void Update()
        {
            // Safety net: if someone calls GameEvents.Reset() our listeners are gone. Re-attach once a second
            // and follow MatchContext.Phase if it changed without us hearing a PhaseChanged event.
            resubTimer += Time.unscaledDeltaTime;
            if (resubTimer < 1f) return;
            resubTimer = 0f;
            Subscribe();
            hud.Subscribe();
            feedback.Subscribe();

            var c = MatchContext.Instance;
            if (c == null) { lastCtxPhaseValid = false; return; }
            if (lastCtxPhaseValid && c.Phase != lastCtxPhase && c.Phase != phase) ApplyPhase(c.Phase);
            lastCtxPhase = c.Phase;
            lastCtxPhaseValid = true;
        }

        void Start()
        {
            if (!hasPhase)
            {
                var c = MatchContext.Instance;
                ApplyPhase(c != null ? c.Phase : GamePhase.Title);
            }
        }

        void OnDestroy()
        {
            GameEvents.PhaseChanged -= OnPhaseChanged;
            GameEvents.IslandDamaged -= OnIslandDamaged;
            GameEvents.MatchEnded -= OnMatchEnded;
        }

        void BuildCanvas()
        {
            var go = new GameObject("UICanvas", typeof(RectTransform));
            go.layer = UIFactory.UILayer;
            go.transform.SetParent(transform, false);

            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            go.AddComponent<GraphicRaycaster>();
            canvasRect = (RectTransform)go.transform;
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var es = new GameObject("EventSystem");
            es.transform.SetParent(transform, false);
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        T CreateScreen<T>(string name) where T : UIScreen
        {
            var rt = UIFactory.Stretch(canvasRect, name);
            var s = rt.gameObject.AddComponent<T>();
            s.Init(this);
            return s;
        }

        // ------------------------------------------------------------------ events

        void OnPhaseChanged(GamePhase p) => ApplyPhase(p);

        void ApplyPhase(GamePhase p)
        {
            GamePhase prev = phase;
            bool hadPhase = hasPhase;
            phase = p;
            hasPhase = true;

            bool matchStart = p == GamePhase.Countdown ||
                              (p == GamePhase.Playing && (!hadPhase || (prev != GamePhase.Countdown && prev != GamePhase.SuddenDeath && prev != GamePhase.Playing)));
            if (matchStart) ResetMatchData();

            if (p == GamePhase.Title) title.Show(); else title.Hide();
            if (p == GamePhase.HowToPlay) howTo.Show(); else howTo.Hide();

            bool hudOn = p == GamePhase.Countdown || p == GamePhase.Playing || p == GamePhase.SuddenDeath || p == GamePhase.Result;
            if (hudOn) hud.Show(); else hud.Hide();
            hud.OnPhase(p, prev);

            if (p == GamePhase.Countdown)
            {
                var cfg = Config;
                countdown.Begin(cfg != null ? cfg.countdownSeconds : 3f);
            }
            else if (p != GamePhase.Playing && p != GamePhase.SuddenDeath)
            {
                countdown.Hide();
            }

            if (p == GamePhase.Result) result.Show(); else result.Hide();

            feedback.OnPhase(p);
        }

        void ResetMatchData()
        {
            int max = MaxHp;
            Hp[0] = max;
            Hp[1] = max;
            HasResult = false;
            hud.ResetMatch();
        }

        void OnIslandDamaged(PlayerId owner, int damage, int hpLeft, Vector3 point)
        {
            Hp[(int)owner] = Mathf.Max(0, hpLeft);
        }

        void OnMatchEnded(PlayerId winner, MatchEndReason reason)
        {
            HasResult = true;
            LastWinner = winner;
            LastReason = reason;
            if (result.IsShown) result.Refresh(true);
        }
    }
}
