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
        private int _stickFinger = -1, _dashFinger = -1, _defenseFinger = -1;
        private Vector2 _stickOrigin, _stickPoint;
        private int _touchScreenHeight;
        private bool _mouseStick, _mouseDash, _mouseDefense;

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
            bool dash = _rawDash || _dashFinger >= 0 || _mouseDash;
            bool defense = _rawDefense || _defenseFinger >= 0 || _mouseDefense;
            Vector2 touchMove = _stickFinger >= 0 || _mouseStick ?
                Vector2.ClampMagnitude((_stickPoint - _stickOrigin) /
                    Mathf.Max(40, (_touchScreenHeight > 0 ? _touchScreenHeight : Screen.height) * .12f), 1) : Vector2.zero;
            _move = touchMove.sqrMagnitude > .01f ? new Vec2(touchMove.x, touchMove.y) : _rawMove;
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
            _stickFinger = _dashFinger = _defenseFinger = -1;
            _mouseStick = _mouseDash = _mouseDefense = false;
            _move = new Vec2(0, 0);
            _stickOrigin = _stickPoint = Vector2.zero;
            _touchScreenHeight = 0;
        }

        public void SampleTouches(Rect arena, bool mouseWidgets, bool tuningOpen = false,
            bool mobileUi = false)
        {
            if (_fighterId != 0) return;
            _touchScreenHeight = Screen.height;
            foreach (Touch touch in Input.touches)
                SampleTouch(touch.fingerId, touch.phase, touch.position, arena, Screen.height,
                    tuningOpen, mobileUi);
            if (!mouseWidgets) return;
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 p = Input.mousePosition;
                int zone = ArenaLayout.TouchAction(arena, p, Screen.height, tuningOpen, mobileUi);
                _mouseStick = zone == 0; _mouseDash = zone == 1; _mouseDefense = zone == 2;
                if (_mouseDash) _dashPending = true;
                if (_mouseDefense) _defensePending = true;
                if (_mouseStick) _stickOrigin = _stickPoint = p;
            }
            if (Input.GetMouseButton(0) && _mouseStick) _stickPoint = Input.mousePosition;
            if (Input.GetMouseButtonUp(0)) _mouseStick = _mouseDash = _mouseDefense = false;
            UpdateCombined();
        }

        /// <summary>Feeds one touch transition. Finger roles remain fixed until release.</summary>
        public void SampleTouch(int fingerId, TouchPhase phase, Vector2 position, Rect arena, int screenHeight,
            bool tuningOpen = false, bool mobileUi = false)
        {
            if (_fighterId != 0) return;
            _touchScreenHeight = screenHeight;
            if (phase == TouchPhase.Began)
            {
                int action = ArenaLayout.TouchAction(arena, position, screenHeight, tuningOpen, mobileUi);
                if (action == 0 && _stickFinger < 0) { _stickFinger = fingerId; _stickOrigin = _stickPoint = position; }
                if (action == 1 && _dashFinger < 0) { _dashFinger = fingerId; _dashPending = true; }
                if (action == 2 && _defenseFinger < 0) { _defenseFinger = fingerId; _defensePending = true; }
            }
            if (fingerId == _stickFinger) _stickPoint = position;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                if (fingerId == _stickFinger) _stickFinger = -1;
                if (fingerId == _dashFinger) _dashFinger = -1;
                if (fingerId == _defenseFinger) _defenseFinger = -1;
            }
            UpdateCombined();
        }
    }
}
