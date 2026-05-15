using UnityEngine;
using UnityEngine.EventSystems;

namespace Territory.IsoSceneCore
{
    /// <summary>Camera pan/zoom/displacement service for isometric scenes. Configure in Start; Tick in Update. GridManager + CameraController delegate here.</summary>
    public sealed class IsoSceneCamera
    {
        private Camera _cam;
        private float _panSpeed = 5f;
        private bool _configured;

        // Zoom state
        private float[] _zoomLevels = new float[] { 2f, 5f, 10f, 15f, 20f, 30f };
        private int _currentZoomLevel;
        private float _targetOrthoSize;
        private float _scrollAccumulator;
        private float _lastZoomStepTime = -1f;
        private float _referenceOrthoSize = 10f;
        private float _scrollThresholdPerLevel = 0.2f;
        private float _zoomStepCooldown = 0.12f;
        private float _zoomSmoothSpeed = 18f;

        // Drag-to-pan state
        private Vector3 _lastMouseScreenPos;
        private Vector2 _rightClickDownScreenPos;
        private bool _isRightHeld;
        private bool _exceededPanThreshold;
        private float _dragPanThresholdPixels = 8f;
        private float _panInertiaDamping = 0.92f;
        private float _panInertiaMinVelocity = 0.001f;
        private Vector2 _panInertiaVelocity;
        private Vector2[] _recentPanDeltas = new Vector2[5];
        private int _panDeltaIndex;
        private int _panDeltaCount;

        /// <summary>True when last right-click release was a pan drag.</summary>
        public bool WasLastRightClickAPan { get; private set; }

        public void Configure(Camera cam, float panSpeed)
        {
            _cam = cam;
            _panSpeed = panSpeed;
            _configured = true;
            if (_zoomLevels.Length > 0)
            {
                _currentZoomLevel = FindClosestZoomLevel(_cam.orthographicSize);
                _targetOrthoSize = _zoomLevels[_currentZoomLevel];
            }
        }

        /// <summary>Configure with full zoom + pan parameters (for CameraController delegation).</summary>
        public void ConfigureFull(Camera cam, float panSpeed, float[] zoomLevels, float startZoom,
            float referenceOrthoSize, float scrollThreshold, float zoomCooldown, float zoomSmoothSpeed,
            float dragPanThreshold, float inertiaDamping, float inertiaMinVelocity)
        {
            _cam = cam;
            _panSpeed = panSpeed;
            _referenceOrthoSize = referenceOrthoSize;
            _scrollThresholdPerLevel = scrollThreshold;
            _zoomStepCooldown = zoomCooldown;
            _zoomSmoothSpeed = zoomSmoothSpeed;
            _dragPanThresholdPixels = dragPanThreshold;
            _panInertiaDamping = inertiaDamping;
            _panInertiaMinVelocity = inertiaMinVelocity;
            if (zoomLevels != null && zoomLevels.Length > 0)
                _zoomLevels = zoomLevels;
            _currentZoomLevel = FindClosestZoomLevel(startZoom);
            _targetOrthoSize = _zoomLevels[_currentZoomLevel];
            _cam.orthographicSize = _targetOrthoSize;
            _configured = true;
        }

        public void Tick(float dt)
        {
            if (_cam == null || !_configured) return;

            if (!Input.GetMouseButton(1))
                WasLastRightClickAPan = false;

            HandleMovement(dt);
            HandleZoomKeys();
            HandleScrollZoom();
            ApplySmoothZoom(dt);
            HandleDragToPan();
            ApplyPanInertia();
        }

        public void ZoomIn()
        {
            if (_currentZoomLevel > 0)
            {
                _currentZoomLevel--;
                _targetOrthoSize = _zoomLevels[_currentZoomLevel];
            }
        }

        public void ZoomOut()
        {
            if (_currentZoomLevel < _zoomLevels.Length - 1)
            {
                _currentZoomLevel++;
                _targetOrthoSize = _zoomLevels[_currentZoomLevel];
            }
        }

        public void MoveTo(Vector3 worldPos)
        {
            if (_cam == null) return;
            _cam.transform.position = new Vector3(worldPos.x, worldPos.y, _cam.transform.position.z);
        }

        private void HandleMovement(float dt)
        {
            if (IsPointerOverBlockingUi()) return;
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            if (horizontal != 0 || vertical != 0)
                _panInertiaVelocity = Vector2.zero;
            float effectiveSpeed = _panSpeed * (_cam.orthographicSize / _referenceOrthoSize);
            Vector3 movement = new Vector3(horizontal, vertical, 0).normalized * effectiveSpeed * dt;
            _cam.transform.Translate(movement);
        }

        private void HandleZoomKeys()
        {
            if (Input.GetKeyDown(KeyCode.Z)) ZoomIn();
            else if (Input.GetKeyDown(KeyCode.X)) ZoomOut();
        }

        private void HandleScrollZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;
            if (IsPointerOverBlockingUi()) return;
            _scrollAccumulator += scroll;
            float t = Time.unscaledTime - _lastZoomStepTime;
            if (t < _zoomStepCooldown) return;
            float threshold = _scrollThresholdPerLevel;
            if (_scrollAccumulator >= threshold)
            {
                int steps = Mathf.Min(Mathf.FloorToInt(_scrollAccumulator / threshold), _currentZoomLevel);
                if (steps > 0)
                {
                    _currentZoomLevel -= steps;
                    _targetOrthoSize = _zoomLevels[_currentZoomLevel];
                    _scrollAccumulator -= steps * threshold;
                    _lastZoomStepTime = Time.unscaledTime;
                }
                else _scrollAccumulator = 0f;
            }
            else if (_scrollAccumulator <= -threshold)
            {
                int steps = Mathf.FloorToInt(-_scrollAccumulator / threshold);
                int maxSteps = _zoomLevels.Length - 1 - _currentZoomLevel;
                steps = Mathf.Min(steps, maxSteps);
                if (steps > 0)
                {
                    _currentZoomLevel += steps;
                    _targetOrthoSize = _zoomLevels[_currentZoomLevel];
                    _scrollAccumulator += steps * threshold;
                    _lastZoomStepTime = Time.unscaledTime;
                }
                else _scrollAccumulator = 0f;
            }
            _scrollAccumulator = Mathf.Clamp(_scrollAccumulator, -threshold * 1.5f, threshold * 1.5f);
        }

        private void ApplySmoothZoom(float dt)
        {
            if (_zoomLevels.Length == 0) return;
            float current = _cam.orthographicSize;
            if (Mathf.Approximately(current, _targetOrthoSize)) return;
            _cam.orthographicSize = Mathf.Lerp(current, _targetOrthoSize, _zoomSmoothSpeed * Time.unscaledDeltaTime);
        }

        private void HandleDragToPan()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                _panInertiaVelocity = Vector2.zero;
            if (IsPointerOverBlockingUi()) return;
            if (Input.GetMouseButtonDown(1))
            {
                _rightClickDownScreenPos = Input.mousePosition;
                _lastMouseScreenPos = Input.mousePosition;
                _isRightHeld = true;
                _exceededPanThreshold = false;
                _panDeltaCount = 0;
                _panDeltaIndex = 0;
                _panInertiaVelocity = Vector2.zero;
            }
            if (Input.GetMouseButton(1) && _isRightHeld)
            {
                if (!_exceededPanThreshold &&
                    Vector2.Distance(Input.mousePosition, _rightClickDownScreenPos) > _dragPanThresholdPixels)
                    _exceededPanThreshold = true;
                if (_exceededPanThreshold)
                {
                    Vector3 curWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
                    Vector3 lastWorld = _cam.ScreenToWorldPoint(_lastMouseScreenPos);
                    Vector3 delta = curWorld - lastWorld;
                    _cam.transform.position -= new Vector3(delta.x, delta.y, 0);
                    _lastMouseScreenPos = Input.mousePosition;
                    _recentPanDeltas[_panDeltaIndex] = new Vector2(delta.x, delta.y);
                    _panDeltaIndex = (_panDeltaIndex + 1) % _recentPanDeltas.Length;
                    if (_panDeltaCount < _recentPanDeltas.Length) _panDeltaCount++;
                }
            }
            if (Input.GetMouseButtonUp(1))
            {
                WasLastRightClickAPan = _exceededPanThreshold;
                _isRightHeld = false;
                if (_exceededPanThreshold && _panDeltaCount > 0)
                {
                    Vector2 avg = Vector2.zero;
                    int count = Mathf.Min(_panDeltaCount, _recentPanDeltas.Length);
                    for (int i = 0; i < count; i++) avg += _recentPanDeltas[i];
                    avg /= count;
                    float zoomScale = _cam.orthographicSize / _referenceOrthoSize;
                    _panInertiaVelocity = avg * zoomScale;
                }
                else _panInertiaVelocity = Vector2.zero;
            }
        }

        private void ApplyPanInertia()
        {
            if (_panInertiaVelocity.sqrMagnitude < _panInertiaMinVelocity * _panInertiaMinVelocity)
            {
                _panInertiaVelocity = Vector2.zero;
                return;
            }
            _cam.transform.position -= new Vector3(_panInertiaVelocity.x, _panInertiaVelocity.y, 0);
            _panInertiaVelocity *= _panInertiaDamping;
        }

        private int FindClosestZoomLevel(float target)
        {
            int closest = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < _zoomLevels.Length; i++)
            {
                float dist = Mathf.Abs(_zoomLevels[i] - target);
                if (dist < minDist) { minDist = dist; closest = i; }
            }
            return closest;
        }

        private static bool IsPointerOverBlockingUi()
        {
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
