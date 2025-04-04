using System;
using System.Collections.Generic;
using MiniRace.Control;
using MiniRace.Environment;
using MiniRace.Game;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace MiniRace
{
    public class AICarInput : MonoBehaviour, ICarInput, ICarPositionTracker
    {
        #region --- Members ---

        [Header("Settings")]
        [SerializeField] private float _waypointReachedDistance;
        [SerializeField] private float _collisionDetectionDistance;
        [SerializeField] private float _avoidanceStrength;
        [SerializeField] private LayerMask _carLayerMask;
        [SerializeField] private float _minSteeringValueForAvoidance;
        [SerializeField][Range(0f, 1f)] private float _steeringLerpFactor;

        [Header("Variables")]
        private float _steeringInput;
        private float _throttleInput;
        private bool _isHandbrakeActive;
        private RaceCarController _carController;
        private List<Collider> _nearbyColliders = new List<Collider>();
        private List<RoadSegment> _segments;
        private int _currentSegmentIndex;
        private float _lastSteeringInput;

        #endregion

        #region --- Events ---

        public event Action OnInputUpdated;
        public event Action OnLapCompleted;

        #endregion

        #region --- Properties ---

        public float ThrottleInput { get => _throttleInput; }
        public float SteeringInput { get => _steeringInput; }
        public bool IsHandbrakeActive { get => _isHandbrakeActive; }

        public int CurrentLap { get; private set; }
        public int CurrentCheckpointIndex { get; private set; }
        public float DistanceToNextCheckpoint { get; private set; }
        public bool IsPlayer { get; private set; }

        #endregion

        #region --- Mono Override Methods ---

        private void Update()
        {
            UpdateInput();
        }

        #endregion

        #region --- ICarInput Implementation ---

        public void UpdateInput()
        {
            TryUpdateRoadSegment();
            CheckForNearbyObstacles();

            CalculateSteeringInput();
            CalculateThrottleAndBrakeInput();

            OnInputUpdated?.Invoke();
        }

        #endregion

        #region --- Control Methods ---

        public void Initialize(List<RoadSegment> segments)
        {
            _carController = GetComponent<RaceCarController>();
            _segments = segments;

            RacePositionManager.Instance.RegisterCar(this);
            IsPlayer = false;
        }
        private void TryUpdateRoadSegment()
        {
            int nextValue = _currentSegmentIndex + 1;
            if (nextValue > _segments.Count - 1) nextValue = 0;

            DistanceToNextCheckpoint = Vector3.Distance(transform.position, _segments[_currentSegmentIndex].EndPoint);

            if (DistanceToNextCheckpoint < _waypointReachedDistance || Vector3.Distance(transform.position, _segments[nextValue].EndPoint) < _waypointReachedDistance)
            {
                _currentSegmentIndex = nextValue;
                CurrentCheckpointIndex = _currentSegmentIndex;
                if (_currentSegmentIndex == 0) CurrentLap++;
            }
        }
        private void CheckForNearbyObstacles()
        {
            _nearbyColliders.Clear();
            Collider[] colliders = Physics.OverlapSphere(transform.position, _collisionDetectionDistance, _carLayerMask);

            foreach (var collider in colliders)
            {
                if (collider.transform != transform)
                {
                    _nearbyColliders.Add(collider);
                }
            }
        }
        private void CalculateSteeringInput()
        {
            Vector3 directionToSectorEnd = _segments[_currentSegmentIndex].EndPoint - transform.position;
            float distanceToEnd = directionToSectorEnd.magnitude;
            Vector3 localDirectionToEnd = transform.InverseTransformDirection(directionToSectorEnd);

            float currentSectorAngle = _segments[_currentSegmentIndex].TurnAngle;
            int nextSectorIndex = (_currentSegmentIndex + 1) % _segments.Count;
            float nextSectorAngle = _segments[nextSectorIndex].TurnAngle;

            float angleToEnd = Mathf.Atan2(localDirectionToEnd.x, localDirectionToEnd.z) * Mathf.Rad2Deg;
            float blendFactor = Mathf.Clamp01(1 - (distanceToEnd / (_waypointReachedDistance * 5)));
            float speedInfluence = Mathf.Clamp01(_carController.CurrentSpeed / 30f);
            float targetAngle = Mathf.Lerp(currentSectorAngle, nextSectorAngle, blendFactor);

            float steeringMultiplier = Mathf.Lerp(0.5f, 1.5f, speedInfluence);
            float rawSteeringAmount = Mathf.Clamp(angleToEnd * steeringMultiplier / 45f, -1f, 1f);

            float avoidanceModifier = 0;
            if (rawSteeringAmount >= -_minSteeringValueForAvoidance && rawSteeringAmount <= _minSteeringValueForAvoidance) avoidanceModifier = CalculateAvoidanceModifier();

            rawSteeringAmount = Mathf.Clamp(rawSteeringAmount + avoidanceModifier, -1f, 1f);
            _steeringInput = Mathf.Lerp(_lastSteeringInput, rawSteeringAmount, _steeringLerpFactor);
            _lastSteeringInput = _steeringInput;
        }
        private void CalculateThrottleAndBrakeInput()
        {
            _throttleInput = 1 - Mathf.Abs(_steeringInput);
        }
        private float CalculateAvoidanceModifier()
        {
            float avoidanceModifier = 0;

            foreach (var collider in _nearbyColliders)
            {
                Vector3 directionToObstacle = collider.transform.position - transform.position;
                float distance = directionToObstacle.magnitude;

                if (Vector3.Dot(transform.forward, directionToObstacle) < 0) continue;

                Vector3 localObstacleDir = transform.InverseTransformDirection(directionToObstacle);
                float side = Mathf.Sign(localObstacleDir.x);

                float proximityFactor = 1 - Mathf.Clamp01(distance / _collisionDetectionDistance);
                float forwardProximityScale = Mathf.Clamp01(localObstacleDir.z);
                float strength = _avoidanceStrength * proximityFactor * proximityFactor * forwardProximityScale;

                avoidanceModifier -= side * strength;
            }
            return Mathf.Clamp(avoidanceModifier, -1f, 1f);
        }

        #endregion
    }
}