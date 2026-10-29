using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Plays every sound and the music. Bootstraps itself after the first scene loads (no scene setup needed),
    /// listens only to GameEvents and the two CraftMenuState objects. All audio is synthesized at runtime.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Volume")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.35f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        [Header("Behaviour")]
        public int sfxVoices = 12;
        [Range(0f, 0.2f)] public float pitchJitter = 0.04f;
        public float crossfadeTime = 0.6f;
        [Tooltip("Stereo offset for player-specific sounds (P1 left, P2 right).")]
        [Range(0f, 1f)] public float playerPan = 0.25f;
        [Tooltip("Seconds left in the match when the music switches to the fast 'hurry' loop.")]
        public float hurrySeconds = 30f;
        [Tooltip("After the result jingle, bring the title loop back quietly (multiplier of musicVolume, 0 = stay silent).")]
        [Range(0f, 1f)] public float resultMusicLevel = 0.5f;

        enum Track { None, Title, Battle, Hurry, SuddenDeath }

        // ---------------------------------------------------------------- bootstrap

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            if (FindFirstObjectByType<AudioManager>() != null) return;
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }

        // ---------------------------------------------------------------- state

        AudioSource[] voices;
        float[] voiceStart;
        AudioSource[] music = new AudioSource[2];
        float[] musicGain = new float[2];
        float[] musicTarget = new float[2];
        Track[] musicTrack = new Track[2];
        int activeMusic;
        float musicLevel = 1f; // extra scale (result ducking)

        readonly Dictionary<Track, AudioClip> trackClips = new Dictionary<Track, AudioClip>();
        readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();

        GamePhase phase = GamePhase.Title;
        bool phaseKnown;
        float elapsed;
        float countdownLeft;
        int lastCountdownBeep;
        int lastTickSecond;
        int lastRage;
        bool showingResultMusic;
        float lastCraftTime = -10f;
        float resubTimer;
        AudioListener ownListener;
        Coroutine jingleRoutine;

        // craft menu binding
        MatchContext boundCtx;
        readonly CraftMenuState[] boundMenus = new CraftMenuState[2];
        readonly System.Action[] menuChanged = new System.Action[2];
        readonly System.Action<RocketRecipe>[] menuCrafted = new System.Action<RocketRecipe>[2];
        readonly System.Action[] menuDenied = new System.Action[2];
        readonly int[] prevSelected = new int[2];
        readonly TargetKind[] prevTarget = new TargetKind[2];
        readonly bool[] prevOpen = new bool[2];

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            voices = new AudioSource[Mathf.Max(1, sfxVoices)];
            voiceStart = new float[voices.Length];
            for (int i = 0; i < voices.Length; i++) voices[i] = MakeSource(false);
            for (int i = 0; i < 2; i++) music[i] = MakeSource(true);

            for (int i = 0; i < 2; i++)
            {
                var id = (PlayerId)i;
                menuChanged[i] = () => OnMenuChanged(id);
                menuCrafted[i] = r => OnCrafted(id, r);
                menuDenied[i] = () => OnDenied(id);
            }

            AttachGameEvents();
            EnsureListener();
            StartCoroutine(Generate());
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            DetachGameEvents();
            UnbindMenus();
            Instance = null;
        }

        AudioSource MakeSource(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        IEnumerator Generate()
        {
            // title first (needed right away), then the battle loops, then prewarm all sfx
            yield return MusicComposer.Render(MusicComposer.Title, c => trackClips[Track.Title] = c);
            yield return MusicComposer.Render(MusicComposer.Battle, c => trackClips[Track.Battle] = c);
            yield return GameSfx.Prewarm();
            yield return MusicComposer.Render(MusicComposer.Hurry, c => trackClips[Track.Hurry] = c);
            yield return MusicComposer.Render(MusicComposer.SuddenDeath, c => trackClips[Track.SuddenDeath] = c);
        }

        // ---------------------------------------------------------------- event wiring

        void AttachGameEvents()
        {
            DetachGameEvents();
            GameEvents.PhaseChanged += OnPhaseChanged;
            GameEvents.TierChanged += OnTierChanged;
            GameEvents.ClockTick += OnClockTick;
            GameEvents.PartCollected += OnPartCollected;
            GameEvents.RocketLaunched += OnRocketLaunched;
            GameEvents.IslandDamaged += OnIslandDamaged;
            GameEvents.PlayerStunned += OnPlayerStunned;
            GameEvents.RobotRageChanged += OnRobotRageChanged;
            GameEvents.RobotHit += OnRobotHit;
            GameEvents.GemIncoming += OnGemIncoming;
            GameEvents.MatchEnded += OnMatchEnded;
            GameEvents.CraftMenuToggled += OnCraftMenuToggled;
            GameEvents.RocketPieceBuilt += OnRocketPieceBuilt;
            GameEvents.RocketReady += OnRocketReady;
        }

        void DetachGameEvents()
        {
            GameEvents.PhaseChanged -= OnPhaseChanged;
            GameEvents.TierChanged -= OnTierChanged;
            GameEvents.ClockTick -= OnClockTick;
            GameEvents.PartCollected -= OnPartCollected;
            GameEvents.RocketLaunched -= OnRocketLaunched;
            GameEvents.IslandDamaged -= OnIslandDamaged;
            GameEvents.PlayerStunned -= OnPlayerStunned;
            GameEvents.RobotRageChanged -= OnRobotRageChanged;
            GameEvents.RobotHit -= OnRobotHit;
            GameEvents.GemIncoming -= OnGemIncoming;
            GameEvents.MatchEnded -= OnMatchEnded;
            GameEvents.CraftMenuToggled -= OnCraftMenuToggled;
            GameEvents.RocketPieceBuilt -= OnRocketPieceBuilt;
            GameEvents.RocketReady -= OnRocketReady;
        }

        void BindMenus(MatchContext ctx)
        {
            UnbindMenus();
            boundCtx = ctx;
            if (ctx == null) return;
            for (int i = 0; i < 2; i++)
            {
                var m = ctx.CraftMenu((PlayerId)i);
                if (m == null) continue;
                boundMenus[i] = m;
                m.Changed += menuChanged[i];
                m.Crafted += menuCrafted[i];
                m.Denied += menuDenied[i];
                prevSelected[i] = m.Selected;
                prevTarget[i] = m.Target;
                prevOpen[i] = m.IsOpen;
            }
            if (!phaseKnown) { phase = ctx.Phase; phaseKnown = true; }
        }

        void UnbindMenus()
        {
            for (int i = 0; i < 2; i++)
            {
                var m = boundMenus[i];
                if (m == null) continue;
                m.Changed -= menuChanged[i];
                m.Crafted -= menuCrafted[i];
                m.Denied -= menuDenied[i];
                boundMenus[i] = null;
            }
            boundCtx = null;
        }

        void EnsureListener()
        {
            // Normally the scene camera has one. If nothing listens, add our own; drop it once another appears.
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            bool other = false;
            foreach (var l in listeners) if (l != ownListener && l.enabled && l.gameObject.activeInHierarchy) other = true;
            if (other)
            {
                if (ownListener != null) { Destroy(ownListener); ownListener = null; }
            }
            else if (ownListener == null)
            {
                ownListener = gameObject.AddComponent<AudioListener>();
            }
        }

        // ---------------------------------------------------------------- update

        void Update()
        {
            float udt = Time.unscaledDeltaTime;

            // follow MatchContext (it may appear late or be replaced on scene reload)
            var ctx = MatchContext.Instance;
            if (!ReferenceEquals(ctx, boundCtx)) BindMenus(ctx);

            // safety net against GameEvents.Reset(): re-attach once a second
            resubTimer += udt;
            if (resubTimer >= 1f)
            {
                resubTimer = 0f;
                AttachGameEvents();
                EnsureListener();
                if (ctx != null && ctx.Phase != phase) ApplyPhase(ctx.Phase, false);
            }

            // countdown beeps 3, 2, 1
            if (phase == GamePhase.Countdown)
            {
                countdownLeft -= Time.deltaTime;
                int c = Mathf.CeilToInt(countdownLeft);
                if (c > 0 && c != lastCountdownBeep)
                {
                    lastCountdownBeep = c;
                    Play(GameSfx.CountBeep, 0.9f, 0f, 0f);
                }
            }

            UpdateMusic(udt);
        }

        // ---------------------------------------------------------------- music

        Track DesiredTrack()
        {
            switch (phase)
            {
                case GamePhase.Title:
                case GamePhase.HowToPlay:
                    return Track.Title;
                case GamePhase.Playing:
                {
                    float duration = MatchDuration();
                    if (duration - elapsed <= hurrySeconds && trackClips.ContainsKey(Track.Hurry)) return Track.Hurry;
                    return Track.Battle;
                }
                case GamePhase.SuddenDeath:
                    return trackClips.ContainsKey(Track.SuddenDeath) ? Track.SuddenDeath
                         : trackClips.ContainsKey(Track.Hurry) ? Track.Hurry : Track.Battle;
                case GamePhase.Result:
                    return showingResultMusic && resultMusicLevel > 0f ? Track.Title : Track.None;
                default:
                    return Track.None; // countdown: let the beeps breathe
            }
        }

        void UpdateMusic(float dt)
        {
            Track want = DesiredTrack();
            if (want != Track.None && !trackClips.ContainsKey(want))
                want = musicTrack[activeMusic]; // not rendered yet: keep whatever is playing

            if (want != musicTrack[activeMusic] || (want != Track.None && musicTarget[activeMusic] <= 0f))
            {
                if (want == Track.None)
                {
                    musicTarget[0] = musicTarget[1] = 0f;
                    musicTrack[activeMusic] = Track.None;
                }
                else if (want == musicTrack[activeMusic])
                {
                    musicTarget[activeMusic] = 1f;
                }
                else
                {
                    int next = 1 - activeMusic;
                    musicTarget[activeMusic] = 0f;
                    var src = music[next];
                    src.clip = trackClips[want];
                    src.time = 0f;
                    src.Play();
                    musicGain[next] = 0f;
                    musicTarget[next] = 1f;
                    musicTrack[next] = want;
                    activeMusic = next;
                }
            }

            float levelTarget = phase == GamePhase.Result ? resultMusicLevel : 1f;
            musicLevel = Mathf.MoveTowards(musicLevel, levelTarget, dt);

            float rate = crossfadeTime > 0.01f ? dt / crossfadeTime : 1f;
            for (int i = 0; i < 2; i++)
            {
                musicGain[i] = Mathf.MoveTowards(musicGain[i], musicTarget[i], rate);
                var s = music[i];
                s.volume = musicGain[i] * musicVolume * masterVolume * musicLevel;
                if (musicGain[i] <= 0f && musicTarget[i] <= 0f && s.isPlaying)
                {
                    s.Stop();
                    if (i != activeMusic) musicTrack[i] = Track.None;
                }
            }
        }

        float MatchDuration()
        {
            var ctx = MatchContext.Instance;
            return ctx != null && ctx.config != null ? ctx.config.matchDuration : 300f;
        }

        // ---------------------------------------------------------------- sfx playback

        /// <summary>Plays a clip on a free pooled voice. pan: -1 left .. 1 right.</summary>
        public void Play(AudioClip clip, float volume = 1f, float pan = 0f, float jitter = -1f, float delay = 0f,
            float pitch = 1f, float minInterval = 0.04f)
        {
            if (clip == null || voices == null) return;
            float now = Time.unscaledTime;
            if (minInterval > 0f && lastPlayed.TryGetValue(clip, out float last) && now - last < minInterval) return;
            lastPlayed[clip] = now;

            int pick = -1;
            float oldest = float.MaxValue;
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying) { pick = i; break; }
                if (voiceStart[i] < oldest) { oldest = voiceStart[i]; pick = i; }
            }
            var s = voices[pick];
            voiceStart[pick] = now + delay;
            s.Stop();
            s.clip = clip;
            float j = jitter < 0f ? pitchJitter : jitter;
            s.pitch = pitch * (1f + Random.Range(-j, j));
            s.panStereo = Mathf.Clamp(pan, -1f, 1f);
            s.volume = Mathf.Clamp01(volume) * sfxVolume * masterVolume;
            if (delay > 0f) s.PlayDelayed(delay); else s.Play();
        }

        float Pan(PlayerId id) => id == PlayerId.P1 ? -playerPan : playerPan;

        // ---------------------------------------------------------------- handlers: flow

        void OnPhaseChanged(GamePhase p) => ApplyPhase(p, true);

        void ApplyPhase(GamePhase p, bool fromEvent)
        {
            GamePhase prev = phase;
            phase = p;
            phaseKnown = true;
            if (prev == p && fromEvent && p != GamePhase.Countdown) return;

            switch (p)
            {
                case GamePhase.Countdown:
                {
                    var ctx = MatchContext.Instance;
                    countdownLeft = ctx != null && ctx.config != null ? ctx.config.countdownSeconds : 3f;
                    lastCountdownBeep = int.MaxValue;
                    lastRage = 0;
                    elapsed = 0f;
                    break;
                }
                case GamePhase.Playing:
                    elapsed = 0f;
                    lastTickSecond = int.MaxValue;
                    if (prev == GamePhase.Countdown) Play(GameSfx.CountGo, 1f, 0f, 0f);
                    break;
                case GamePhase.SuddenDeath:
                    if (fromEvent) Play(GameSfx.DrumRoll, 1f, 0f, 0f);
                    break;
                case GamePhase.Result:
                    showingResultMusic = false;
                    break;
                case GamePhase.Title:
                case GamePhase.HowToPlay:
                    showingResultMusic = false;
                    break;
            }
        }

        void OnTierChanged(PartTier t)
        {
            if ((int)t <= 1) return; // the Tier 1 announcement at match start is silent
            Play(GameSfx.TierUp, 0.85f, 0f, 0f);
        }

        void OnClockTick(float e)
        {
            elapsed = e;
            if (phase != GamePhase.Playing) return;
            float remaining = MatchDuration() - e;
            if (remaining > 10f || remaining <= 0f) return;
            int sec = Mathf.CeilToInt(remaining);
            if (sec != lastTickSecond)
            {
                lastTickSecond = sec;
                Play(GameSfx.Tick, sec <= 3 ? 0.75f : 0.5f, 0f, 0f, pitch: sec <= 3 ? 1.25f : 1f);
            }
        }

        void OnMatchEnded(PlayerId winner, MatchEndReason reason)
        {
            float wait;
            if (reason == MatchEndReason.TimeUp)
            {
                Play(GameSfx.Whistle, 0.8f, 0f, 0.01f, minInterval: 0f);
                wait = 0.95f;
            }
            else
            {
                Play(GameSfx.Explosion(3), 1f, 0f, 0.02f, minInterval: 0f);
                wait = 0.6f;
            }
            if (jingleRoutine != null) StopCoroutine(jingleRoutine);
            jingleRoutine = StartCoroutine(JingleRoutine(wait));
        }

        IEnumerator JingleRoutine(float wait)
        {
            yield return new WaitForSecondsRealtime(wait);
            Play(GameSfx.Victory, 0.9f, 0f, 0f, minInterval: 0f);
            var v = GameSfx.Victory;
            yield return new WaitForSecondsRealtime(v != null ? v.length + 0.3f : 2.5f);
            if (phase == GamePhase.Result) showingResultMusic = true;
            jingleRoutine = null;
        }

        // ---------------------------------------------------------------- handlers: gameplay

        void OnPartCollected(PlayerId p, PartKey key)
        {
            Play(GameSfx.PartPop(key.tier), 0.7f, Pan(p), minInterval: 0.03f);
        }

        void OnRocketLaunched(PlayerId p, RocketRecipe recipe, TargetKind target)
        {
            int tier = recipe != null ? (int)recipe.tier : 1;
            Play(GameSfx.Launch(tier), 0.65f + 0.07f * tier, Pan(p) * 0.6f);
        }

        void OnIslandDamaged(PlayerId owner, int dmg, int hpLeft, Vector3 point)
        {
            if (dmg <= 0) return;
            int size = dmg <= 40 ? 0 : dmg <= 120 ? 1 : dmg <= 250 ? 2 : 3;
            float pan = Pan(owner) * 1.4f;
            Play(GameSfx.Explosion(size), 0.75f + 0.08f * size, pan, minInterval: 0.02f);
            // the owner of the island squeals "ว้ายยย!"
            Play(GameSfx.Wah(owner == PlayerId.P2 ? 0 : 1), 0.55f, pan, 0.06f, delay: 0.12f, minInterval: 0.25f);
        }

        void OnPlayerStunned(PlayerId p, float duration)
        {
            Play(GameSfx.Stunned, 0.75f, Pan(p));
        }

        void OnRobotRageChanged(int level, int accumulated)
        {
            if (level > lastRage && level >= 1) Play(GameSfx.RageUp(level), 0.8f, 0f, 0.02f);
            lastRage = level;
        }

        void OnRobotHit(PlayerId shooter, RocketRecipe recipe)
        {
            Play(GameSfx.RobotHit, 0.8f, 0f);
        }

        void OnGemIncoming(Vector3 point)
        {
            Play(GameSfx.GemWarning, 0.6f, Mathf.Clamp(point.x / 10f, -0.6f, 0.6f), 0.02f, minInterval: 0.12f);
        }

        void OnRocketPieceBuilt(PlayerId p, PartType type)
        {
            Play(GameSfx.BuildClink, 0.6f, Pan(p), delay: 0.08f, minInterval: 0.05f);
        }

        void OnRocketReady(PlayerId p, RocketRecipe recipe)
        {
            Play(GameSfx.ReadyChime, 0.7f, Pan(p), 0.01f, delay: 0.3f, minInterval: 0.2f);
        }

        // ---------------------------------------------------------------- handlers: craft menu

        void OnCraftMenuToggled(PlayerId p, bool open)
        {
            if (open) Play(GameSfx.MenuOpen, 0.55f, Pan(p));
            else if (Time.unscaledTime - lastCraftTime > 0.25f) Play(GameSfx.MenuClose, 0.45f, Pan(p));
        }

        void OnMenuChanged(PlayerId p)
        {
            int i = (int)p;
            var m = boundMenus[i];
            if (m == null) return;
            if (m.IsOpen && prevOpen[i])
            {
                if (m.Target != prevTarget[i]) Play(GameSfx.UiTarget, 0.5f, Pan(p), minInterval: 0.02f);
                else if (m.Selected != prevSelected[i]) Play(GameSfx.UiMove, 0.45f, Pan(p), minInterval: 0.02f);
            }
            prevOpen[i] = m.IsOpen;
            prevSelected[i] = m.Selected;
            prevTarget[i] = m.Target;
        }

        void OnCrafted(PlayerId p, RocketRecipe recipe)
        {
            lastCraftTime = Time.unscaledTime;
            Play(GameSfx.CraftSuccess, 0.75f, Pan(p), minInterval: 0f);
        }

        void OnDenied(PlayerId p)
        {
            Play(GameSfx.Denied, 0.6f, Pan(p));
        }
    }
}
