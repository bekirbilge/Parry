using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.TouchPhase;

namespace Game.Unity
{
    /// <summary>Buffers render-frame edges until one simulation tick consumes them.</summary>
    public sealed class InputAdapter
    {
        private readonly int _fighterId;
        private bool _jumpPending, _dashPending, _defensePending;
        private bool _jumpHeld, _defenseHeld;
        private Vec2 _move, _rawMove;
        private bool _rawJump, _rawDash, _rawDefense;
        private int _stickFinger = -1;
        private readonly HashSet<int> _dashFingers = new HashSet<int>();
        private readonly HashSet<int> _defenseFingers = new HashSet<int>();
        private readonly Dictionary<int, BoostSwipeGesture> _boostSwipes =
            new Dictionary<int, BoostSwipeGesture>();
        private Vector2 _stickOrigin, _stickPoint;
        private float _joystickTravel;
        private bool _mouseStick, _mouseDash, _mouseDefense;
        private BoostSwipeGesture _mouseBoostSwipe;

        public bool JoystickActive => _stickFinger >= 0 || _mouseStick;
        public Vector2 JoystickOrigin => _stickOrigin;
        public Vector2 JoystickPoint => _stickPoint;

        public InputAdapter(int fighterId) { _fighterId = fighterId; }

        public void SampleFrame(bool enableTouch)
        {
            SampleFrame(enableTouch, Gamepad.all.Count > 0 ? Gamepad.all[0] : null);
        }

        /// <summary>Samples a frame with an explicit controller for virtual-device tests.</summary>
        public void SampleFrame(bool enableTouch, Gamepad gamepad)
        {
            bool left = _fighterId == 0;
            float x = (Input.GetKey(left ? KeyCode.D : KeyCode.RightArrow) ? 1 : 0) -
                (Input.GetKey(left ? KeyCode.A : KeyCode.LeftArrow) ? 1 : 0);
            float y = (Input.GetKey(left ? KeyCode.W : KeyCode.UpArrow) ? 1 : 0) -
                (Input.GetKey(left ? KeyCode.S : KeyCode.DownArrow) ? 1 : 0);
            if (left && gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                x += stick.x;
                y += stick.y;
            }
            Vector2 normalized = Vector2.ClampMagnitude(new Vector2(x, y), 1);
            bool dash = Input.GetKey(left ? KeyCode.LeftShift : KeyCode.I) ||
                Input.GetKey(left ? KeyCode.Space : KeyCode.U) ||
                (left && gamepad != null && gamepad.buttonSouth.isPressed);
            bool defense = Input.GetKey(left ? KeyCode.E : KeyCode.O) ||
                (left && gamepad != null && gamepad.buttonWest.isPressed);
            SampleRaw(new Vec2(normalized.x, normalized.y), false, dash, defense,
                false,
                Input.GetKeyDown(left ? KeyCode.LeftShift : KeyCode.I) ||
                    Input.GetKeyDown(left ? KeyCode.Space : KeyCode.U) ||
                    (left && gamepad != null && gamepad.buttonSouth.wasPressedThisFrame),
                Input.GetKeyDown(left ? KeyCode.E : KeyCode.O) ||
                    (left && gamepad != null && gamepad.buttonWest.wasPressedThisFrame));
        }

        /// <summary>Feeds one sampled frame. Explicit press flags preserve taps that end before a fixed tick.</summary>
        public void SampleRaw(Vec2 move, bool jump, bool dash, bool defense,
            bool jumpPressed = false, bool dashPressed = false, bool defensePressed = false)
        {
            _rawMove = move;
            _rawJump = jump;
            _rawDash = dash;
            _rawDefense = defense;
            _jumpPending |= jumpPressed;
            _dashPending |= dashPressed;
            _defensePending |= defensePressed;
            UpdateCombined();
        }

        private void UpdateCombined()
        {
            bool jump = _rawJump;
            bool dash = _rawDash || _dashFingers.Count > 0 || _mouseDash;
            bool defense = _rawDefense || _defenseFingers.Count > 0 || _mouseDefense;
            Vector2 touchMove = JoystickActive ?
                Vector2.ClampMagnitude((_stickPoint - _stickOrigin) /
                    Mathf.Max(1, _joystickTravel), 1) : Vector2.zero;
            // Near the bottom edge, only a short downward drag may be possible.
            // Preserve the walking dead zone, but accept small deliberate boost aim.
            float aimDeadZone = dash ? Mathf.Min(.1f, 3f / Mathf.Max(1, _joystickTravel)) : .1f;
            _move = touchMove.sqrMagnitude > aimDeadZone * aimDeadZone
                ? new Vec2(touchMove.x, touchMove.y) : _rawMove;
            if (jump && !_jumpHeld) _jumpPending = true;
            if (dash && !_dashHeldPrevious) _dashPending = true;
            if (defense && !_defenseHeld) _defensePending = true;
            _jumpHeld = jump;
            _dashHeldPrevious = dash;
            _defenseHeld = defense;
        }

        private bool _dashHeldPrevious;

        public FighterInput ConsumeTick()
        {
            var input = new FighterInput(_move, _jumpPending, _jumpHeld, _dashPending,
                _defensePending, _defenseHeld, _dashHeldPrevious);
            _jumpPending = _dashPending = _defensePending = false;
            return input;
        }

        public void Clear()
        {
            _jumpPending = _dashPending = _defensePending = false;
            _jumpHeld = _dashHeldPrevious = _defenseHeld = false;
            _rawJump = _rawDash = _rawDefense = false;
            _rawMove = new Vec2(0, 0);
            _stickFinger = -1;
            _dashFingers.Clear();
            _defenseFingers.Clear();
            _boostSwipes.Clear();
            _mouseBoostSwipe = null;
            _mouseStick = _mouseDash = _mouseDefense = false;
            _move = new Vec2(0, 0);
            _stickOrigin = _stickPoint = Vector2.zero;
            _joystickTravel = 0;
        }

        public void SampleTouches(Rect arena, bool mouseWidgets, bool tuningOpen = false,
            bool mobileUi = false, bool menuOpen = false)
        {
            if (_fighterId != 0) return;
            if (menuOpen || tuningOpen) { Clear(); return; }
            foreach (Touch touch in Input.touches)
                SampleTouch(touch.fingerId, touch.phase, touch.position, arena, Screen.height,
                    tuningOpen, mobileUi, menuOpen);
            if (!mouseWidgets) return;
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 p = Input.mousePosition;
                int zone = ArenaLayout.TouchAction(arena, p, Screen.height, tuningOpen, mobileUi, menuOpen);
                _mouseStick = zone == 0 && _stickFinger < 0;
                _mouseDash = zone == 1;
                _mouseDefense = zone == 2;
                _mouseBoostSwipe = _mouseDash ? new BoostSwipeGesture(p, Time.unscaledTimeAsDouble) : null;
                if (_mouseStick)
                {
                    _stickOrigin = _stickPoint = p;
                    _joystickTravel = mobileUi ? ArenaLayout.JoystickTravel(arena, true) :
                        Mathf.Max(40, Screen.height * .12f);
                }
                UpdateCombined();
            }
            if (Input.GetMouseButton(0))
            {
                if (_mouseStick) _stickPoint = Input.mousePosition;
                if (_mouseBoostSwipe != null &&
                    _mouseBoostSwipe.Sample(Input.mousePosition, Time.unscaledTimeAsDouble, Screen.height))
                    _defensePending = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (_mouseBoostSwipe != null &&
                    _mouseBoostSwipe.Sample(Input.mousePosition, Time.unscaledTimeAsDouble, Screen.height))
                    _defensePending = true;
                _mouseStick = _mouseDash = _mouseDefense = false;
                _mouseBoostSwipe = null;
                if (_stickFinger < 0) _stickOrigin = _stickPoint = Vector2.zero;
            }
            UpdateCombined();
        }

        /// <summary>Feeds one touch transition. Finger roles remain fixed until release.</summary>
        public void SampleTouch(int fingerId, TouchPhase phase, Vector2 position, Rect arena, int screenHeight,
            bool tuningOpen = false, bool mobileUi = false, bool menuOpen = false,
            double sampleTimeSeconds = double.NaN)
        {
            if (_fighterId != 0) return;
            if (menuOpen || tuningOpen) { Clear(); return; }
            double sampleTime = double.IsNaN(sampleTimeSeconds) ? Time.unscaledTimeAsDouble : sampleTimeSeconds;
            if (phase == TouchPhase.Began)
            {
                if (fingerId != _stickFinger && !_dashFingers.Contains(fingerId) &&
                    !_defenseFingers.Contains(fingerId))
                {
                    int action = ArenaLayout.TouchAction(arena, position, screenHeight, tuningOpen, mobileUi, menuOpen);
                    if (action == 0 && _stickFinger < 0 && !_mouseStick)
                    {
                        _stickFinger = fingerId;
                        _stickOrigin = _stickPoint = position;
                        _joystickTravel = mobileUi ? ArenaLayout.JoystickTravel(arena, true) :
                            Mathf.Max(40, screenHeight * .12f);
                    }
                    if (action == 1)
                    {
                        _dashFingers.Add(fingerId);
                        _boostSwipes.Add(fingerId, new BoostSwipeGesture(position, sampleTime));
                    }
                    if (action == 2) _defenseFingers.Add(fingerId);
                }
            }
            else if (phase != TouchPhase.Canceled && _boostSwipes.TryGetValue(fingerId, out BoostSwipeGesture swipe) &&
                swipe.Sample(position, sampleTime, screenHeight))
                _defensePending = true;
            if (fingerId == _stickFinger) _stickPoint = position;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                if (fingerId == _stickFinger)
                {
                    _stickFinger = -1;
                    if (!_mouseStick) _stickOrigin = _stickPoint = Vector2.zero;
                }
                _dashFingers.Remove(fingerId);
                _defenseFingers.Remove(fingerId);
                _boostSwipes.Remove(fingerId);
            }
            UpdateCombined();
        }

        /// <summary>Recognizes one quick displacement per burst, then waits for a quiet pause.</summary>
        private sealed class BoostSwipeGesture
        {
            private const double WindowSeconds = .18;
            private const double QuietSeconds = .12;
            private Vector2 _anchor, _quietAnchor;
            private double _anchorTime, _quietStart;
            private bool _armed = true;
            private bool _moving;

            public BoostSwipeGesture(Vector2 position, double time)
            {
                _anchor = _quietAnchor = position;
                _anchorTime = _quietStart = time;
            }

            public bool Sample(Vector2 position, double time, int screenHeight)
            {
                float slop = Mathf.Max(1, screenHeight * .006f);
                float fromQuiet = Vector2.Distance(position, _quietAnchor);
                if (fromQuiet > slop)
                {
                    _quietAnchor = position;
                    _quietStart = time;
                }
                else if (time - _quietStart >= QuietSeconds)
                {
                    _armed = true;
                    _moving = false;
                    _anchor = position;
                    _anchorTime = time;
                }

                if (!_armed) return false;
                float distance = Vector2.Distance(position, _anchor);
                if (!_moving)
                {
                    if (distance <= slop) return false;
                    _moving = true;
                }
                double elapsed = time - _anchorTime;
                if (elapsed > WindowSeconds)
                {
                    _armed = false;
                    return false;
                }
                float minimumDistance = Mathf.Clamp(screenHeight * .035f, 12, 96);
                if (elapsed <= 0 || distance < minimumDistance ||
                    distance / elapsed < screenHeight) return false;
                _armed = false;
                return true;
            }
        }
    }
}
