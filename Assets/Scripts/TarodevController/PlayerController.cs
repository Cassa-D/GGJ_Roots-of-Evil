// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TarodevController {
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController {
        [SerializeField] private ScriptableStats _stats;

        #region Internal

        [HideInInspector] private Rigidbody2D _rb; // Hide is for serialization to avoid errors in gizmo calls
        [SerializeField] private Collider2D _standingCollider;
        private PlayerHealthHandler _healthHandler;
        private Collider2D _col; // current active collider
        private PlayerInput _input;
        private bool _cachedTriggerSetting;

        private FrameInput _frameInput;
        private Vector2 _speed;
        private Vector2 _currentExternalVelocity;
        private int _fixedFrame;
        public bool HasControl {get; private set; }

        #endregion

        #region External

        public event Action<bool, float> GroundedChanged;
        public event Action<bool, Vector2> DashingChanged;
        public event Action<bool> WallGrabChanged;
        public event Action LedgeClimbChanged;
        public event Action<bool> Jumped;
        public event Action AirJumped;
        // public event Action Attacked;
        public ScriptableStats PlayerStats => _stats;
        public Vector2 Input => _frameInput.Move;
        public Vector2 Velocity => _rb.velocity;
        public Vector2 Speed => _speed; // + _currentExternalVelocity; // we should add this, right?
        public Vector2 GroundNormal { get; private set; }
        public int WallDirection { get; private set; }
        public bool ClimbingLadder { get; private set; }
        public bool GrabbingLedge { get; private set; }
        public bool ClimbingLedge { get; private set; }

        public virtual void ApplyVelocity(Vector2 vel, PlayerForce forceType) {
            if (forceType == PlayerForce.Burst) _speed += vel;
            else _currentExternalVelocity += vel;
        }

        public virtual void SetVelocity(Vector2 vel, PlayerForce velocityType) {
            if (velocityType == PlayerForce.Burst) _speed = vel;
            else _currentExternalVelocity = vel;
        }

        public virtual void TakeAwayControl(bool resetVelocity = true) {
            if (resetVelocity) _rb.velocity = Vector2.zero;
            HasControl = false;
        }

        public virtual void ReturnControl() {
            _speed = Vector2.zero;
            HasControl = true;
        }

        #endregion

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _input = GetComponent<PlayerInput>();
            _healthHandler = GetComponent<PlayerHealthHandler>();
            _cachedTriggerSetting = Physics2D.queriesHitTriggers;
            Physics2D.queriesStartInColliders = false;

            ToggleColliders();
        }

        protected virtual void Update() {
            if (!HasControl) return;
            GatherInput();
        }

        protected virtual void GatherInput() {
            _frameInput = _input.FrameInput;

            if (_stats.SnapInput) {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadzoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadzoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown) {
                _jumpToConsume = true;
                _frameJumpWasPressed = _fixedFrame;
            }

            if (_frameInput.Move.x != 0) _stickyFeet = false;

            if (_frameInput.DashDown && (_stats.AllowDash || _hasDashPowerUp)) _dashToConsume = true;
            // if (_frameInput.AttackDown && _stats.AllowAttacks) _attackToConsume = true;
        }

        protected virtual void FixedUpdate() {
            if (DialogueManager.Instance.DialogueIsPlaying)
            {
                if (!HasControl) return;
                TakeAwayControl();
            }
            else if (!HasControl)
            {
                ReturnControl();
            }
            
            _fixedFrame++;

            CheckCollisions();
            HandleCollisions();
            HandleWalls();
            HandleLedges();
            HandleLadders();

            HandleJump();
            HandleDash();
            // HandleAttacking();

            HandleHorizontal();
            HandleVertical();
            ApplyMovement();
        }

        #region Collisions

        private readonly RaycastHit2D[] _spikeRaycastHits = new RaycastHit2D[4];
        private readonly Collider2D[] _spikeColliderHits = new Collider2D[4];
        
        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[2];
        private readonly RaycastHit2D[] _ceilingHits = new RaycastHit2D[2];
        private readonly Collider2D[] _wallHits = new Collider2D[5];
        private readonly Collider2D[] _ladderHits = new Collider2D[1];
        private RaycastHit2D _hittingWall;
        private int _spikeHitCount;
        private int _groundHitCount;
        private int _ceilingHitCount;
        private int _wallHitCount;
        private int _ladderHitCount;
        private int _frameLeftGrounded = int.MinValue;
        private bool _grounded;
        private Vector2 _skinWidth = new(0.02f, 0.02f); // Expose this?

        protected virtual void CheckCollisions() {
            Physics2D.queriesHitTriggers = false;

            var colSize = _col switch
            {
                CapsuleCollider2D capsuleCol => capsuleCol.size, 
                BoxCollider2D boxCol => boxCol.size,
                _ => Vector2.zero
            };

            // Ground and Ceiling
            _groundHitCount = Physics2D.CapsuleCastNonAlloc(_col.bounds.center, colSize, CapsuleDirection2D.Horizontal, 0, Vector2.down, _groundHits, _stats.GrounderDistance, ~_stats.PlayerLayer);
            _ceilingHitCount = Physics2D.CapsuleCastNonAlloc(_col.bounds.center, colSize, CapsuleDirection2D.Horizontal, 0, Vector2.up, _ceilingHits, _stats.GrounderDistance, ~_stats.PlayerLayer);

            // Walls and Ladders
            var bounds = GetWallDetectionBounds(); // won't be able to detect a wall if we're crouching mid-air
            _wallHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _wallHits, _stats.ClimbableLayer);

            _hittingWall = Physics2D.CapsuleCast(_col.bounds.center, colSize, CapsuleDirection2D.Horizontal, 0, new Vector2(_input.FrameInput.Move.x, 0), _stats.GrounderDistance, ~_stats.PlayerLayer);

            // Spike
            _spikeHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _spikeColliderHits, _stats.SpikeLayer);
            if (_spikeHitCount == 0) _spikeHitCount = Physics2D.CapsuleCastNonAlloc(_col.bounds.center, colSize, CapsuleDirection2D.Horizontal, 0, Vector2.down, _spikeRaycastHits, _stats.GrounderDistance, _stats.SpikeLayer);
            if (_spikeHitCount == 0) _spikeHitCount = Physics2D.CapsuleCastNonAlloc(_col.bounds.center, colSize, CapsuleDirection2D.Horizontal, 0, Vector2.up, _spikeRaycastHits, _stats.GrounderDistance, _stats.SpikeLayer);

            Physics2D.queriesHitTriggers = true; // Ladders are set to Trigger
            _ladderHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _ladderHits, _stats.LadderLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
        }
        
        protected virtual void ToggleColliders() {
            _col = _standingCollider;
            
            _standingCollider.enabled = true;
        }

        protected virtual bool TryGetGroundNormal(out Vector2 groundNormal) {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.Raycast(_rb.position, Vector2.down, _stats.GrounderDistance * 2, ~_stats.PlayerLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            groundNormal = hit.normal; // defaults to Vector2.zero if nothing was hit
            return hit.collider;
        }

        private Bounds GetWallDetectionBounds() {
            var colliderOrigin = _rb.position + _standingCollider.offset;
            return new Bounds(colliderOrigin, _stats.WallDetectorSize);
        }

        protected virtual void HandleCollisions() {
            if (_spikeHitCount > 0 && _healthHandler.Health > 0)
            {
                _healthHandler.TakeDamage(1);
            }
            
            // Hit a Ceiling
            if (_ceilingHitCount > 0) {
                // prevent sticking to ceiling if we did an InAir jump after receiving external velocity w/ PlayerForce.Decay
                _currentExternalVelocity.y = Mathf.Min(0f, _currentExternalVelocity.y);
                _speed.y = Mathf.Min(0, _speed.y);
            }

            // Landed on the Ground
            if (!_grounded && _groundHitCount > 0) {
                _grounded = true;
                ResetDash();
                ResetJump();
                GroundedChanged?.Invoke(true, Mathf.Abs(_speed.y));
                if (_frameInput.Move.x == 0) _stickyFeet = true;
            }
            // Left the Ground
            else if (_grounded && _groundHitCount == 0) {
                _grounded = false;
                _frameLeftGrounded = _fixedFrame;
                GroundedChanged?.Invoke(false, 0);
            }
        }

        private bool IsStandingPosClear(Vector2 pos) => CheckPos(pos, _standingCollider);

        protected virtual bool CheckPos(Vector2 pos, Collider2D col) {
            Physics2D.queriesHitTriggers = false;
            var hit = col switch
            {
                CapsuleCollider2D capsuleCol => Physics2D.OverlapCapsule(pos + capsuleCol.offset, capsuleCol.size - _skinWidth, capsuleCol.direction, 0, ~_stats.PlayerLayer),
                BoxCollider2D boxCol => Physics2D.OverlapBox(pos + boxCol.offset, boxCol.size - _skinWidth, 0, ~_stats.PlayerLayer),
                _ => null
            };
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            return !hit;
        }

        #endregion

        #region Walls

        private ContactPoint2D _wallContact;
        private float _currentWallJumpMoveMultiplier = 1f; // aka "Horizontal input influence"
        private int _lastWallDirection; // for coyote wall jumps
        private int _frameLeftWall; // for coyote wall jumps
        private bool _isLeavingWall; // prevents immediate re-sticking to wall
        private bool _isOnWall;

        protected virtual void HandleWalls() {
            if (!_stats.AllowWalls) return;

            _currentWallJumpMoveMultiplier = Mathf.MoveTowards(_currentWallJumpMoveMultiplier, 1f, 1f / _stats.WallJumpInputLossFrames);

            var collisions = new List<ContactPoint2D>();
            // May need to prioritize the nearest wall here... But who is going to make a climbable wall that tight?
            if (_wallHits[0]) _wallHits[0].GetContacts(collisions);

            for (var i = 0; i < collisions.Count-1; i++)
            {
                if (collisions[i].collider && collisions[i].collider.CompareTag("Player"))
                {
                    _wallContact = collisions[i];
                }
            }
            
            WallDirection = _wallHitCount > 0 ? (int) Mathf.Sign(_wallContact.point.x - transform.position.x) : 0;
            if (WallDirection != 0) _lastWallDirection = WallDirection;

            if (!_isOnWall && ShouldStickToWall() && _speed.y <= 0) ToggleOnWall(true);
            else if (_isOnWall && !ShouldStickToWall()) ToggleOnWall(false);

            bool ShouldStickToWall() {
                if (WallDirection == 0 || _grounded) return false;
                return !_stats.RequireInputPush || (HorizontalInputPressed && Mathf.Sign(_frameInput.Move.x) == WallDirection);
            }
        }

        private void ToggleOnWall(bool on) {
            _isOnWall = on;
            if (on) {
                _speed = Vector2.zero;
                _currentExternalVelocity = Vector2.zero;
                _bufferedJumpUsable = true;
                _wallJumpCoyoteUsable = true;
            }
            else {
                _frameLeftWall = _fixedFrame;
                _isLeavingWall = false; // after we've left the wall
                ResetAirJumps(); // so that we can air jump even if we didn't leave via a wall jump
            }

            WallGrabChanged?.Invoke(on);
        }

        #endregion

        #region Ledges

        private Vector2 _ledgeCornerPos;

        private bool LedgeClimbInputDetected => Input.y > _stats.VerticalDeadzoneThreshold || Input.x == WallDirection;

        protected virtual void HandleLedges() {
            if (!_stats.AllowLedges) return;
            if (ClimbingLedge || !_isOnWall) return;

            GrabbingLedge = TryGetLedgeCorner(out _ledgeCornerPos);

            if (GrabbingLedge) HandleLedgeGrabbing();
        }

        protected virtual bool TryGetLedgeCorner(out Vector2 cornerPos) {
            cornerPos = Vector2.zero;
            var grabHeight = _rb.position + _stats.LedgeGrabPoint.y * Vector2.up;

            var hit1 = Physics2D.Raycast(grabHeight + _stats.LedgeRaycastSpacing * Vector2.down, WallDirection * Vector2.right, 0.5f, _stats.ClimbableLayer);
            if (!hit1.collider) return false; // Should hit below the ledge. Mainly used to determine xPos accurately

            var hit2 = Physics2D.Raycast(grabHeight + _stats.LedgeRaycastSpacing * Vector2.up, WallDirection * Vector2.right, 0.5f, _stats.ClimbableLayer);
            if (hit2.collider)
                return false; // we only are within ledge-grab range when the first hits and second doesn't

            var hit3 = Physics2D.Raycast(grabHeight + new Vector2(WallDirection * 0.5f, _stats.LedgeRaycastSpacing), Vector2.down, 0.5f, _stats.ClimbableLayer);
            if (!hit3.collider) return false; // gets our yPos of the corner

            cornerPos = new(hit1.point.x, hit3.point.y);
            return true;
        }

        protected virtual void HandleLedgeGrabbing() {
            // Nudge towards better grabbing position
            if (Input.x == 0 && HasControl) {
                var pos = _rb.position;
                var targetPos = _ledgeCornerPos - Vector2.Scale(_stats.LedgeGrabPoint, new(WallDirection, 1f));
                _rb.position = Vector2.MoveTowards(pos, targetPos, _stats.LedgeGrabDeceleration * Time.fixedDeltaTime);
            }

            if (!LedgeClimbInputDetected) return;
            
            var finalPos = _ledgeCornerPos + Vector2.Scale(_stats.StandUpOffset, new(WallDirection, 1f));

            if (!IsStandingPosClear(finalPos)) return;
            
            StartLedgeClimb();
        }

        protected virtual void StartLedgeClimb() {
            LedgeClimbChanged?.Invoke();
            TakeAwayControl();
            ClimbingLedge = true;
            GrabbingLedge = false;
            _rb.position = _ledgeCornerPos - Vector2.Scale(_stats.LedgeGrabPoint, new(WallDirection, 1f));
        }

        public virtual void TeleportMidLedgeClimb() {
            transform.position = _rb.position = _ledgeCornerPos + Vector2.Scale(_stats.StandUpOffset, new(WallDirection, 1f));
            ToggleOnWall(false);
        }

        public virtual void FinishClimbingLedge() {
            ClimbingLedge = false;
            ReturnControl();
        }

        #endregion

        #region Ladders

        private Vector2 _ladderSnapVel;
        private int _frameLeftLadder;

        private bool CanEnterLadder => _ladderHitCount > 0 && _fixedFrame > _frameLeftLadder + _stats.LadderCooldownFrames;
        private bool ShouldMountLadder => _stats.AutoAttachToLadders || _frameInput.Move.y > _stats.VerticalDeadzoneThreshold || (!_grounded && _frameInput.Move.y < -_stats.VerticalDeadzoneThreshold);
        private bool ShouldDismountLadder => !_stats.AutoAttachToLadders && _grounded && _frameInput.Move.y < -_stats.VerticalDeadzoneThreshold;
        private bool ShouldCenterOnLadder => _stats.SnapToLadders && _frameInput.Move.x == 0 && HasControl;

        protected virtual void HandleLadders() {
            if (!_stats.AllowLadders) return;

            if (!ClimbingLadder && CanEnterLadder && ShouldMountLadder) ToggleClimbingLadder(true);
            else if (ClimbingLadder && (_ladderHitCount == 0 || ShouldDismountLadder)) ToggleClimbingLadder(false);

            if (ClimbingLadder && ShouldCenterOnLadder) {
                var pos = _rb.position;
                var targetX = _ladderHits[0].transform.position.x;
                _rb.position = Vector2.SmoothDamp(pos, new Vector2(targetX, pos.y), ref _ladderSnapVel, _stats.LadderSnapTime);
            }
        }

        private void ToggleClimbingLadder(bool on) {
            if (ClimbingLadder == on) return;
            if (on) {
                _speed = Vector2.zero;
                _ladderSnapVel = Vector2.zero; // reset damping velocity for consistency
            }
            else {
                if (_ladderHitCount > 0) _frameLeftLadder = _fixedFrame; // to prevent immediately re-mounting ladder
                if (_frameInput.Move.y > 0) _speed.y += _stats.LadderPopForce; // Pop off ladders
            }

            ClimbingLadder = on;
            ResetAirJumps();
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private bool _wallJumpCoyoteUsable;
        private int _frameJumpWasPressed;
        private int _airJumpsRemaining;

        private int _maxAirJumps;

        private bool HasBufferedJump => _bufferedJumpUsable && _fixedFrame < _frameJumpWasPressed + _stats.JumpBufferFrames;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _fixedFrame < _frameLeftGrounded + _stats.CoyoteFrames;
        private bool CanWallJump => (_isOnWall && !_isLeavingWall) || (_wallJumpCoyoteUsable && _fixedFrame < _frameLeftWall + _stats.WallJumpCoyoteFrames);
        private bool CanAirJump => !_grounded && _airJumpsRemaining > 0;

        protected virtual void HandleJump() {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true; // Early end detection

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (CanWallJump) WallJump();
            else if (_grounded || ClimbingLadder || CanUseCoyote) NormalJump();
            else if (_jumpToConsume && CanAirJump) AirJump();

            _jumpToConsume = false; // Always consume the flag
        }

        // Includes Ladder Jumps
        protected virtual void NormalJump() {
            _endedJumpEarly = false;
            _frameJumpWasPressed = 0; // prevents double-dipping 1 input's jumpToConsume and buffered jump for low ceilings
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            ToggleClimbingLadder(false);
            _speed.y = _stats.JumpPower;
            Jumped?.Invoke(false);
        }

        protected virtual void WallJump() {
            _endedJumpEarly = false;
            _bufferedJumpUsable = false;
            if (_isOnWall) _isLeavingWall = true; // only toggle if it's a real WallJump, not CoyoteWallJump
            _wallJumpCoyoteUsable = false;
            _currentWallJumpMoveMultiplier = 0;
            _speed = Vector2.Scale(_stats.WallJumpPower, new(-_lastWallDirection, 1));
            Jumped?.Invoke(true);
        }

        protected virtual void AirJump() {
            _endedJumpEarly = false;
            _airJumpsRemaining--;
            _speed.y = _stats.JumpPower;
            _currentExternalVelocity.y = 0; // optional. test it out with a Bouncer if this feels better or worse
            AirJumped?.Invoke();
        }

        protected virtual void ResetJump() {
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            ResetAirJumps();
        }

        protected virtual void ResetAirJumps() => _airJumpsRemaining = _maxAirJumps;

        #endregion

        #region Dashing

        private bool _dashToConsume;
        private bool _canDash;
        private Vector2 _dashVel;
        private bool _dashing;
        private int _startedDashing;

        private bool _hasDashPowerUp;

        protected virtual void HandleDash() {
            if (_dashToConsume && _canDash) {
                var dir = new Vector2(_frameInput.Move.x, Mathf.Max(_frameInput.Move.y, 0f)).normalized;
                if (dir == Vector2.zero) {
                    _dashToConsume = false;
                    return;
                }

                _dashVel = dir * _stats.DashVelocity;
                _dashing = true;
                _canDash = false;
                _startedDashing = _fixedFrame;
                DashingChanged?.Invoke(true, dir);

                _currentExternalVelocity = Vector2.zero; // Strip external buildup
            }

            if (_dashing) {
                _speed = _dashVel;
                // Cancel when the time is out or we've reached our max safety distance
                if (_fixedFrame > _startedDashing + _stats.DashDurationFrames) {
                    _dashing = false;
                    DashingChanged?.Invoke(false, Vector2.zero);
                    _speed.y = Mathf.Min(0, _speed.y);
                    _speed.x *= _stats.DashEndHorizontalMultiplier;
                    if (_grounded) ResetDash();
                }
            }

            _dashToConsume = false;
        }

        protected virtual void ResetDash() {
            _canDash = true;
        }

        #endregion

        // #region Attacking
        //
        // private bool _attackToConsume;
        // private int _frameLastAttacked = int.MinValue;
        //
        //
        // protected virtual void HandleAttacking() {
        //     if (!_attackToConsume) return;
        //     // note: animation looks weird if we allow attacking while crouched. consider different attack animations or not allow it while crouched
        //     if (_fixedFrame > _frameLastAttacked + _stats.AttackFrameCooldown) {
        //         _frameLastAttacked = _fixedFrame;
        //         Attacked?.Invoke();
        //     }
        //
        //     _attackToConsume = false;
        // }
        //
        // #endregion

        #region Horizontal

        private bool HorizontalInputPressed => Mathf.Abs(_frameInput.Move.x) > _stats.HorizontalDeadzoneThreshold;
        private bool _stickyFeet;

        protected virtual void HandleHorizontal() {
            if (_dashing) return;

            // Deceleration
            if (!HorizontalInputPressed) {
                var deceleration = _grounded ? _stats.GroundDeceleration * (_stickyFeet ? _stats.StickyFeetMultiplier : 1) : _stats.AirDeceleration;
                _speed.x = Mathf.MoveTowards(_speed.x, 0, deceleration * Time.fixedDeltaTime);
            }
            // Regular Horizontal Movement
            else {
                // Prevent useless horizontal speed buildup when against a wall
                if (_hittingWall.collider && Mathf.Abs(_rb.velocity.x) < 0.01f && !_isLeavingWall) _speed.x = 0;

                var xInput = _frameInput.Move.x * (ClimbingLadder ? _stats.LadderShimmySpeedMultiplier : 1);
                _speed.x = Mathf.MoveTowards(_speed.x, xInput * _stats.MaxSpeed, _currentWallJumpMoveMultiplier * _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Vertical

        protected virtual void HandleVertical() {
            if (_dashing) return;

            // Grounded & Slopes
            if (_grounded && _speed.y <= 0f) {
                _speed.y = _stats.GroundingForce;

                if (TryGetGroundNormal(out var groundNormal)) {
                    GroundNormal = groundNormal;
                    if (!Mathf.Approximately(GroundNormal.y, 1f)) {
                        // on a slope
                        _speed.y = _speed.x * -GroundNormal.x / GroundNormal.y;
                        if (_speed.x != 0) _speed.y += _stats.GroundingForce;
                    }
                }
            }
            // Wall Climbing & Sliding
            else if (_isOnWall && !_isLeavingWall) {
                if (_frameInput.Move.y > 0) _speed.y = _stats.WallClimbSpeed;
                else if (_frameInput.Move.y < 0) _speed.y = -_stats.MaxWallFallSpeed;
                else if (GrabbingLedge) _speed.y = Mathf.MoveTowards(_speed.y, 0, _stats.LedgeGrabDeceleration * Time.fixedDeltaTime);
                else _speed.y = Mathf.MoveTowards(Mathf.Min(_speed.y, 0), -_stats.MaxWallFallSpeed, _stats.WallFallAcceleration * Time.fixedDeltaTime);
            }
            // In Air
            else {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _speed.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _speed.y = Mathf.MoveTowards(_speed.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        protected virtual void ApplyMovement() {
            if (!HasControl) return;

            _rb.velocity = _speed + _currentExternalVelocity;
            _currentExternalVelocity = Vector2.MoveTowards(_currentExternalVelocity, Vector2.zero, _stats.ExternalVelocityDecay * Time.fixedDeltaTime);
        }
        
        public void AddPowerUp(PowerUpType powerUp) {
            switch (powerUp)
            {
                case PowerUpType.Jump:
                    _maxAirJumps++;
                    ResetAirJumps();
                    break;
                case PowerUpType.Dash:
                    _hasDashPowerUp = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(powerUp), powerUp, null);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (_stats == null) return;

            if (_stats.ShowWallDetection && _standingCollider != null) {
                Gizmos.color = Color.white;
                var bounds = GetWallDetectionBounds();
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            if (_stats.AllowLedges && _stats.ShowLedgeDetection) {
                Gizmos.color = Color.red;
                var facingDir = Mathf.Sign(WallDirection);
                var grabHeight = transform.position + _stats.LedgeGrabPoint.y * Vector3.up;
                var grabPoint = grabHeight + facingDir * _stats.LedgeGrabPoint.x * Vector3.right;
                Gizmos.DrawWireSphere(grabPoint, 0.05f);
                Gizmos.DrawWireSphere(grabPoint + Vector3.Scale(_stats.StandUpOffset, new(facingDir, 1)), 0.05f);
                Gizmos.DrawRay(grabHeight + _stats.LedgeRaycastSpacing * Vector3.down, 0.5f * facingDir * Vector3.right);
                Gizmos.DrawRay(grabHeight + _stats.LedgeRaycastSpacing * Vector3.up, 0.5f * facingDir * Vector3.right);
            }
        }

        private void OnValidate() {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
            if (_standingCollider == null) Debug.LogWarning("Please assign a Capsule Collider to the Standing Collider slot", this);
            if (_rb == null && !TryGetComponent(out _rb)) Debug.LogWarning("Ensure the GameObject with the Player Controller has a Rigidbody2D", this);
        }
#endif
    }

    public interface IPlayerController {
        /// <summary>
        /// true = Landed. false = Left the Ground. float is Impact Speed
        /// </summary>
        public event Action<bool, float> GroundedChanged;

        public event Action<bool, Vector2> DashingChanged; // Dashing - Dir
        public event Action<bool> WallGrabChanged;
        public event Action LedgeClimbChanged; // Into Crawl
        public event Action<bool> Jumped; // Is wall jump
        public event Action AirJumped;
        // public event Action Attacked;

        public ScriptableStats PlayerStats { get; }
        public Vector2 Input { get; }
        public Vector2 Speed { get; }
        public Vector2 Velocity { get; }
        public Vector2 GroundNormal { get; }
        public int WallDirection { get; }
        // public bool ClimbingLadder { get; }
        public bool GrabbingLedge { get; }
        public bool ClimbingLedge { get; }
        public bool HasControl {get; }
        public void ApplyVelocity(Vector2 vel, PlayerForce forceType);
        public void SetVelocity(Vector2 vel, PlayerForce velocityType);
    }

    public enum PlayerForce {
        /// <summary>
        /// Added directly to the players movement speed, to be controlled by the standard deceleration
        /// </summary>
        Burst,

        /// <summary>
        /// An external velocity that decays over time, applied additively to the rigidbody's velocity
        /// </summary>
        Decay
    }
}