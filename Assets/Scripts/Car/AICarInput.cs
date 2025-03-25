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

        [Header("Spline Settings")]
        [SerializeField] private SplineContainer _pathSpline;
        [SerializeField] private float _maxRandomSpeedVariation = 0.2f;
        [SerializeField] private float _waypointReachedDistance = 3f;

        [SerializeField] private float _collisionDetectionDistance = 6f;
        [SerializeField] private float _avoidanceStrength = 0.7f;
        [SerializeField] private LayerMask _carLayerMask;

        private RaceCarController _carController;
        private float _steeringInput;
        private float _throttleInput;
        private bool _isHandbrakeActive;
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
            // Направление до конца текущего сектора
            Vector3 directionToSectorEnd = _segments[_currentSegmentIndex].EndPoint - transform.position;
            float distanceToEnd = directionToSectorEnd.magnitude;
            Vector3 localDirectionToEnd = transform.InverseTransformDirection(directionToSectorEnd);

            // Получаем углы поворота в текущем и следующем секторах
            float currentSectorAngle = _segments[_currentSegmentIndex].TurnAngle;
            int nextSectorIndex = (_currentSegmentIndex + 1) % _segments.Count;
            float nextSectorAngle = _segments[nextSectorIndex].TurnAngle;

            // Рассчитываем угол поворота относительно машины
            float angleToEnd = Mathf.Atan2(localDirectionToEnd.x, localDirectionToEnd.z) * Mathf.Rad2Deg;

            // Влияние следующего сектора увеличивается при приближении к концу текущего
            float blendFactor = Mathf.Clamp01(1 - (distanceToEnd / (_waypointReachedDistance * 5)));

            // Учитываем скорость - чем быстрее едем, тем сильнее поворачиваем
            float speedInfluence = Mathf.Clamp01(_carController.CurrentSpeed / 30f);

            // Смешиваем углы текущего и следующего секторов
            float targetAngle = Mathf.Lerp(currentSectorAngle, nextSectorAngle, blendFactor);

            // Применяем финальный поворот с учетом скорости
            float steeringMultiplier = Mathf.Lerp(0.5f, 1.5f, speedInfluence);
            float rawSteeringAmount = Mathf.Clamp(angleToEnd * steeringMultiplier / 45f, -1f, 1f);

            // Визуальная отладка
            //Debug.DrawRay(transform.position, directionToSectorEnd, Color.red, 0.1f);
            //Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue, 0.1f);

            // Применяем избегание препятствий и сглаживание поворота
            float avoidanceModifier = 0;
            if (rawSteeringAmount >= -0.3f && rawSteeringAmount <= 0.3f) avoidanceModifier = CalculateAvoidanceModifier();

            rawSteeringAmount = Mathf.Clamp(rawSteeringAmount + avoidanceModifier, -1f, 1f);
            _steeringInput = Mathf.Lerp(_lastSteeringInput, rawSteeringAmount, 0.9f);
            _lastSteeringInput = _steeringInput;
        }
        private float _val;
        private void CalculateThrottleAndBrakeInput()
        {
            _throttleInput = 1 - Mathf.Abs(_steeringInput);
        }


        private float CalculateAvoidanceModifier()
        {
            float avoidanceModifier = 0;

            // Учет препятствий
            foreach (var collider in _nearbyColliders)
            {
                Vector3 directionToObstacle = collider.transform.position - transform.position;
                float distance = directionToObstacle.magnitude;

                // Игнорируем машины позади
                if (Vector3.Dot(transform.forward, directionToObstacle) < 0)
                    continue;

                Vector3 localObstacleDir = transform.InverseTransformDirection(directionToObstacle);
                float side = Mathf.Sign(localObstacleDir.x);

                float proximityFactor = 1 - Mathf.Clamp01(distance / _collisionDetectionDistance);
                float forwardProximityScale = Mathf.Clamp01(localObstacleDir.z); // Сильнее реагируем на близкие по Z препятствия
                float strength = _avoidanceStrength * proximityFactor * proximityFactor * forwardProximityScale;

                avoidanceModifier -= side * strength;
            }
            return Mathf.Clamp(avoidanceModifier, -1f, 1f);
        }

        #endregion
    }
}