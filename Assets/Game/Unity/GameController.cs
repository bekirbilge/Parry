using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>Local playable shell. MatchSimulation alone owns combat state.</summary>
    [RequireComponent(typeof(FixedTickDriver), typeof(AudioSource))]
    public sealed class GameController : MonoBehaviour
    {
        private FixedTickDriver _driver;
        private MatchSimulation _sim;
        private readonly InputAdapter[] _inputs = { new InputAdapter(0), new InputAdapter(1) };
        private Texture2D _pixel, _circle;
        private Texture2D _referenceCharacter, _referenceArena;
        private ArtPresentation.CharacterLayout _referenceCharacterLayout;
        private bool _referenceCharacterLayoutLoaded;
        private bool _referenceArtEnabled = true;
        private AudioSource _audio;
        private AudioClip _tone;
        private GUIStyle _label, _large, _small;
        private bool _paused, _slow, _showTuning, _showColliders, _showTouchWidgets, _forceTouchPreview;
        private bool _localTwoPlayer;
        private bool _prototypeInstantTestingStart = true;
        private bool _prototypeFuelBoost = true;
        private int _botDifficulty;
        private bool _smoke;
        private SimulationState _smokeRenderState;
        private int _smokeDriverStepsRemaining, _smokeDriverStepsDone;
        private string _smokeError;
        private string _capturePath = "";
        private string _lastEvent = "";
        private float _lastEventUntil;
        private int _hitstopTicks;
        private Vector2 _tuningScroll;
        private double _runSpeed = CombatTuning.Default.Movement.RunSpeedUnitsPerSecond;
        private double _jumpSpeed = CombatTuning.Default.Movement.JumpSpeedUnitsPerSecond;
        private double _dashSpeed = CombatTuning.Default.Movement.DashSpeedUnitsPerSecond;
        private double _boostDuration = CombatTuning.Default.Movement.DashDurationSeconds;
        private double _upwardBoostSpeed = CombatTuning.Default.Movement.MaximumUpwardBoostSpeedUnitsPerSecond;
        private double _parryExtraReach = CombatTuning.Default.Defense.ParryExtraReachUnits;
        private double _parryWindow = CombatTuning.Default.Movement.PerfectParryWindowSeconds;
        private double _projectileSpeed = CombatTuning.Default.Projectile.BaseSpeedUnitsPerSecond;
        private double _projectileSpeedDecay = CombatTuning.Default.Projectile.SpeedDecayUnitsPerSecondSquared;
        private double _projectileRadius = CombatTuning.Default.Arena.ProjectileRadiusUnits;
        private double _fighterRadius = 2.5;
        private double _spring = CombatTuning.Default.Divider.SpringStrength;
        private double _damping = CombatTuning.Default.Divider.Damping;
        private double _rebound = CombatTuning.Default.Divider.DashReboundSpeedUnitsPerSecond;
        private double _boostLoadDuration = CombatTuning.Default.Divider.BoostLoadSeconds;
        private double _boostLoadSpring = CombatTuning.Default.Divider.BoostLoadSpringStrength;
        private double _boostLoadDamping = CombatTuning.Default.Divider.BoostLoadDamping;

        public MatchSimulation Sim => _sim;
        public CombatTuning Tuning => _sim?.Tuning;
        public int BotDifficulty => _botDifficulty;
        public bool ReferenceArtEnabled
        {
            get => _referenceArtEnabled;
            set => _referenceArtEnabled = value;
        }
        public bool HasReferenceCharacter => _referenceCharacter != null;
        public bool HasReferenceArena => _referenceArena != null;
        public bool HasReferenceCharacterLayout => _referenceCharacterLayoutLoaded;
        public bool RopePreviewSawRebound { get; private set; }
        public double RopePreviewRecoilUnits { get; private set; }
        public bool PrototypeInstantTestingStart
        {
            get => _prototypeInstantTestingStart;
            set => _prototypeInstantTestingStart = value;
        }
        public bool PrototypeFuelBoost
        {
            get => _prototypeFuelBoost;
            set => _prototypeFuelBoost = value;
        }
        public void SetFuelBoostMode(bool enabled)
        {
            _prototypeFuelBoost = enabled;
            RestartMatch();
        }
        public SimulationState CaptureSnapshot() => _sim?.CaptureSnapshot();
        public void SetBotDifficulty(int difficulty)
        {
            _botDifficulty = Mathf.Clamp(difficulty, 0, 2);
            if (_sim != null) SaveTuning();
        }

        private string BotDifficultyName => _botDifficulty == 0 ? "Easy" :
            _botDifficulty == 1 ? "Normal" : "Hard";
        private bool PhoneUi => Application.isMobilePlatform || _showTouchWidgets || _forceTouchPreview;
        public void RestartMatch()
        {
            var movement = new MovementTuning(runSpeedUnitsPerSecond: _runSpeed,
                jumpSpeedUnitsPerSecond: _jumpSpeed, dashSpeedUnitsPerSecond: _dashSpeed,
                dashDurationSeconds: _boostDuration, perfectParryWindowSeconds: _parryWindow,
                maximumUpwardBoostSpeedUnitsPerSecond: _upwardBoostSpeed,
                useFuelBoost: _prototypeFuelBoost);
            var projectile = new ProjectileTuning(baseSpeedUnitsPerSecond: _projectileSpeed,
                speedDecayUnitsPerSecondSquared: _projectileSpeedDecay);
            var divider = new DividerTuning(springStrength: _spring, damping: _damping,
                dashReboundSpeedUnitsPerSecond: _rebound,
                boostLoadSeconds: _boostLoadDuration,
                boostLoadSpringStrength: _boostLoadSpring,
                boostLoadDamping: _boostLoadDamping);
            var match = _prototypeInstantTestingStart ?
                new MatchTuning(roundCountdownSeconds: 0, startImmediatelyForTesting: true) :
                new MatchTuning();
            _sim = new MatchSimulation(new SimulationConfig(), new CombatTuning(movement, projectile,
                new DefenseTuning(parryExtraReachUnits: _parryExtraReach), divider,
                new ArenaTuning(fighterRadiusUnits: _fighterRadius, projectileRadiusUnits: _projectileRadius), match));
            _inputs[0].Clear(); _inputs[1].Clear();
            _lastEvent = "";
            _hitstopTicks = 0;
            SaveTuning();
        }

        private void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--parry-smoke") _smoke = true;
                if (args[i] == "--parry-touch") _forceTouchPreview = true;
                if (args[i] == "--parry-output" && i + 1 < args.Length) _capturePath = args[++i];
            }
            Application.targetFrameRate = 60;
            if (_smoke) Application.runInBackground = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            _driver = GetComponent<FixedTickDriver>();
            _audio = GetComponent<AudioSource>();
            _audio.playOnAwake = false;
            _tone = MakeTone();
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _pixel.SetPixel(0, 0, Color.white); _pixel.Apply();
            _circle = MakeCircle(64);
            _referenceCharacter = Resources.Load<Texture2D>(ArtPresentation.CharacterResource);
            _referenceArena = Resources.Load<Texture2D>(ArtPresentation.ArenaResource);
            _referenceCharacterLayout = ArtPresentation.LoadCharacterLayout(out _referenceCharacterLayoutLoaded);
            RestartMatch();
        }

        private void Start()
        {
            _driver.TickAdvanced += OnTick;
            if (_smoke) { _smokeDriverStepsRemaining = 3; StartCoroutine(RunSmoke()); }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) { _inputs[0].Clear(); _inputs[1].Clear(); }
        }

        private void OnDestroy()
        {
            if (_driver != null) _driver.TickAdvanced -= OnTick;
            if (_pixel != null) Destroy(_pixel);
            if (_circle != null) Destroy(_circle);
            if (_tone != null) Destroy(_tone);
        }

        private void Update()
        {
            if (_smoke) return;
            if (Input.GetKeyDown(KeyCode.F1)) SetFuelBoostMode(!_prototypeFuelBoost);
            if (Input.GetKeyDown(KeyCode.F5)) ToggleTuning();
            if (Input.GetKeyDown(KeyCode.F2)) { _slow = !_slow; Time.timeScale = _slow && !_paused ? .25f : _paused ? 0 : 1; }
            if (Input.GetKeyDown(KeyCode.P)) TogglePause();
            if (Input.GetKeyDown(KeyCode.N) && _paused) StepOnce();
            if (Input.GetKeyDown(KeyCode.R)) RestartMatch();
            if (Input.GetKeyDown(KeyCode.B)) ToggleMode();
            if (Input.GetKeyDown(KeyCode.F3)) _showColliders = !_showColliders;
            if (Input.GetKeyDown(KeyCode.F6)) _referenceArtEnabled = !_referenceArtEnabled;
            if (Input.GetKeyDown(KeyCode.F4))
            {
                _showTouchWidgets = !_showTouchWidgets;
                _inputs[0].Clear();
            }
            if (Input.GetKeyDown(KeyCode.F12)) CaptureScreenshot();
            Rect arena = ArenaLayout.Compute(Screen.width, Screen.height, Screen.safeArea, PhoneUi);
            if (Input.touchSupported)
                foreach (Touch touchEvent in Input.touches)
                    if (touchEvent.phase == TouchPhase.Began) HandleToolbar(arena, touchEvent.position);
            if ((_showTouchWidgets || _forceTouchPreview) && Input.GetMouseButtonDown(0)) HandleToolbar(arena, Input.mousePosition);
            bool touch = Input.touchSupported || _showTouchWidgets || _forceTouchPreview;
            _inputs[0].SampleTouches(arena, _showTouchWidgets || _forceTouchPreview, _showTuning,
                PhoneUi);
            _inputs[0].SampleFrame(touch);
            if (_localTwoPlayer) _inputs[1].SampleFrame(false);
        }

        private void TogglePause()
        {
            _paused = !_paused;
            Time.timeScale = _paused ? 0 : _slow ? .25f : 1;
        }

        private void ToggleMode()
        {
            _localTwoPlayer = !_localTwoPlayer;
            _inputs[1].Clear();
            SaveTuning();
        }

        private void ToggleTuning()
        {
            _showTuning = !_showTuning;
            _inputs[0].Clear(); _inputs[1].Clear();
        }

        private void HandleToolbar(Rect arena, Vector2 bottomLeftPosition)
        {
            Vector2 point = new Vector2(bottomLeftPosition.x, Screen.height - bottomLeftPosition.y);
            for (int i = 0; i < 4; i++)
            {
                if (!ArenaLayout.ToolbarButton(arena, i, PhoneUi).Contains(point)) continue;
                if (i == 0) TogglePause();
                if (i == 1) ToggleMode();
                if (i == 2) ToggleTuning();
                if (i == 3) RestartMatch();
                break;
            }
        }

        private void OnTick(long _)
        {
            if (_smoke)
            {
                if (_smokeDriverStepsRemaining > 0 && _smokeError == null)
                {
                    try
                    {
                        StepSmokeBotTick();
                        _smokeDriverStepsRemaining--;
                        _smokeDriverStepsDone++;
                    }
                    catch (Exception e) { _smokeError = e.ToString(); }
                }
                return;
            }
            if (_paused) return;
            if (_hitstopTicks > 0) { _hitstopTicks--; return; }
            StepOnce();
        }

        private void StepSmokeBotTick()
        {
            SimulationState state = _sim.CaptureSnapshot();
            StepOnce(new SimulationInput(_sim.Tick,
                DebugBot.Decide(state, 0, 0, Tuning.Movement.UseFuelBoost),
                DebugBot.Decide(state, 1, 0, Tuning.Movement.UseFuelBoost)));
            ValidateBounds(_sim.CaptureSnapshot());
        }

        private void StepOnce()
        {
            if (_sim == null) return;
            SimulationState before = _sim.CaptureSnapshot();
            FighterInput left = _inputs[0].ConsumeTick();
            FighterInput right = _localTwoPlayer ? _inputs[1].ConsumeTick() :
                DebugBot.Decide(before, 1, _botDifficulty, Tuning.Movement.UseFuelBoost);
            StepOnce(new SimulationInput(_sim.Tick, left, right));
        }

        /// <summary>Advances one numbered authoritative tick with supplied controls.</summary>
        public IReadOnlyList<SimulationEvent> StepOnce(SimulationInput input)
        {
            if (_sim == null) throw new InvalidOperationException("Controller has not initialized.");
            var events = _sim.Step(input);
            foreach (SimulationEvent e in events)
            {
                if (e.Type == SimulationEventType.PerfectParry || e.Type == SimulationEventType.OwnedProjectileRedirected ||
                    e.Type == SimulationEventType.GuardBroken ||
                    e.Type == SimulationEventType.FighterEliminated ||
                    (e.Type == SimulationEventType.BodyImpact && e.Value > 0))
                {
                    _hitstopTicks = Math.Max(_hitstopTicks, e.Type == SimulationEventType.PerfectParry ? 3 :
                        e.Type == SimulationEventType.OwnedProjectileRedirected ? 1 : 2);
                    if (!_smoke && _audio != null && _tone != null) _audio.PlayOneShot(_tone, .22f);
                }
                if (e.Type == SimulationEventType.PerfectParry || e.Type == SimulationEventType.OwnedProjectileRedirected ||
                    e.Type == SimulationEventType.GuardBroken ||
                    e.Type == SimulationEventType.RoundDraw || e.Type == SimulationEventType.RoundWon ||
                    e.Type == SimulationEventType.MatchWon || e.Type == SimulationEventType.DividerRebound)
                {
                    _lastEvent = e.Type == SimulationEventType.RoundDraw ? "DOUBLE K.O. / DRAW" :
                        e.Type == SimulationEventType.RoundWon ? "P" + (e.EntityId + 1) + " ROUND" :
                        e.Type == SimulationEventType.MatchWon ? "P" + (e.EntityId + 1) + " MATCH" :
                        e.Type == SimulationEventType.OwnedProjectileRedirected ? "SAFE REDIRECT" :
                        e.Type.ToString().ToUpperInvariant();
                    _lastEventUntil = Time.unscaledTime + 1.2f;
                }
            }
            return events;
        }

        private void OnGUI()
        {
            if (_sim == null || _pixel == null) return;
            Rect arena = ArenaLayout.Compute(Screen.width, Screen.height, Screen.safeArea, PhoneUi);
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold };
                _label.normal.textColor = Color.white;
                _large = new GUIStyle(_label);
                _small = new GUIStyle(_label);
            }
            if (PhoneUi)
            {
                float scale = arena.height / 360f;
                _label.fontSize = Mathf.Max(12, Mathf.RoundToInt(18 * scale));
                _large.fontSize = Mathf.Max(20, Mathf.RoundToInt(29 * scale));
                _small.fontSize = Mathf.Max(10, Mathf.RoundToInt(14 * scale));
            }
            else
            {
                _label.fontSize = Mathf.Clamp(Mathf.RoundToInt(arena.height * .028f), 12, 20);
                _large.fontSize = Mathf.Clamp(Mathf.RoundToInt(arena.height * .048f), 20, 34);
                _small.fontSize = Mathf.Clamp(Mathf.RoundToInt(arena.height * .018f), 10, 13);
            }
            Box(new Rect(0, 0, Screen.width, Screen.height), new Color(.035f, .045f, .07f));
            Box(arena, new Color(.09f, .12f, .17f));
            if (_referenceArtEnabled && _referenceArena != null)
                GUI.DrawTextureWithTexCoords(arena, _referenceArena,
                    _referenceCharacterLayout.ArenaUvRect, true);
            var state = _smokeRenderState ?? _sim.CaptureSnapshot();
            float unit = arena.width / (float)Tuning.Arena.WidthUnits;
            float floor = Y(arena, Tuning.Arena.FighterRadiusUnits) + (float)Tuning.Arena.FighterRadiusUnits * unit;
            if (!_referenceArtEnabled || _referenceArena == null)
            {
                Box(new Rect(arena.xMin, floor, arena.width, Mathf.Max(2, arena.yMax - floor)), new Color(.17f, .23f, .30f));
                for (int i = 1; i < 10; i++) Box(new Rect(arena.xMin + arena.width * i / 10f, arena.yMin, 1, arena.height), new Color(1, 1, 1, .045f));
            }
            DrawDivider(arena, state, unit);
            DrawPortal(arena, state, unit);
            foreach (ProjectileState p in state.Projectiles) DrawProjectile(arena, p, unit);
            foreach (FighterState f in state.Fighters) DrawFighter(arena, f, unit);
            DrawHud(arena, state);
            DrawInstructions(ArenaLayout.Footer(Screen.width, Screen.height, Screen.safeArea, PhoneUi));
            if (Input.touchSupported || _showTouchWidgets || _forceTouchPreview)
            {
                DrawToolbar(arena);
                if (!_showTuning) DrawTouch(arena);
            }
            if (_showTuning) DrawTuning(arena);
        }

        private void DrawDivider(Rect a, SimulationState s, float u)
        {
            Color rope = new Color(.40f, .86f, 1f, .82f);
            float width = Mathf.Max(3, u * .36f);
            Vector2 previous = Point(a, DividerSystem.GetRopePoint(s, Tuning, 0));
            Vector2 bottomAnchor = previous;
            for (int i = 1; i < DividerSystem.RopePointCount; i++)
            {
                Vector2 next = Point(a, DividerSystem.GetRopePoint(s, Tuning, i));
                Line(previous, next, rope, width);
                float jointRadius = width * .5f;
                Circle(new Rect(next.x - jointRadius, next.y - jointRadius,
                    width, width), rope);
                previous = next;
            }
            float anchorRadius = Mathf.Max(4, width * .8f);
            Circle(new Rect(bottomAnchor.x - anchorRadius, bottomAnchor.y - anchorRadius,
                anchorRadius * 2, anchorRadius * 2), Color.white);
            Circle(new Rect(previous.x - anchorRadius, previous.y - anchorRadius,
                anchorRadius * 2, anchorRadius * 2), Color.white);
            if (_showColliders) Label(new Rect(a.center.x - 90, a.center.y - 18, 180, 36),
                RopeBend(s, Tuning).ToString("0.0"), _small);
        }

        private static double RopeBend(SimulationState state, CombatTuning tuning)
        {
            double center = tuning.Arena.WidthUnits / 2.0;
            double bend = 0;
            for (int i = 0; i < DividerSystem.RopePointCount; i++)
                bend = Math.Max(bend, Math.Abs(DividerSystem.GetRopePoint(state, tuning, i).X - center));
            return bend;
        }

        private void DrawPortal(Rect a, SimulationState s, float u)
        {
            if (s.Projectiles.Count >= Tuning.Projectile.MaximumActiveCount || s.SpawnWarningIssued == false) return;
            float left = Mathf.Max(0, (float)((s.NextSpawnTick - s.Tick) * _sim.Config.TickDurationSeconds));
            if (left > Tuning.Projectile.SpawnWarningSeconds + .1f) return;
            Vector2 p = Point(a, new Vec2(Tuning.Arena.WidthUnits / 2,
                Tuning.Arena.HeightUnits - Tuning.Arena.ProjectileRadiusUnits - Tuning.Projectile.SpawnPortalInsetUnits));
            float r = (2.0f + .35f * Mathf.Sin(Time.unscaledTime * 10)) * u;
            Circle(new Rect(p.x - r, p.y - r, r * 2, r * 2), new Color(1, .55f, .2f, .8f));
            Label(new Rect(p.x - 55, p.y + r, 110, 22), left.ToString("0.0") + "s", _small);
        }

        private void DrawProjectile(Rect a, ProjectileState p, float u)
        {
            Vector2 pos = Point(a, p.Position), prev = Point(a, p.PreviousPosition);
            Color[] colors = { new Color(.95f, .95f, .95f), new Color(.2f, .9f, 1),
                new Color(1, .55f, .16f), new Color(1, .1f, .42f) };
            Color c = colors[Mathf.Clamp(p.PowerLevel, 0, 3)];
            Line(prev, pos, new Color(c.r, c.g, c.b, .45f), Mathf.Max(2, u * .65f));
            float r = (float)Tuning.Arena.ProjectileRadiusUnits * u;
            Circle(new Rect(pos.x - r * 1.8f, pos.y - r * 1.8f, r * 3.6f, r * 3.6f), new Color(c.r, c.g, c.b, .18f));
            if (p.LastParrierId >= 0)
            {
                Color owner = p.LastParrierId == 0 ? new Color(.2f, .72f, 1f) :
                    new Color(1f, .38f, .48f);
                Circle(new Rect(pos.x - r * 1.35f, pos.y - r * 1.35f,
                    r * 2.7f, r * 2.7f), owner);
            }
            Circle(new Rect(pos.x - r, pos.y - r, r * 2, r * 2), c);
            if (_showColliders) Circle(new Rect(pos.x - r, pos.y - r, r * 2, r * 2), new Color(1, 0, 0, .35f));
        }

        private void DrawFighter(Rect a, FighterState f, float u)
        {
            Vector2 p = Point(a, f.Position);
            float r = (float)Tuning.Arena.FighterRadiusUnits * u;
            Color c = f.Id == 0 ? new Color(.20f, .72f, 1) : new Color(1, .38f, .48f);
            if (f.Eliminated) c = Color.gray;
            if (PoweredBoost(f))
            {
                Vec2 thrust = Tuning.Movement.UseFuelBoost ? f.Velocity : f.DashDirection;
                Vector2 direction = new Vector2((float)thrust.X, -(float)thrust.Y);
                if (direction.sqrMagnitude < .01f) direction = Vector2.right * f.FacingSign;
                direction.Normalize();
                Vector2 tail = p - direction * (r * 2.8f);
                Color exhaust = f.Id == 0 ? new Color(.36f, .93f, 1f, .75f) :
                    new Color(1f, .7f, .28f, .75f);
                Line(p - direction * r * .65f, tail, exhaust, Mathf.Max(3, r * .62f));
                Line(p - direction * r, tail - direction * r * .65f,
                    new Color(exhaust.r, exhaust.g, exhaust.b, .3f), Mathf.Max(2, r * .28f));
                Circle(new Rect(tail.x - r * .32f, tail.y - r * .32f, r * .64f, r * .64f),
                    new Color(1f, .95f, .65f, .8f));
            }
            if (f.ParryTicksRemaining > 0)
            {
                float parryRadius = (float)(Tuning.Arena.FighterRadiusUnits + Tuning.Defense.ParryExtraReachUnits) * u;
                Circle(new Rect(p.x - parryRadius, p.y - parryRadius, parryRadius * 2, parryRadius * 2),
                    new Color(.38f, 1f, .78f, .45f));
            }
            else if (f.GuardBreakTicksRemaining == 0 && f.DefenseHeldPrevious &&
                f.DefenseHoldTicks >= SimMath.SecondsToTicks(Tuning.Defense.GuardHoldThresholdSeconds, _sim.Config.TickRateHz))
                Circle(new Rect(p.x - 1.5f*r, p.y - 1.5f*r, 3*r, 3*r), new Color(.5f, .8f, 1f, .26f));
            if (_referenceArtEnabled && _referenceCharacter != null)
            {
                Circle(new Rect(p.x-r, p.y-r, 2*r, 2*r), new Color(c.r, c.g, c.b, .23f));
                Rect art = ArtPresentation.FighterRect(a, f.Position,
                    (float)Tuning.Arena.WidthUnits, (float)Tuning.Arena.HeightUnits,
                    (float)Tuning.Arena.FighterRadiusUnits, _referenceCharacterLayout);
                Color old = GUI.color;
                GUI.color = f.Eliminated ? new Color(.65f, .65f, .65f, .8f) : Color.white;
                GUI.DrawTextureWithTexCoords(art, _referenceCharacter,
                    f.FacingSign > 0 ? new Rect(0, 0, 1, 1) : new Rect(1, 0, -1, 1), true);
                GUI.color = old;
                Box(new Rect(p.x-r*.75f, p.y+r*.92f, r*1.5f, Mathf.Max(2, u*.12f)), c);
                Label(new Rect(p.x-r*.65f, p.y-r*1.55f, r*1.3f, Mathf.Max(16, u*.55f)),
                    "P" + (f.Id + 1), _small);
            }
            else
            {
                Circle(new Rect(p.x-r, p.y-r, 2*r, 2*r), new Color(c.r, c.g, c.b, .42f));
                Box(new Rect(p.x - r*.72f, p.y - r, r*1.44f, r*2f), c);
                Box(new Rect(p.x + (f.FacingSign > 0 ? r*.18f : -r*.65f), p.y - r*.48f, r*.47f, r*.24f), Color.white);
            }
            if (f.GuardBreakTicksRemaining > 0) Label(new Rect(p.x - 30, p.y - 2.5f*r, 60, 20), "BREAK", _small);
            if (_showColliders) Circle(new Rect(p.x-r, p.y-r, 2*r, 2*r), new Color(1, 0, 0, .28f));
        }

        private bool PoweredBoost(FighterState fighter) => Tuning.Movement.UseFuelBoost ?
            fighter.FuelBoostActive :
            (fighter.DashTicksRemaining > 0 || fighter.DividerLoadTicksRemaining > 0) &&
            fighter.DividerReboundDistanceRemaining <= 0;

        private void DrawHud(Rect a, SimulationState s)
        {
            if (PhoneUi) { DrawPhoneHud(a, s); return; }
            float pad = Mathf.Max(8, a.width * .015f);
            float barWidth = a.width * .25f;
            for (int i = 0; i < 2; i++)
            {
                FighterState f = s.Fighter(i);
                float x = i == 0 ? a.xMin + pad : a.xMax - pad - barWidth;
                Label(new Rect(x, a.yMin + 5, barWidth, 26), "P" + (i+1) + "  " + f.RoundWins + " / 2" +
                    (i == 0 && !_localTwoPlayer ? "  BOT " + BotDifficultyName.ToUpperInvariant() : ""), _label);
                Box(new Rect(x, a.yMin + 31, barWidth, 12), new Color(.04f, .05f, .08f));
                Box(new Rect(x, a.yMin + 31, barWidth * Mathf.Clamp01((float)(f.Posture / Tuning.Defense.MaximumPosture)), 12),
                    f.Posture > 75 ? new Color(1, .22f, .2f) : new Color(.98f, .77f, .27f));
                if (Tuning.Movement.UseFuelBoost)
                {
                    float fraction = Mathf.Clamp01((float)(f.FuelSeconds / Tuning.Movement.FuelCapacitySeconds));
                    Rect fuel = new Rect(x, a.yMin + 50, barWidth, 10);
                    Box(fuel, new Color(.10f, .16f, .19f));
                    Box(new Rect(fuel.x, fuel.y, fuel.width * fraction, fuel.height),
                        f.FuelBoostActive ? new Color(.35f, 1f, .78f) : new Color(.35f, .95f, 1f));
                    string fuelState = f.FuelBoostActive ? "BOOST" :
                        f.FuelBoostNeedsRelease ? "RELEASE TO BOOST" :
                        f.FuelRegenDelayTicksRemaining > 0 ? "COOLING" :
                        fraction < .999f ? "REFILLING" : "READY";
                    Label(new Rect(x, a.yMin + 62, barWidth, 15),
                        "FUEL " + (fraction * 100).ToString("0") + "%  " + fuelState, _small);
                }
                else
                    for (int j = 0; j < MovementTuning.MaximumDashCharges; j++)
                        Circle(new Rect(x + j * 18, a.yMin + 48, 12, 12),
                            j < f.DashCharges ? new Color(.35f, .95f, 1) : new Color(.3f, .35f, .4f));
            }
            string center = "ROUND " + s.RoundNumber;
            if (s.Phase == MatchPhase.Countdown) center = "READY  " + Math.Ceiling(s.PhaseTicksRemaining * _sim.Config.TickDurationSeconds);
            if (s.Phase == MatchPhase.RoundResult) center = s.RoundWinnerId < 0 ? "DOUBLE K.O. / DRAW" : "P" + (s.RoundWinnerId+1) + " ROUND";
            if (s.Phase == MatchPhase.MatchComplete) center = "P" + (s.MatchWinnerId+1) + " WINS  ·  R TO RESTART";
            Label(new Rect(a.center.x - a.width*.25f, a.yMin + 7, a.width*.5f, 40), center, _large);
            if (s.Phase == MatchPhase.Active && s.Projectiles.Count < Tuning.Projectile.MaximumActiveCount)
                Label(new Rect(a.center.x-70, a.yMin+46, 140, 20), "NEXT " +
                    Math.Max(0, (s.NextSpawnTick-s.Tick)*_sim.Config.TickDurationSeconds).ToString("0.0") + "s", _small);
            if (Time.unscaledTime < _lastEventUntil) Label(new Rect(a.center.x - 180, a.center.y - 22, 360, 44), _lastEvent, _large);
            if (_paused) Label(new Rect(a.center.x-100, a.center.y+30, 200, 35), "PAUSED", _large);
            if (_showColliders) Label(new Rect(a.xMin+4, a.yMin+70, 110, 20), "FPS " + (1f/Mathf.Max(.0001f, Time.unscaledDeltaTime)).ToString("0") + "  T" + s.Tick, _small);
        }

        private void DrawPhoneHud(Rect a, SimulationState s)
        {
            float unit = a.height / 360f;
            float pad = a.width * .015f;
            float barWidth = a.width * .27f;
            for (int i = 0; i < 2; i++)
            {
                FighterState f = s.Fighter(i);
                float x = i == 0 ? a.xMin + pad : a.xMax - pad - barWidth;
                Label(new Rect(x, a.yMin + 4 * unit, barWidth, 27 * unit),
                    "P" + (i + 1) + "  " + f.RoundWins + "/2", _label);
                Box(new Rect(x, a.yMin + 32 * unit, barWidth, 10 * unit), new Color(.04f, .05f, .08f));
                Box(new Rect(x, a.yMin + 32 * unit,
                    barWidth * Mathf.Clamp01((float)(f.Posture / Tuning.Defense.MaximumPosture)), 10 * unit),
                    f.Posture > 75 ? new Color(1, .22f, .2f) : new Color(.98f, .77f, .27f));
                if (Tuning.Movement.UseFuelBoost)
                {
                    float fraction = Mathf.Clamp01((float)(f.FuelSeconds / Tuning.Movement.FuelCapacitySeconds));
                    Rect fuel = new Rect(x, a.yMin + 48 * unit, barWidth, 8 * unit);
                    Box(fuel, new Color(.10f, .16f, .19f));
                    Box(new Rect(fuel.x, fuel.y, fuel.width * fraction, fuel.height),
                        f.FuelBoostActive ? new Color(.35f, 1f, .78f) : new Color(.35f, .95f, 1f));
                    string fuelState = f.FuelBoostActive ? "BOOST" :
                        f.FuelBoostNeedsRelease ? "RELEASE" :
                        f.FuelRegenDelayTicksRemaining > 0 ? "COOLING" :
                        fraction < .999f ? "REFILL" : "READY";
                    Label(new Rect(x, a.yMin + 58 * unit, barWidth, 18 * unit),
                        "FUEL " + (fraction * 100).ToString("0") + "% " + fuelState, _small);
                }
                else
                    for (int j = 0; j < MovementTuning.MaximumDashCharges; j++)
                        Circle(new Rect(x + j * 18 * unit, a.yMin + 48 * unit, 12 * unit, 12 * unit),
                            j < f.DashCharges ? new Color(.35f, .95f, 1) : new Color(.3f, .35f, .4f));
            }
            string center = "ROUND " + s.RoundNumber;
            if (s.Phase == MatchPhase.Countdown) center = "READY " + Math.Ceiling(s.PhaseTicksRemaining * _sim.Config.TickDurationSeconds);
            if (s.Phase == MatchPhase.RoundResult) center = s.RoundWinnerId < 0 ? "DRAW" : "P" + (s.RoundWinnerId + 1) + " ROUND";
            if (s.Phase == MatchPhase.MatchComplete) center = "P" + (s.MatchWinnerId + 1) + " WINS";
            Label(new Rect(a.center.x - a.width * .23f, a.yMin + 4 * unit,
                a.width * .46f, 42 * unit), center, _large);
            if (s.Phase == MatchPhase.MatchComplete)
                Label(new Rect(a.center.x - a.width * .20f, a.yMin + 48 * unit,
                    a.width * .40f, 20 * unit), "TAP RESTART", _small);
            else if (s.Phase == MatchPhase.Active && s.Projectiles.Count < Tuning.Projectile.MaximumActiveCount)
                Label(new Rect(a.center.x - a.width * .20f, a.yMin + 48 * unit,
                    a.width * .40f, 20 * unit), "NEXT " +
                    Math.Max(0, (s.NextSpawnTick - s.Tick) * _sim.Config.TickDurationSeconds).ToString("0.0") + "s", _small);
            if (Time.unscaledTime < _lastEventUntil)
                Label(new Rect(a.center.x - a.width * .23f, a.center.y - 22 * unit,
                    a.width * .46f, 44 * unit), _lastEvent, _large);
            if (_paused) Label(new Rect(a.center.x - 100 * unit, a.center.y + 30 * unit,
                200 * unit, 35 * unit), "PAUSED", _large);
            if (_showColliders) Label(new Rect(a.xMin + 4 * unit, a.yMin + 76 * unit,
                120 * unit, 20 * unit), "FPS " + (1f / Mathf.Max(.0001f, Time.unscaledDeltaTime)).ToString("0") +
                " T" + s.Tick, _small);
        }

        private void DrawInstructions(Rect footer)
        {
            if (footer.height < 16) return;
            Box(footer, new Color(.055f, .075f, .10f));
            if (PhoneUi)
            {
                Label(new Rect(footer.xMin + 4, footer.yMin, footer.width - 8, footer.height),
                    "MOVE · HOLD BOOST · TAP/HOLD DEF  |  " +
                    (_localTwoPlayer ? "LOCAL 2P" : "P1 VS BOT " + BotDifficultyName.ToUpperInvariant()), _small);
                return;
            }
            float row = footer.height * .5f;
            string boostLabel = Tuning.Movement.UseFuelBoost ? "hold thrust" : "boost";
            string p1Controls = "P1 WASD aim/move · Space/Shift " + boostLabel + " · E parry/guard";
            if (UnityEngine.InputSystem.Gamepad.all.Count > 0)
                p1Controls = UnityEngine.InputSystem.Gamepad.all[0] is UnityEngine.InputSystem.DualShock.DualShockGamepad
                    ? "P1 PAD: stick aim/move · Cross " + boostLabel + " · Square parry/guard"
                    : "P1 PAD: stick aim/move · A " + boostLabel + " · X parry/guard";
            Label(new Rect(footer.xMin+4, footer.yMin, footer.width-8, row),
                p1Controls + "     P2 arrows aim/move · U/I boost · O parry/guard", _small);
            Label(new Rect(footer.xMin+4, footer.yMin+row, footer.width-8, row),
                (_localTwoPlayer ? "LOCAL 2P" : "P1 VS BOT " + BotDifficultyName.ToUpperInvariant()) +
                "  |  " + (Tuning.Movement.UseFuelBoost ? "FUEL" : "3 CHARGES") +
                " · Neutral boost rises · Ring = temporary parry safety  |  B mode  P pause  N step  R restart  F1 fuel/charges  F5 tune  F6 art  F12 capture", _small);
        }

        private void DrawTouch(Rect a)
        {
            Rect stick = ArenaLayout.TouchControl(a, 0);
            Circle(stick, new Color(.3f, .55f, .7f, .35f));
            Label(stick, "MOVE", _small);
            string[] names = { "BOOST", "DEF" };
            for (int i = 0; i < 2; i++)
            {
                Rect target = ArenaLayout.TouchControl(a, i + 1);
                Circle(target, new Color(.4f, .7f, .9f, .35f));
                Label(target, names[i], _small);
            }
        }

        private void DrawToolbar(Rect a)
        {
            string[] names = { _paused ? "RESUME" : "PAUSE", _localTwoPlayer ? "2P" : "BOT",
                "TUNE", "RESTART" };
            for (int i = 0; i < 4; i++)
            {
                Rect button = ArenaLayout.ToolbarButton(a, i, PhoneUi);
                Box(button, new Color(.16f, .27f, .35f, .82f));
                Label(button, names[i], _small);
            }
        }

        private void DrawTuning(Rect a)
        {
            Rect box = ArenaLayout.TuningPanel(a, PhoneUi);
            Box(box, new Color(.025f, .04f, .07f, .96f));
            float padding = Mathf.Clamp(box.width * .025f, 7, 14);
            GUILayout.BeginArea(new Rect(box.x+padding, box.y+padding, box.width-2*padding, box.height-2*padding));
            _tuningScroll = GUILayout.BeginScrollView(_tuningScroll);
            GUILayout.Label("TUNING · drag sliders, then Apply / reset match");
            bool fuelMode = GUILayout.Toggle(_prototypeFuelBoost, "Fuel boost prototype (off: three charges)");
            if (fuelMode != _prototypeFuelBoost) SetFuelBoostMode(fuelMode);
            Slider("Run speed", ref _runSpeed, 3, 40);
            Slider("Boost speed", ref _dashSpeed, 15, 120);
            Slider("Boost duration", ref _boostDuration, .15f, .5f);
            Slider("Upward boost cap", ref _upwardBoostSpeed, 20, 80);
            Slider("Parry extra reach", ref _parryExtraReach, 0, 4);
            Slider("Parry window", ref _parryWindow, .03f, .3f);
            Slider("Projectile speed", ref _projectileSpeed, 8, 60);
            Slider("Projectile slowdown (units/s²)", ref _projectileSpeedDecay, 3, 48);
            Slider("Projectile radius", ref _projectileRadius, .3f, 1.5f);
            Slider("Fighter radius", ref _fighterRadius, 1.5f, 3.5f);
            Slider("Spring", ref _spring, 0, 80);
            Slider("Damping", ref _damping, 0, 30);
            Slider("Rebound speed", ref _rebound, 10, 85);
            Slider("Rope boost load", ref _boostLoadDuration, .05f, .35f);
            Slider("Rope load spring", ref _boostLoadSpring, 50, 400);
            Slider("Rope load damping", ref _boostLoadDamping, 2, 60);
            GUILayout.Label("Neutral boost rises; aim with move stick. Ring: temporary contact safety (blue P1, red P2); return hits are dangerous.");
            if (GUILayout.Button("Bot difficulty: " + BotDifficultyName + " (tap to change)"))
                SetBotDifficulty((_botDifficulty + 1) % 3);
            _prototypeInstantTestingStart = GUILayout.Toggle(_prototypeInstantTestingStart,
                "Immediate test start (apply/reset)");
            if (GUILayout.Button("Apply / reset match")) RestartMatch();
            GUILayout.Label("Capture: " + _capturePath);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static void Slider(string name, ref double value, float min, float max)
        {
            GUILayout.Label(name + "  " + value.ToString("0.00"));
            value = GUILayout.HorizontalSlider((float)value, min, max);
        }

        private void SaveTuning()
        {
            try
            {
                string dir = string.IsNullOrEmpty(_capturePath) ?
                    Path.Combine(Application.persistentDataPath, "Playtests") : _capturePath;
                Directory.CreateDirectory(dir);
                _capturePath = dir;
                var data = new TuningCapture { runSpeed = Tuning.Movement.RunSpeedUnitsPerSecond,
                    jumpSpeed = Tuning.Movement.JumpSpeedUnitsPerSecond,
                    dashSpeed = Tuning.Movement.DashSpeedUnitsPerSecond,
                    boostDuration = Tuning.Movement.DashDurationSeconds,
                    fuelCapacitySeconds = Tuning.Movement.FuelCapacitySeconds,
                    fuelRegenerationPerSecond = Tuning.Movement.FuelRegenerationPerSecond,
                    fuelRegenerationDelaySeconds = Tuning.Movement.FuelRegenerationDelaySeconds,
                    fuelIgnitionCostSeconds = Tuning.Movement.FuelIgnitionCostSeconds,
                    fuelSteeringAcceleration = Tuning.Movement.FuelSteeringAccelerationUnitsPerSecondSquared,
                    fuelInvulnerabilityRearmSeconds = Tuning.Movement.FuelInvulnerabilityRearmSeconds,
                    boostInvulnerability = Tuning.Movement.DashInvulnerabilitySeconds,
                    parryWindow = Tuning.Movement.PerfectParryWindowSeconds,
                    upwardBoostSpeed = Tuning.Movement.MaximumUpwardBoostSpeedUnitsPerSecond,
                    parryExtraReach = Tuning.Defense.ParryExtraReachUnits,
                    projectileSpeed = Tuning.Projectile.BaseSpeedUnitsPerSecond,
                    projectileDecaySecondsPerLevel = Tuning.Projectile.PowerDecaySecondsPerLevel,
                    projectileSpeedDecay = Tuning.Projectile.SpeedDecayUnitsPerSecondSquared,
                    projectileRadius = Tuning.Arena.ProjectileRadiusUnits,
                    fighterRadius = Tuning.Arena.FighterRadiusUnits,
                    projectileLevel0Speed = Tuning.Projectile.BaseSpeedUnitsPerSecond,
                    projectileLevel1Speed = Tuning.Projectile.BaseSpeedUnitsPerSecond * Tuning.Projectile.Level1SpeedMultiplier,
                    projectileLevel2Speed = Tuning.Projectile.BaseSpeedUnitsPerSecond * Tuning.Projectile.Level2SpeedMultiplier,
                    projectileLevel3Speed = Tuning.Projectile.BaseSpeedUnitsPerSecond * Tuning.Projectile.Level3SpeedMultiplier,
                    spring = Tuning.Divider.SpringStrength,
                    damping = Tuning.Divider.Damping,
                    rebound = Tuning.Divider.DashReboundSpeedUnitsPerSecond,
                    boostLoadDuration = Tuning.Divider.BoostLoadSeconds,
                    boostLoadSpring = Tuning.Divider.BoostLoadSpringStrength,
                    boostLoadDamping = Tuning.Divider.BoostLoadDamping,
                    mode = _localTwoPlayer ? "local-2p" : "bot",
                    boostMode = Tuning.Movement.UseFuelBoost ? "fuel" : "charges",
                    botDifficulty = _botDifficulty,
                    botDifficultyName = BotDifficultyName,
                    immediateTestStart = Tuning.Match.StartImmediatelyForTesting,
                    referenceArtEnabled = _referenceArtEnabled,
                    referenceCharacterLoaded = HasReferenceCharacter,
                    referenceArenaLoaded = HasReferenceArena,
                    referenceCharacterLayoutLoaded = HasReferenceCharacterLayout,
                    checksum = _sim.CaptureSnapshot().ComputeChecksum().ToString("X16"), tick = _sim.Tick };
                File.WriteAllText(Path.Combine(dir, "tuning.json"), JsonUtility.ToJson(data, true));
            }
            catch (Exception e) { Debug.LogWarning("Could not save tuning capture: " + e.Message); }
        }

        [Serializable] private sealed class TuningCapture
        {
            public double runSpeed, jumpSpeed, dashSpeed, boostDuration, boostInvulnerability,
                parryWindow, upwardBoostSpeed, parryExtraReach, projectileSpeed, spring, damping, rebound, boostLoadDuration,
                boostLoadSpring, boostLoadDamping, projectileDecaySecondsPerLevel, projectileSpeedDecay, projectileRadius,
                fighterRadius,
                projectileLevel0Speed, projectileLevel1Speed, projectileLevel2Speed, projectileLevel3Speed,
                fuelCapacitySeconds, fuelRegenerationPerSecond, fuelRegenerationDelaySeconds,
                fuelIgnitionCostSeconds, fuelSteeringAcceleration, fuelInvulnerabilityRearmSeconds;
            public string mode, boostMode, botDifficultyName;
            public int botDifficulty;
            public bool immediateTestStart;
            public bool referenceArtEnabled, referenceCharacterLoaded, referenceArenaLoaded,
                referenceCharacterLayoutLoaded;
            public string checksum;
            public long tick;
        }

        [Serializable] private sealed class SmokeResult
        {
            public bool completed;
            public int ticks;
            public long initialTick;
            public long finalTick;
            public long driverTicks;
            public int driverSimTicks;
            public string checksum;
            public string screenshot;
            public string ropeScreenshot;
            public double ropeBend;
            public double ropeIntrusion;
            public bool ropeReboundSeen;
            public double ropeRecoilUnits;
            public bool referenceArtEnabled;
            public bool referenceCharacterLoaded;
            public bool referenceArenaLoaded;
            public bool referenceCharacterLayoutLoaded;
            public string error;
        }

        private IEnumerator RunSmoke()
        {
            var result = new SmokeResult();
            string dir = string.IsNullOrEmpty(_capturePath) ? Path.Combine(Application.persistentDataPath, "Playtests") : _capturePath;
            Directory.CreateDirectory(dir);
            _capturePath = dir;
            result.initialTick = _sim.Tick;
            result.referenceArtEnabled = _referenceArtEnabled;
            result.referenceCharacterLoaded = HasReferenceCharacter;
            result.referenceArenaLoaded = HasReferenceArena;
            result.referenceCharacterLayoutLoaded = HasReferenceCharacterLayout;
            long driverStart = _driver.Tick;
            for (int i = 0; i < 16 && _smokeDriverStepsDone < 3 && _smokeError == null; i++)
                yield return new WaitForFixedUpdate();
            result.driverTicks = _driver.Tick - driverStart;
            result.driverSimTicks = _smokeDriverStepsDone;
            result.ticks = (int)(_sim.Tick - result.initialTick);
            try
            {
                if (_smokeError != null) throw new InvalidOperationException(_smokeError);
                if (result.driverSimTicks < 3) throw new InvalidOperationException("FixedTickDriver did not deliver three simulation ticks.");
                for (int i = result.ticks; i < 900; i++)
                {
                    StepSmokeBotTick();
                    result.ticks++;
                }
                result.finalTick = _sim.Tick;
                result.completed = result.ticks == 900 && result.finalTick - result.initialTick == 900;
                result.checksum = _sim.CaptureSnapshot().ComputeChecksum().ToString("X16");
                SaveTuning();
            }
            catch (Exception e) { result.finalTick = _sim.Tick; result.error = e.ToString(); result.completed = false; }
            if (!Application.isBatchMode) yield return new WaitForEndOfFrame();
            try
            {
                if (!Application.isBatchMode)
                {
                    string path = Path.Combine(dir, "smoke.png");
                    Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(path, image.EncodeToPNG());
                    Destroy(image);
                    result.screenshot = path;
                }
            }
            catch (Exception e) { Debug.LogWarning("Smoke screenshot unavailable: " + e.Message); }
            if (!Application.isBatchMode)
            {
                try
                {
                    _smokeRenderState = CreateRopePreviewSnapshot();
                    result.ropeBend = RopeBend(_smokeRenderState, Tuning);
                    result.ropeIntrusion = _smokeRenderState.Fighter(0).Position.X - Tuning.Arena.WidthUnits * .5;
                    result.ropeReboundSeen = RopePreviewSawRebound;
                    result.ropeRecoilUnits = RopePreviewRecoilUnits;
                }
                catch (Exception e) { Debug.LogWarning("Rope smoke setup unavailable: " + e.Message); }
                if (_smokeRenderState != null)
                {
                    yield return new WaitForEndOfFrame();
                    try
                    {
                        string path = Path.Combine(dir, "smoke-rope.png");
                        Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                        File.WriteAllBytes(path, image.EncodeToPNG());
                        Destroy(image);
                        result.ropeScreenshot = path;
                    }
                    catch (Exception e) { Debug.LogWarning("Rope smoke screenshot unavailable: " + e.Message); }
                    _smokeRenderState = null;
                }
            }
            File.WriteAllText(Path.Combine(dir, "smoke.json"), JsonUtility.ToJson(result, true));
            Application.Quit(result.completed ? 0 : 1);
        }

        /// <summary>An isolated grounded boost loaded into the rope for the smoke image.</summary>
        public SimulationState CreateRopePreviewSnapshot()
        {
            var demo = new MatchSimulation(_sim.Config, _sim.Tuning);
            SimulationState best = demo.CaptureSnapshot();
            double deepestIntrusion = double.NegativeInfinity;
            bool boosted = false;
            int heldBoostTicks = 0;
            RopePreviewSawRebound = false;
            RopePreviewRecoilUnits = 0;
            double reboundStartX = 0;
            double launchX = demo.Tuning.Arena.WidthUnits * .5 -
                demo.Tuning.Arena.FighterRadiusUnits - 2.25;
            for (int i = 0; i < 170; i++)
            {
                SimulationState before = demo.CaptureSnapshot();
                bool launch = !boosted && before.Fighter(0).Position.X >= launchX;
                if (launch) boosted = true;
                var left = new FighterInput(new Vec2(1, 0), dashPressed: launch,
                    dashHeld: boosted && heldBoostTicks++ < 65);
                demo.Step(new SimulationInput(demo.Tick, left, default(FighterInput)));
                SimulationState current = demo.CaptureSnapshot();
                FighterState fighter = current.Fighter(0);
                double intrusion = fighter.Position.X - demo.Tuning.Arena.WidthUnits * .5;
                if (!RopePreviewSawRebound && fighter.DividerReboundDistanceRemaining > 0)
                {
                    RopePreviewSawRebound = true;
                    reboundStartX = fighter.Position.X;
                }
                if (RopePreviewSawRebound)
                    RopePreviewRecoilUnits = Math.Max(RopePreviewRecoilUnits,
                        reboundStartX - fighter.Position.X);
                if (boosted && PoweredBoost(fighter) && intrusion > deepestIntrusion)
                {
                    deepestIntrusion = intrusion;
                    best = current;
                }
            }
            if (!boosted || deepestIntrusion < 4.0 || RopeBend(best, demo.Tuning) < 4.0 ||
                !RopePreviewSawRebound || RopePreviewRecoilUnits < 1)
                throw new InvalidOperationException("Grounded boost did not load and recoil from the rope.");
            return best;
        }

        private void ValidateBounds(SimulationState s)
        {
            foreach (FighterState f in s.Fighters)
            {
                if (!Finite(f.Position.X) || !Finite(f.Position.Y) || !Finite(f.Velocity.X) || !Finite(f.Velocity.Y) ||
                    f.Position.X < -0.001 || f.Position.X > Tuning.Arena.WidthUnits + .001 ||
                    f.Position.Y < -0.001 || f.Position.Y > Tuning.Arena.HeightUnits + .001)
                    throw new InvalidOperationException("Fighter left valid arena bounds at tick " + s.Tick);
            }
            foreach (ProjectileState p in s.Projectiles)
            {
                if (!Finite(p.Position.X) || !Finite(p.Position.Y) || !Finite(p.Velocity.X) || !Finite(p.Velocity.Y) ||
                    p.Position.X < -0.001 || p.Position.X > Tuning.Arena.WidthUnits + .001 ||
                    p.Position.Y < -0.001 || p.Position.Y > Tuning.Arena.HeightUnits + .001)
                    throw new InvalidOperationException("Projectile left valid arena bounds at tick " + s.Tick);
            }
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private void CaptureScreenshot()
        {
            Directory.CreateDirectory(_capturePath);
            ScreenCapture.CaptureScreenshot(Path.Combine(_capturePath, "playtest-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".png"));
            SaveTuning();
        }

        private static Vector2 Point(Rect a, Vec2 p) => new Vector2(a.xMin + a.width*(float)(p.X/100.0),
            a.yMax - a.height*(float)(p.Y/56.25));
        private static float Y(Rect a, double y) => a.yMax - a.height*(float)(y/56.25);
        private void Label(Rect r, string s, GUIStyle style) => GUI.Label(r, s, style);
        private void Box(Rect r, Color c) { Color old = GUI.color; GUI.color = c; GUI.DrawTexture(r, _pixel); GUI.color = old; }
        private void Circle(Rect r, Color c) { Color old = GUI.color; GUI.color = c; GUI.DrawTexture(r, _circle); GUI.color = old; }
        private void Line(Vector2 a, Vector2 b, Color c, float width)
        {
            float length = Vector2.Distance(a, b);
            if (length < .01f) return;
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y, b.x-a.x)*Mathf.Rad2Deg, a);
            Box(new Rect(a.x, a.y-width*.5f, length, width), c);
            GUI.matrix = old;
        }
        private static Texture2D MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x+.5f, y+.5f), new Vector2(size*.5f, size*.5f));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(size*.5f - d)));
            }
            tex.Apply(); return tex;
        }

        private static AudioClip MakeTone()
        {
            const int sampleRate = 22050;
            int length = sampleRate / 12;
            var samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float fade = 1f - i / (float)length;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 660f * i / sampleRate) * fade * .2f;
            }
            AudioClip clip = AudioClip.Create("Graybox cue", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
